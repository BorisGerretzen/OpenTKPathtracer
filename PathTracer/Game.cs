using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using PathTracer.Helpers;


namespace PathTracer;

public class Game : GameWindow {
    public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings) { }
    
    private Camera _camera;
    private bool _firstMove;
    private Vector2 _lastPos;


    public readonly List<GameObject> GameObjects = new();
    protected override void OnLoad() {
        base.OnLoad();
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        GL.Enable(EnableCap.DepthTest);
        
        _camera = new Camera(Vector3.UnitZ * 3, Size.X / (float)Size.Y);
        CursorGrabbed = true;
    }

    protected override void OnResize(ResizeEventArgs e) {
        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.AspectRatio = e.Width / (float)e.Height;
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);


        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs e) {
        base.OnUpdateFrame(e);

        if (!IsFocused) // Check to see if the window is focused
        {
            return;
        }

        var input = KeyboardState;

        if (input.IsKeyDown(Keys.Escape)) {
            Close();
        }

        const float cameraSpeed = 1.5f;
        const float sensitivity = 0.2f;

        if (input.IsKeyDown(Keys.W)) {
            _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
        }

        if (input.IsKeyDown(Keys.S)) {
            _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
        }

        if (input.IsKeyDown(Keys.A)) {
            _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
        }

        if (input.IsKeyDown(Keys.D)) {
            _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
        }

        if (input.IsKeyDown(Keys.Space)) {
            _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
        }

        if (input.IsKeyDown(Keys.LeftShift)) {
            _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
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

            // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
            _camera.Yaw += deltaX * sensitivity;
            _camera.Pitch -= deltaY * sensitivity; // Reversed since y-coordinates range from bottom to top
        }
    }
}