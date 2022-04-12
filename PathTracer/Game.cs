using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PathTracer; 

public class Game : GameWindow{
    public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings) { }

    protected override void OnUpdateFrame(FrameEventArgs args) {
        if (KeyboardState.IsKeyDown(Keys.Escape)) {
            Close();
        }
        base.OnUpdateFrame(args);
    }

    protected override void OnRenderFrame(FrameEventArgs args) {
        GL.ClearColor(1, 0, 0, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        SwapBuffers();
    }
}