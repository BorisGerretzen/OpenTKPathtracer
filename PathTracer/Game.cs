using System.Runtime.InteropServices;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PathTracer.Helpers;

namespace PathTracer;

public class Game : GameWindow {
    private readonly int _maxCuboids = 64;

    private readonly int _maxSpheres = 256;
    public readonly List<GameObject> GameObjects = new();

    private BufferHandle _basicDataUbo;
    private Camera _camera;
    private bool _firstMove;
    private FramebufferHandle _framebufferHandle;
    private BufferHandle _gameObjectsUbo;
    private Vector2 _lastPos;
    private ShaderProgram _shaderProgram;
    private TextureHandle _textureHandle;
    private Vector2i _windowSize;
    private int frameNumber;

    public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings) {
        _windowSize = new Vector2i(0);
        _windowSize.X = nativeWindowSettings.Size.X;
        _windowSize.Y = nativeWindowSettings.Size.Y;
    }

    private static void OpenGlDebugCallback(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam) {
        var errorString = source == DebugSource.DebugSourceApplication
            ? $"OpenGL - {Marshal.PtrToStringAnsi(message, length)}"
            : $"OpenGL - {Marshal.PtrToStringAnsi(message, length)}\n\tid:{id} severity:{severity} type:{type} source:{source}\n";
        if (severity == DebugSeverity.DebugSeverityHigh) throw new Exception(errorString);
        Console.WriteLine(errorString);
    }

    protected override void OnLoad() {
        base.OnLoad();

        // Enable debugging
        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);
        GL.DebugMessageCallback(OpenGlDebugCallback, IntPtr.Zero);
        GL.DebugMessageInsert(DebugSource.DebugSourceApplication, DebugType.DebugTypeMarker, 0, DebugSeverity.DebugSeverityNotification, -1, "Debug callback initialized");

        // Create basic data UBO
        GL.CreateBuffer(out _basicDataUbo);
        GL.NamedBufferStorage(_basicDataUbo, Vector4.SizeInBytes * 4 * 2 + Vector4.SizeInBytes, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.UniformBuffer, 0, _basicDataUbo, IntPtr.Zero, Vector4.SizeInBytes * 4 * 2 + Vector4.SizeInBytes);

        // Create GameObjects UBO
        GL.CreateBuffer(out _gameObjectsUbo);
        GL.NamedBufferStorage(_gameObjectsUbo, _maxSpheres * Sphere.SizeInBytes + _maxCuboids * Cuboid.SizeInBytes, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.UniformBuffer, 1, _gameObjectsUbo, IntPtr.Zero, _maxSpheres * Sphere.SizeInBytes + _maxCuboids * Cuboid.SizeInBytes);

        // Create texture to render to
        _textureHandle = GL.CreateTexture(TextureTarget.Texture2d);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, _textureHandle);
        GL.TextureParameteri(_textureHandle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2d, 0, (int)InternalFormat.Rgba32f, _windowSize.X, _windowSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

        // Create framebuffer to display rendered frame
        _framebufferHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferHandle);
        GL.NamedFramebufferTexture(_framebufferHandle, FramebufferAttachment.ColorAttachment0, _textureHandle, 0);
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _framebufferHandle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, new FramebufferHandle(0));

        // Load compute shader
        _shaderProgram = new ShaderProgram(new List<Shader> { new("Shader/pathtracer.compute", ShaderType.ComputeShader) });
        _shaderProgram.Use();

        // Spawn objects
        CreateScene();

        // Spawn camera
        _camera = new Camera(Vector3.Zero, Size.X / (float)Size.Y);
        CursorGrabbed = true;
    }

    private void CreateScene() {
        var instance = 0;
        var blueDiffuse = new Material(new Vector3(0.337f, 0.368f, 0.674f), new Vector3(0));
        var whiteLight = new Material(new Vector3(0.04f), new Vector3(0.2f, 0.945f, 0.2f) * 20.0f);

        GameObjects.Add(new Sphere(new Vector3(0, 0, 4), 1, blueDiffuse, instance++));
        GameObjects.Add(new Sphere(new Vector3(0, 0, 0), 1, whiteLight, instance++));

        foreach (var gameObject in GameObjects) gameObject.Upload(_gameObjectsUbo);
    }

    protected override void OnResize(ResizeEventArgs e) {
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.AspectRatio = e.Width / (float)e.Height;
        _windowSize.X = e.Width;
        _windowSize.Y = e.Height;
        GL.NamedBufferSubData(_basicDataUbo, (IntPtr)0, Vector4.SizeInBytes * 4, _camera.GetProjectionMatrix().Inverted());
        GL.TexImage2D(TextureTarget.Texture2d, 0, (int)InternalFormat.Rgba32f, _windowSize.X, _windowSize.Y, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
    }


    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        _shaderProgram.SetUniformInt(0, frameNumber++);
        _shaderProgram.SetUniformVec2(1, new Vector2(2, 0));

        GL.BindTexture(TextureTarget.Texture2d, _textureHandle);
        GL.BindImageTexture(0, _textureHandle, 0, 0, 0, BufferAccessARB.ReadWrite, InternalFormat.Rgba32f);
        GL.DispatchCompute((uint)(_windowSize.X + 8 - 1) / 8, (uint)(_windowSize.Y + 4 - 1) / 4, 1);
        GL.MemoryBarrier(MemoryBarrierMask.TextureFetchBarrierBit);
        GL.BlitFramebuffer(0, 0, _windowSize.X, _windowSize.Y, 0, 0, _windowSize.X, _windowSize.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e) {
        base.OnUpdateFrame(e);
        var cameraMoved = false;

        if (!IsFocused) // Check to see if the window is focused
            return;

        var input = KeyboardState;

        if (input.IsKeyDown(Keys.Escape)) Close();

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

        if (cameraMoved || frameNumber == 0) {
            GL.NamedBufferSubData(_basicDataUbo, (IntPtr)(Vector4.SizeInBytes * 4), Vector4.SizeInBytes * 4, _camera.GetViewMatrix().Inverted());
            GL.NamedBufferSubData(_basicDataUbo, (IntPtr)(Vector4.SizeInBytes * 8), Vector4.SizeInBytes, _camera.Position);
            frameNumber = 0;
            Console.WriteLine(_camera.GetViewMatrix().Inverted());
            Console.WriteLine("--");
            Console.WriteLine(_camera.GetProjectionMatrix().Inverted());
        }
    }
}