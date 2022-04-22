// This line creates a new instance, and wraps the instance in a using statement so it's automatically disposed once we've exited the block.

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using PathTracer;

var gameWindowSettings = new GameWindowSettings();
gameWindowSettings.RenderFrequency = 60;
gameWindowSettings.UpdateFrequency = 60;

var nativeWindowSettings = new NativeWindowSettings();
nativeWindowSettings.Size = new Vector2i(960, 540);
nativeWindowSettings.Title = "Pathtracer?";
nativeWindowSettings.Flags = ContextFlags.Debug;
using (var game = new Game(gameWindowSettings, nativeWindowSettings)) {
    //Run takes a double, which is how many frames per second it should strive to reach.
    //You can leave that out and it'll just update as fast as the hardware will allow it.
    game.Run();
}