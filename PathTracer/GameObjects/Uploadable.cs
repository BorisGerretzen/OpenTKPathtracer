using System.Text.Json.Serialization;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace PathTracer;

public abstract class Uploadable {
    [JsonIgnore] public abstract int BufferOffset { get; }
    public abstract Vector4[] GetGPUData();

    public void Upload(BufferHandle handle) {
        var data = GetGPUData();
        GL.NamedBufferSubData(handle, (IntPtr)BufferOffset, Vector4.SizeInBytes * data.Length, data);
    }
}