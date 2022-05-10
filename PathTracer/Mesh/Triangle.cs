using OpenTK.Mathematics;

namespace PathTracer;

public class Triangle {
    public static int SizeInBytes => Vector4.SizeInBytes;
    public int index1;
    public int index2;
    public int index3;

    public Vector3 Center;
    public Vertex Vertex1;
    public Vertex Vertex2;
    public Vertex Vertex3;

    public Triangle(int index1, int index2, int index3, Vertex vertex1, Vertex vertex2, Vertex vertex3) {
        this.index1 = index1;
        this.index2 = index2;
        this.index3 = index3;
        Vertex1 = vertex1;
        Vertex2 = vertex2;
        Vertex3 = vertex3;
        Center = (Vertex1.Position + Vertex2.Position + Vertex3.Position) / 3.0f;
    }

    public int BufferOffset => throw new NotSupportedException("Do not upload directly.");

    public Vector4[] GetGPUData() {
        var gpuData = new Vector4[1];
        gpuData[0].X = index1;
        gpuData[0].Y = index2;
        gpuData[0].Z = index3;
        return gpuData;
    }
}