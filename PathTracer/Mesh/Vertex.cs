using OpenTK.Mathematics;

namespace PathTracer;

public class Vertex : Uploadable {
    public static int SizeInBytes => 3 * Vector4.SizeInBytes;
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TextureCoordinates;

    public Vertex(Vector3 position, Vector3 normal, Vector2 textureCoordinates) {
        Position = position;
        Normal = normal;
        TextureCoordinates = textureCoordinates;
    }

    public override int BufferOffset => throw new NotSupportedException("Do not upload directly.");

    public override Vector4[] GetGPUData() {
        var gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        gpuData[0].Xyz = Position;
        gpuData[1].Xyz = Normal;
        gpuData[2].Xy = TextureCoordinates;
        return gpuData;
    }
}