using OpenTK.Mathematics;

namespace PathTracer;

public class Triangle {
    public static int SizeInBytes => Vector4.SizeInBytes;
    public int index1;
    public int index2;
    public int index3;

    public Triangle(int index1, int index2, int index3) {
        this.index1 = index1;
        this.index2 = index2;
        this.index3 = index3;
    }

    public int BufferOffset => throw new NotSupportedException("Do not upload directly.");

    public Vector4i[] GetGPUData() {
        var gpuData = new Vector4i[1];
        gpuData[0].X = index1;
        gpuData[0].Y = index2;
        gpuData[0].Z = index3;
        return gpuData;
    }
}