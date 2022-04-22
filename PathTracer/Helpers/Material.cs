using OpenTK.Mathematics;

namespace PathTracer.Helpers;

public class Material : Uploadable {
    public static int SizeInBytes = Vector4.SizeInBytes * 2;

    private readonly Vector4[] _gpuData = new Vector4[2];
    public Vector3 Albedo;
    public Vector3 Emission;
    private float metallic;
    public float Specularity;

    public Material(Vector3 albedo, Vector3 emission, float specularity = 0.0f) {
        Albedo = albedo;
        Emission = emission;
        Specularity = specularity;
    }

    public override int BufferOffset => throw new NotSupportedException("Do not upload directly");

    public override Vector4[] GetGPUData() {
        _gpuData[0].Xyz = Emission;
        _gpuData[0].W = Specularity;
        _gpuData[1].Xyz = Albedo;
        return _gpuData;
    }
}