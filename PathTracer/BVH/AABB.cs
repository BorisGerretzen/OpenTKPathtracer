using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer.BVH;

public class AABB : Uploadable {
    public readonly Vector3 Min;
    public readonly Vector3 Max;

    public float LengthX => Max.X - Min.X;
    public float LengthY => Max.Y - Min.Y;
    public float LengthZ => Max.Z - Min.Z;

    public Axis GetLongestAxis() {
        if (LengthX > LengthY && LengthX > LengthZ) return Axis.X;
        if (LengthY > LengthX && LengthY > LengthZ) return Axis.Y;
        return Axis.Z;
    }

    public override int BufferOffset => throw new NotSupportedException("Do not upload directly.");
    public static int SizeInBytes = 2 * Vector4.SizeInBytes;

    public AABB(Vector3 min, Vector3 max) {
        Min = min;
        Max = max;
    }

    public override Vector4[] GetGPUData() {
        var gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        gpuData[0].Xyz = Min;
        gpuData[1].Xyz = Max;
        return gpuData;
    }
}