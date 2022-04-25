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

    public static TextureTarget CubeMapTextureTargetFromString(string name) {
        var pos = name.Contains("POS");

        if (name.Contains("_X")) {
            return pos ? TextureTarget.TextureCubeMapPositiveX : TextureTarget.TextureCubeMapNegativeX;
        }

        if (name.Contains("_Y")) {
            return pos ? TextureTarget.TextureCubeMapPositiveY : TextureTarget.TextureCubeMapNegativeY;
        }

        if (name.Contains("_Z")) {
            return pos ? TextureTarget.TextureCubeMapPositiveZ : TextureTarget.TextureCubeMapNegativeZ;
        }

        throw new ArgumentException("Invalid name format");
    }
}