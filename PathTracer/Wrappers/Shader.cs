using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace PathTracer;

public class Shader : IDisposable {
    public readonly ShaderHandle Handle;

    public Shader(string filename, ShaderType shaderType) {
        Handle = GL.CreateShader(shaderType);
        GL.ShaderSource(Handle, File.ReadAllText(filename));
        GL.CompileShader(Handle);

        Console.WriteLine($"Shader '{filename}' compiled successfully.");
    }

    public void Dispose() {
        GL.DeleteShader(Handle);
    }
}