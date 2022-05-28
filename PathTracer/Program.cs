// This line creates a new instance, and wraps the instance in a using statement so it's automatically disposed once we've exited the block.

using CommandLine;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using PathTracer;

Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs()).WithParsed(o => {
    Console.WriteLine(o.SceneFile);
    var gameWindowSettings = new GameWindowSettings();
    gameWindowSettings.RenderFrequency = 144;
    gameWindowSettings.UpdateFrequency = 144;

    var nativeWindowSettings = new NativeWindowSettings();
    var windowSize = new Vector2i(960, 540);
    windowSize /= 1;
    nativeWindowSettings.Size = windowSize;
    nativeWindowSettings.Title = "Pathtracer?";
    nativeWindowSettings.Flags = ContextFlags.Debug;
    using (var game = new Game(gameWindowSettings, nativeWindowSettings, o.RayDepth, o.SceneFile)) {
        //Run takes a double, which is how many frames per second it should strive to reach.
        //You can leave that out and it'll just update as fast as the hardware will allow it.
        game.Run();
    }
});

public class Options {
    [Option('s', "scene", Required = false, HelpText = "Scene file to load")]
    public string SceneFile { get; set; }

    [Option('d', "depth", Required = false, HelpText = "Intial ray depth")]
    public int RayDepth { get; set; }
}
