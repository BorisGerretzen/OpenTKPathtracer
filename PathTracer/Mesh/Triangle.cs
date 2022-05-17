using OpenTK.Mathematics;

namespace PathTracer;

public class Triangle {
    public static int SizeInBytes => Vector4.SizeInBytes;
    public List<int> Indices = new(3);
    public List<Vertex> Vertices = new(3);
    public Vector3 Center;

    public Triangle(int index1, int index2, int index3, Vertex vertex1, Vertex vertex2, Vertex vertex3) {
        Indices.Add(index1);
        Indices.Add(index2);
        Indices.Add(index3);
        Vertices.Add(vertex1);
        Vertices.Add(vertex2);
        Vertices.Add(vertex3);
        Center = (vertex1.Position + vertex2.Position + vertex3.Position) / 3.0f;
    }

    public Vector4i[] GetGPUData() {
        var gpuData = new Vector4i[1];
        gpuData[0].X = Indices[0];
        gpuData[0].Y = Indices[1];
        gpuData[0].Z = Indices[2];
        return gpuData;
    }
}