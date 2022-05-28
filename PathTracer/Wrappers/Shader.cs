using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace PathTracer;

public class Shader : IDisposable {
    public readonly ShaderHandle Handle;

    public Shader(string filename, ShaderType shaderType) {
        Handle = GL.CreateShader(shaderType);
        GL.ShaderSource(Handle, File.ReadAllText(filename));
        GL.CompileShader(Handle);
        string info;
        GL.GetShaderInfoLog(Handle, out info);
        Console.WriteLine(info);
        if (string.IsNullOrEmpty(info))
            Console.WriteLine($"Shader '{filename}' compiled successfully.");
        else
            throw new InvalidOperationException("Shader failed to compile");
    }

    public void Dispose() {
        GL.DeleteShader(Handle);
    }
}