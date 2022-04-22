using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace PathTracer;

public class ShaderProgram : IDisposable {
    public readonly ProgramHandle Handle;

    public ShaderProgram(List<Shader> shaders) {
        Handle = GL.CreateProgram();
        shaders.ForEach(shader => GL.AttachShader(Handle, shader.Handle));
        GL.LinkProgram(Handle);
        shaders.ForEach(shader => GL.DetachShader(Handle, shader.Handle));
        shaders.ForEach(shader => shader.Dispose());
    }

    public void Dispose() {
        GL.DeleteProgram(Handle);
    }

    public void Use() {
        GL.UseProgram(Handle);
    }

    public void SetUniformUInt(int location, uint value) {
        GL.ProgramUniform1ui(Handle, location, value);
    }

    public unsafe void SetUniformVec2(int location, Vector2 vector2) {
        var vec = new float[2];
        vec[0] = vector2.X;
        vec[1] = vector2.Y;
        fixed (float* ptr = &vec[0]) {
            GL.ProgramUniform2fv(Handle, location, 1, ptr);
        }
    }
}