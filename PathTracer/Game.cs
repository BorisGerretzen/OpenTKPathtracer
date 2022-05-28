using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PathTracer.Helpers;
using PathTracer.Scene;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;

namespace PathTracer;

public class Game : GameWindow {
    private readonly int _maxCuboids = 64;
    private readonly int _maxSpheres = 256;
    private SceneLoader _sceneLoader;
    private readonly Stopwatch _stopwatch = new();
    private bool quality;
    private BufferHandle _basicDataUbo;
    private Camera _camera;
    private bool _cameraLocked;
    private bool _firstMove;
    private FramebufferHandle _framebufferHandle;
    private uint _frameNumber;
    private uint _lastFrameCount;

    private KeyboardState _lastKeyboardState;
    private Vector2 _lastPos;
    private ShaderProgram _shaderProgram;
    private TextureHandle _skyboxTexture;
    private TextureHandle _textureHandle;
    private Vector2i _windowSize;
    private GLDebugProc callback;
    private int _exportStartFrame = -10000;
    private readonly string _scene;
    private int _rayDepth;

    private int RayDepth {
        get => _rayDepth;
        set {
            if (value <= 0) {
                Console.WriteLine("Ray depth <= 0 is not allowed");
                return;
            }

            _rayDepth = value;
            GL.NamedBufferSubData(_basicDataUbo, (IntPtr)(Vector4.SizeInBytes * 9), 4, (float)_rayDepth);
            _frameNumber = 0;
            Console.WriteLine($"Rendering with maximum ray depth of {_rayDepth}.");
        }
    }

    public Game(
        GameWindowSettings gameWindowSettings,
        NativeWindowSettings nativeWindowSettings,
        int rayDepth,
        string scene = "")
        : base(gameWindowSettings, nativeWindowSettings) {
        _windowSize = new Vector2i(0);
        _windowSize.X = nativeWindowSettings.Size.X;
        _windowSize.Y = nativeWindowSettings.Size.Y;
        quality = false;
        _rayDepth = rayDepth == 0 ? 2 : rayDepth;
        _scene = scene;
    }

    private static void OpenGlDebugCallback(DebugSource source, DebugType type, uint id, DebugSeverity severity,
        int length, IntPtr message, IntPtr userParam) {
        var errorString = source == DebugSource.DebugSourceApplication
            ? $"OpenGL - {Marshal.PtrToStringAnsi(message, length)}"
            : $"OpenGL - {Marshal.PtrToStringAnsi(message, length)}\n\tid:{id} severity:{severity} type:{type} source:{source}\n";
        if (severity == DebugSeverity.DebugSeverityHigh) throw new Exception(errorString);
        Console.WriteLine(errorString);
    }

    protected override void OnLoad() {
        GL.Enable(EnableCap.TextureCubeMapSeamless);

        base.OnLoad();

        // Enable debugging
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        callback = OpenGlDebugCallback;
        GL.DebugMessageCallback(callback, IntPtr.Zero);
        GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0,
            DebugSeverity.DebugSeverityNotification, -1, "Debug callback initialized");

        // Create basic data UBO
        GL.CreateBuffer(out _basicDataUbo);
        GL.NamedBufferStorage(_basicDataUbo, Vector4.SizeInBytes * 4 * 2 + Vector4.SizeInBytes + 4, IntPtr.Zero,
            BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.UniformBuffer, 0, _basicDataUbo, IntPtr.Zero,
            Vector4.SizeInBytes * 4 * 2 + Vector4.SizeInBytes + 4);
        RayDepth = _rayDepth;
        
        // Create GameObjects UBO
        BufferHandle gameObjectsUbo;
        GL.CreateBuffer(out gameObjectsUbo);
        GL.NamedBufferStorage(gameObjectsUbo, _maxSpheres * Sphere.SizeInBytes + _maxCuboids * Cuboid.SizeInBytes,
            IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.UniformBuffer, 1, gameObjectsUbo, IntPtr.Zero,
            _maxSpheres * Sphere.SizeInBytes + _maxCuboids * Cuboid.SizeInBytes);

        // Create lights ubo
        BufferHandle lightsUBO;
        GL.CreateBuffer(out lightsUBO);
        GL.NamedBufferStorage(lightsUBO, _maxSpheres * Sphere.SizeInBytes, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.UniformBuffer, 7, lightsUBO, IntPtr.Zero, _maxSpheres * Sphere.SizeInBytes);
        
