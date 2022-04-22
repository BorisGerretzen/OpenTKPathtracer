using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer;

public class Cuboid : GameObject {
    public static int SizeInBytes = Vector3.SizeInBytes * 2 + Material.SizeInBytes;
    public Material Material;
    public Vector3 Max;

    public Vector3 Min;

    public Cuboid(Vector3 min, Vector3 max, Material material) {
        Min = min;
        Max = max;
        Material = material;
    }

    public override int BufferOffset => throw new NotImplementedException();

    public override Vector4[] GetGPUData() {
        throw new NotImplementedException();
    }
}