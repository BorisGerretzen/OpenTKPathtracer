using System.Text;

namespace PathTracer;

public class ShaderCode {
    private readonly ShaderStructure _shaderStructure;

    public ShaderCode(ShaderStructure shaderStructure) {
        _shaderStructure = shaderStructure;
    }

    public string Build() {
        var sb = new StringBuilder();
        _shaderStructure.ShaderFragments.ForEach(path => sb.Append(File.ReadAllText(path)).Append("\n"));
        return sb.ToString();
    }
}