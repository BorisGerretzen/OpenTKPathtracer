using System.Runtime.Serialization;

namespace PathTracer;

[DataContract]
public class ShaderStructure {
    #region Templates

    public static ShaderStructure Default = new(@"Assets\Shaders",
        "defines.glsl",
        "inputs.glsl",
        "helpers.glsl",
        "intersections.glsl",
        "brdf.glsl",
        "main.glsl");

    public static ShaderStructure DebugNee = new(@"Assets\Shaders",
        "defines.glsl",
        "inputs.glsl",
        "helpers.glsl",
        "intersections.glsl",
        "brdf.glsl",
        @"debug_nee\main.glsl");

    #endregion

    [DataMember] public List<string> ShaderFragments { get; set; }

    private ShaderStructure() {
        ShaderFragments = new List<string>();
    }

    public ShaderStructure(string baseDirectory, params string[] fragments) {
        ShaderFragments = fragments.Select(file => $@"{baseDirectory}\{file}").ToList();
    }
}