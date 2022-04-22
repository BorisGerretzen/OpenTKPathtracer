using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer;

public class Cuboid : GameObject {
    public static int SizeInBytes = Vector4.SizeInBytes * 2 + Material.SizeInBytes;
    private readonly Vector4[] _gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
    public int Instance;
    
    public Material Material;
    public Vector3 Max;
    public Vector3 Min;

    public Cuboid(Vector3 min, Vector3 max, Material material, int instance) {
        Min = min;
        Max = max;
        Material = material;
        Instance = instance;
    }

    public override int BufferOffset => 256 * Sphere.SizeInBytes + Instance * SizeInBytes;

    public override Vector4[] GetGPUData() {
        _gpuData[0].Xyz = Min;
        _gpuData[1].Xyz = Max;
        Array.Copy(Material.GetGPUData(), 0, _gpuData, 2, _gpuData.Length - 2);
        return _gpuData;
    }
}