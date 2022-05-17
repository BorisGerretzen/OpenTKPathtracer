using OpenTK.Mathematics;

namespace PathTracer.Helpers;

public class Material : Uploadable {
    #region default materials

    public static Material WhiteDiffuse = new(Vector3.One, Vector3.Zero);
    public static Material WhiteLight = new(Vector3.One, Vector3.One);
    public static Material FullSpecular = new(Vector3.One, Vector3.Zero, 1.0f);

    #endregion
    
    
    public static int SizeInBytes = Vector4.SizeInBytes * 3;

    private Vector4[] _gpuData;
    public Vector3 Albedo;
    public Vector3 Emission;

    public float IndexOfRefraction;
    public float Refractive;
    public float Specularity;

    public Material(Vector3 albedo, Vector3 emission, float specularity = 0.0f, float refractive = 0.0f, float indexOfRefraction = 0.0f) {
        Albedo = albedo;
        Emission = emission;
        Specularity = specularity;
        Refractive = refractive;
        IndexOfRefraction = indexOfRefraction;
    }

    public override int BufferOffset => throw new NotSupportedException("Do not upload directly");

    public override Vector4[] GetGPUData() {
        _gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        _gpuData[0].Xyz = Emission;
        _gpuData[0].W = Specularity;
        _gpuData[1].Xyz = Albedo;
        _gpuData[1].W = Refractive;
        _gpuData[2].X = IndexOfRefraction;
        return _gpuData;
    }
}