using OpenTK.Mathematics;

namespace PathTracer.Helpers;

public class Material : Uploadable {
    public static int SizeInBytes = Vector4.SizeInBytes * 2;

    private readonly Vector4[] _gpuData = new Vector4[2];
    public Vector3 Albedo;
    public Vector3 Emission;
    private float metallic;
    private float specularity;

    public Material(Vector3 albedo, Vector3 emission) {
        Albedo = albedo;
        Emission = emission;
    }

    public override int BufferOffset => throw new NotSupportedException("Do not upload directly");

    public override Vector4[] GetGPUData() {
        _gpuData[0].Xyz = Emission;
        _gpuData[1].Xyz = Albedo;
        return _gpuData;
    }
}