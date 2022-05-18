using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer;

[DataContract]
public class Cuboid : GameObject {
    public static int SizeInBytes = Vector4.SizeInBytes * 2 + Material.SizeInBytes;
    private readonly Vector4[] _gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];

    [DataMember] public int Instance { get; set; }
    [DataMember] public Material Material { get; set; }
    [DataMember] public Vector3 Max { get; set; }
    [DataMember] public Vector3 Min { get; set; }

    public Cuboid(Vector3 min, Vector3 max, Material material, int instance) {
        Min = min;
        Max = max;
        Material = material;
        Instance = instance;
    }

    private Cuboid() { }
    public Cuboid(Vector3 pos, float size, Material material, int instance) : this(new Vector3(pos.X - size / 2, pos.Y - size / 2, pos.Z - size / 2),
        new Vector3(pos.X + size / 2, pos.Y + size / 2, pos.Z + size / 2), material, instance) { }

    [JsonIgnore] public override int BufferOffset => 256 * Sphere.SizeInBytes + Instance * SizeInBytes;

    public override Vector4[] GetGPUData() {
        _gpuData[0].Xyz = Min;
        _gpuData[1].Xyz = Max;
        Array.Copy(Material.GetGPUData(), 0, _gpuData, 2, _gpuData.Length - 2);
        return _gpuData;
    }
}