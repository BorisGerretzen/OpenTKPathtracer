using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using OpenTK.Mathematics;

namespace PathTracer.Helpers;

[DataContract]
public class Material : Uploadable {
    #region default materials
    public static Material WhiteDiffuse = new(Vector3.One, Vector3.Zero);
    public static Material WhiteLight = new(Vector3.One, Vector3.One);
    public static Material FullSpecular = new(Vector3.One, Vector3.Zero, 1.0f);
    public static Material Glass = new(Vector3.One, Vector3.Zero, 0f, 1f, 1.2f);
    public static Material Glossy = new(Vector3.One, Vector3.Zero, 0.3f);
    public static Material BlueDiffuse = new(new Vector3(0.3f, 0.3f, 1), new Vector3(0));
    public static Material RedDiffuse = new(new Vector3(1f, 0.3f, 0.3f), new Vector3(0));
    public static Material PurpleDiffuse = new(Color.FromHex(0xEE00EE), new Vector3(0));
    public static Material YellowDiffuse = new(Color.FromHex(0xEEEE00), new Vector3(0));

    #endregion
    
    public static int SizeInBytes = Vector4.SizeInBytes * 3;
    private Vector4[] _gpuData;

    [DataMember] public Vector3 Albedo;
    [DataMember] public Vector3 Emission;
    [DataMember] public float IndexOfRefraction;
    [DataMember] public float Refractive;
    [DataMember] public float Specularity;

    public Material(Vector3 albedo, Vector3 emission, float specularity = 0.0f, float refractive = 0.0f, float indexOfRefraction = 0.0f) {
        Albedo = albedo;
        Emission = emission;
        Specularity = specularity;
        Refractive = refractive;
        IndexOfRefraction = indexOfRefraction;
    }

    private Material() { }
    [JsonIgnore] public override int BufferOffset => throw new NotSupportedException("Do not upload directly");

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