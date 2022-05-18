using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer;

[DataContract]
public class Sphere : GameObject {
    public static int SizeInBytes = Vector3.SizeInBytes + 4 + Material.SizeInBytes;
    private readonly Vector4[] _gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];

    [DataMember] public int Instance { get; set; }
    [DataMember] public Material Material { get; set; }
    [DataMember] public Vector3 Position { get; set; }
    [DataMember] public float Radius { get; set; }

    public Sphere(Vector3 position, float radius, Material material, int instance) {
        Position = position;
        Radius = radius;
        Material = material;
        Instance = instance;
    }

    private Sphere() { }
    [JsonIgnore] public override int BufferOffset => Instance * SizeInBytes;

    public override Vector4[] GetGPUData() {
        _gpuData[0].Xyz = Position;
        _gpuData[0].W = Radius;
        Array.Copy(Material.GetGPUData(), 0, _gpuData, 1, _gpuData.Length - 1);
        return _gpuData;
    }
}