        // Create texture to render to
        GL.ActiveTexture(TextureUnit.Texture0);
        _textureHandle = GL.CreateTexture(TextureTarget.Texture2d);
        GL.BindTexture(TextureTarget.Texture2d, _textureHandle);
        GL.TextureParameteri(_textureHandle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2d, 0, (int)InternalFormat.Rgba32f, _windowSize.X, _windowSize.Y, 0,
            PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

        // Create framebuffer to display rendered frame
        _framebufferHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferHandle);
        GL.NamedFramebufferTexture(_framebufferHandle, FramebufferAttachment.ColorAttachment0, _textureHandle, 0);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebufferHandle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, new FramebufferHandle(0));

        // Load skybox texture
        GL.ActiveTexture(TextureUnit.Texture1);
        _skyboxTexture = GL.CreateTexture(TextureTarget.TextureCubeMap); //GL.CreateTexture(TextureTarget.TextureCubeMap);
        GL.BindTexture(TextureTarget.TextureCubeMap, _skyboxTexture);
        foreach (var file in Directory.GetFiles(@"Assets\Images\Skybox"))
            using (var image = Image.Load(file)) {
                image.Mutate(img => img.Flip(FlipMode.Horizontal));
                using (var ms = new MemoryStream()) {
                    image.Save(ms, new RawBytesEncoder());
                    GL.TexImage2D(Texture.CubeMapTextureTargetFromString(file), 0, (int)InternalFormat.Rgb, 2048,
                        2048, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ms.ToArray());
                }
            }
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        
        // Load compute shader
        _shaderProgram = new ShaderProgram(new List<Shader>
            { new("Shader/pathtracer.comp", ShaderType.ComputeShader) });
        _shaderProgram.Use();

        // Create Vertex, indices, mesh, BVH buffer handles
        BufferHandle vertexBufferHandle, indicesBufferHandle, meshBufferHandle, bvhMetadataHandle, bvhBufferHandle;
        GL.CreateBuffer(out vertexBufferHandle);
        GL.CreateBuffer(out indicesBufferHandle);
        GL.CreateBuffer(out meshBufferHandle);
        GL.CreateBuffer(out bvhMetadataHandle);
        GL.CreateBuffer(out bvhBufferHandle);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, vertexBufferHandle);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, indicesBufferHandle);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, meshBufferHandle);
        GL.BindBuffer(BufferTargetARB.UniformBuffer, bvhMetadataHandle);
        GL.BindBuffer(BufferTargetARB.ShaderStorageBuffer, bvhBufferHandle);

        // Load scene
        var modelHolder = new ModelHolder(vertexBufferHandle, indicesBufferHandle, meshBufferHandle, bvhMetadataHandle, bvhBufferHandle);
        _sceneLoader = new SceneLoader(_maxCuboids, _maxSpheres, gameObjectsUbo, lightsUBO, modelHolder);
        if (string.IsNullOrEmpty(_scene))
            CreateScene();
        else
            _sceneLoader.LoadScene(_scene);

        _sceneLoader.Upload();
        
        // Spawn camera
        _camera = new Camera(new Vector3(5, 4, 8), Size.X / (float)Size.Y);
        CursorGrabbed = true;
    }

    private void CreateScene() {
        var whiteDiffuse = new Material(new Vector3(0.9f, 0.9f, 0.9f), new Vector3(0));
        var whiteDiffuseRefractive = new Material(new Vector3(1, 1, 1), new Vector3(0.2f, 0.2f, 0.2f), 0.02f, 0.98f, 1.52f);
        var whiteDiffuseReflective = new Material(new Vector3(1, 1, 1), new Vector3(0, 0, 0), 1f);
        var redDiffuse = new Material(new Vector3(1, 0.3f, 0.3f), new Vector3(0.0f));
        var greenDiffuse = new Material(new Vector3(0.65f, 0.3f, 0.65f), new Vector3(0));
        var blueDiffuse = new Material(new Vector3(0.3f, 0.3f, 1), new Vector3(0));

        var greenLight = new Material(new Vector3(0.04f), new Vector3(0.2f, 1f, 0.2f) * 10.0f);
        var purpleLight = new Material(new Vector3(0.04f), new Vector3(0.678f, 0.4f, 0.815f));
        var redLight = new Material(new Vector3(1, 0, 0), new Vector3(0.4f, 0.2f, 0.2f));
        var blueLight = new Material(new Vector3(0.04f), new Vector3(0.2f, 0.2f, 1f) * 10.0f);
        var whiteLight = new Material(new Vector3(0.04f), new Vector3(1, 0.964f, 0.929f) * 40);
        var yellowLight = new Material(new Vector3(0.04f), new Vector3(1, 1, 0.4f) * 20);
        var whiteLightSoft = new Material(new Vector3(0.02f), new Vector3(1, 0.964f, 0.929f) * 2f);

        // floor
        _sceneLoader.AddCuboid(new Vector3(0, 0, -10), new Vector3(10, 1, 10), whiteDiffuse);
        // // roof
        _sceneLoader.AddCuboid(new Vector3(0, 10, -10), new Vector3(10, 11, 10), whiteDiffuse);
        // right wall
        _sceneLoader.AddCuboid(new Vector3(0, 1, -10), new Vector3(1, 10, 9), blueDiffuse);
        // left wall
        _sceneLoader.AddCuboid(new Vector3(9, 1, -10), new Vector3(10, 10, 9), redDiffuse);
        // backwall
        _sceneLoader.AddCuboid(new Vector3(0, 1, 9), new Vector3(10, 2, 10), whiteDiffuse);
        _sceneLoader.AddCuboid(new Vector3(0, 9, 9), new Vector3(10, 10, 10), whiteDiffuse);
        _sceneLoader.AddCuboid(new Vector3(0, 1, 9), new Vector3(2, 10, 10), whiteDiffuse);
        _sceneLoader.AddCuboid(new Vector3(8, 1, 9), new Vector3(10, 10, 10), whiteDiffuse);
        _sceneLoader.AddCuboid(new Vector3(2, 2, 9), new Vector3(8, 9, 10), whiteDiffuse);
        // Frontwall
        _sceneLoader.AddCuboid(new Vector3(0, 1, -5), new Vector3(10, 10, -4), whiteDiffuse);
        // //Light
        _sceneLoader.AddSphere(new Vector3(5f, 7.4f, 3.5f), 0.5f, whiteLight);
        // _sceneLoader.AddSphere(new Vector3(5f, 7.4f, 6.5f), 0.5f, yellowLight);

        // _sceneLoader.AddSphere(new Vector3(3.5f, 2.5f, 3.5f), 1.5f, Material.FullSpecular);
        // _sceneLoader.AddSphere(new Vector3(5f, 4.5f, 3.5f), 1.5f, Material.Glass);
        // _sceneLoader.AddModel(@"Assets\Models\bunny.obj", Material.WhiteDiffuse, new Vector3(5.5f, -0.2f, 3f), Vector3.One * 30);

        _sceneLoader.AddCuboid(new Vector3(3f, 2f, 2f), 2f, Material.Glossy);
        _sceneLoader.AddCuboid(new Vector3(6f, 2.5f, -1f), 3f, Material.Glossy);
        // _sceneLoader.AddSphere(new Vector3(3f, 4f, 2f), 1f, Material.FullSpecular);
        // _sceneLoader.AddSphere(new Vector3(6f, 3f, -1f), 2f, Material.FullSpecular);
        var serializer = new XmlSerializer(typeof(Scene.Scene));
        var writer = new StreamWriter(@"Assets\Scenes\export.xml", false);
        serializer.Serialize(writer, _sceneLoader.Scene);
        _stopwatch.Start();
    }

    protected override void OnResize(ResizeEventArgs e) {
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.AspectRatio = e.Width / (float)e.Height;
        _windowSize.X = e.Width;
        _windowSize.Y = e.Height;
        GL.NamedBufferSubData(_basicDataUbo, (IntPtr)0, Vector4.SizeInBytes * 4,
            _camera.GetProjectionMatrix().Inverted());
        GL.BindTexture(TextureTarget.Texture2d, _textureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, (int)InternalFormat.Rgba32f, _windowSize.X, _windowSize.Y, 0,
            PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        _frameNumber = 0;
    }


    protected override void OnRenderFrame(FrameEventArgs args) {
        if (_stopwatch.ElapsedMilliseconds > 3000) {
            Console.WriteLine((_frameNumber - _lastFrameCount) / (_stopwatch.ElapsedMilliseconds / 1000));
            _stopwatch.Restart();
            _lastFrameCount = _frameNumber;
        }

        base.OnRenderFrame(args);

        _shaderProgram.SetUniformUInt(0, _frameNumber++);
        _shaderProgram.SetUniformVec2(1, _sceneLoader.GetBasicData());
        _shaderProgram.SetUniformVec2(2, _sceneLoader.GetLightsSize());
        // GL.BindTexture(TextureTarget.Texture2d, _textureHandle);
        GL.BindTexture(TextureTarget.TextureCubeMap, _skyboxTexture);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

        GL.BindImageTexture(0, _textureHandle, 0, 0, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba32f);

        GL.DispatchCompute((uint)(_windowSize.X + 8 - 1) / 8, (uint)(_windowSize.Y + 4 - 1) / 4, 1);
        GL.MemoryBarrier(MemoryBarrierMask.TextureFetchBarrierBit);
        GL.BlitFramebuffer(0, 0, _windowSize.X, _windowSize.Y, 0, 0, _windowSize.X, _windowSize.Y,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
        SwapBuffers();

        if (_exportStartFrame >= _frameNumber - 1000 && _frameNumber % 100 == 0) SaveImage();
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private void SaveImage() {
        var name = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString();
        GL.PixelStorei(PixelStoreParameter.PackAlignment, 1);
        var pixels = new byte[_windowSize.X * _windowSize.Y * 4];
        GL.ReadPixels(0, 0, _windowSize.X, _windowSize.Y, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);
        var fixedPixels = new byte[_windowSize.X * _windowSize.Y * 4];

        for (var y = 0; y < _windowSize.Y; y++)
            Array.Copy(pixels, y * _windowSize.X * 4, fixedPixels,
                (_windowSize.X * _windowSize.Y - (y + 1) * _windowSize.X) * 4, _windowSize.X * 4);
        var bmp = new Bitmap(_windowSize.X, _windowSize.Y, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var bmpData = bmp.LockBits(new Rectangle(0, 0, _windowSize.X, _windowSize.Y), ImageLockMode.WriteOnly,
            bmp.PixelFormat);
        Marshal.Copy(fixedPixels, 0, bmpData.Scan0, fixedPixels.Length);

        bmp.UnlockBits(bmpData);
        bmp.Save($"{name}.png", ImageFormat.Png);
    }

    protected override void OnUpdateFrame(FrameEventArgs e) {
        var cameraMoved = false;
        if (!IsFocused) // Check to see if the window is focused
            return;

        var input = KeyboardState;

        if (input.IsKeyDown(Keys.Escape)) Close();

        // Lock camera on press L
        if (KeyboardState.IsKeyDown(Keys.L) && !_lastKeyboardState.IsKeyDown(Keys.L)) {
            _cameraLocked = !_cameraLocked;
            var lockState = _cameraLocked ? "locked" : "unlocked";
            GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0,
                DebugSeverity.DebugSeverityNotification, -1, $"Camera {lockState}");
        }

        if (KeyboardState.IsKeyDown(Keys.Equal) && !_lastKeyboardState.IsKeyDown(Keys.Equal)) RayDepth++;

        if (KeyboardState.IsKeyDown(Keys.Minus) && !_lastKeyboardState.IsKeyDown(Keys.Minus)) RayDepth--;

        // Save image on press I
        if (KeyboardState.IsKeyDown(Keys.I) && !_lastKeyboardState.IsKeyDown(Keys.I)) {
            GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0,
                DebugSeverity.DebugSeverityNotification, -1, "Starting image export");
            SaveImage();
            GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0,
                DebugSeverity.DebugSeverityNotification, -1, "Image export finished");
        }

        // Do export of 1000 frames on E
        if (KeyboardState.IsKeyDown(Keys.E) && !_lastKeyboardState.IsKeyDown(Keys.E)) {
            GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0,
                DebugSeverity.DebugSeverityNotification, -1, "Starting progressive export");
            _exportStartFrame = (int)_frameNumber;
        }

        _lastKeyboardState = KeyboardState.GetSnapshot();
        if (_cameraLocked) return;

        const float cameraSpeed = 1.5f;
        const float sensitivity = 0.2f;

        if (input.IsKeyDown(Keys.W)) {
            _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
            cameraMoved = true;
        }

        if (input.IsKeyDown(Keys.S)) {
            _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
            cameraMoved = true;
        }

        if (input.IsKeyDown(Keys.A)) {
            _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
            cameraMoved = true;
        }

        if (input.IsKeyDown(Keys.D)) {
            _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
            cameraMoved = true;
        }

        if (input.IsKeyDown(Keys.Space)) {
            _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
            cameraMoved = true;
        }

        if (input.IsKeyDown(Keys.LeftShift)) {
            _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
            cameraMoved = true;
        }

        // Get the mouse state
        var mouse = MouseState;

        if (_firstMove) // This bool variable is initially set to true.
        {
            _lastPos = new Vector2(mouse.X, mouse.Y);
            _firstMove = false;
        }
        else {
            // Calculate the offset of the mouse position
            var deltaX = mouse.X - _lastPos.X;
            var deltaY = mouse.Y - _lastPos.Y;
            _lastPos = new Vector2(mouse.X, mouse.Y);
            if (deltaX != 0 || deltaY != 0) cameraMoved = true;

            // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
            _camera.Yaw += deltaX * sensitivity;
            _camera.Pitch -= deltaY * sensitivity; // Reversed since y-coordinates range from bottom to top
        }

        if (cameraMoved || _frameNumber == 0) {
            GL.NamedBufferSubData(_basicDataUbo, (IntPtr)(Vector4.SizeInBytes * 4), Vector4.SizeInBytes * 4,
                _camera.GetViewMatrix().Inverted());
            GL.NamedBufferSubData(_basicDataUbo, (IntPtr)(Vector4.SizeInBytes * 8), Vector4.SizeInBytes,
                new Vector4(_camera.Position));
            _frameNumber = 0;
        }
    }
}