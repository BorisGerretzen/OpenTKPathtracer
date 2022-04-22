using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace PathTracer;

public class Texture : IDisposable {
    public readonly TextureHandle Handle;
    private TextureTarget _textureTarget;

    public Texture(TextureTarget textureTarget) {
        Handle = GL.CreateTexture(textureTarget);
        _textureTarget = textureTarget;
    }

    public void Dispose() { }

    public void SetFilters(TextureMinFilter minFilter, TextureMagFilter magFilter) {
        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int)magFilter);
    }

    public void Bind() {
        GL.BindTexture(TextureTarget.Texture2d, Handle);
    }
}