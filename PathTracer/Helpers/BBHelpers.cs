using OpenTK.Mathematics;
using PathTracer.BVH;

namespace PathTracer.Helpers;

public static class BBHelpers {
    public delegate Vector3 VectorTransformation(Vector3 vector, Vector3 vector2);

    public static AABB AABBFromVertices(List<Vertex> vertices, VectorTransformation transformation) {
        var min = Vector3.PositiveInfinity;
        var max = Vector3.NegativeInfinity;

        foreach (var vertex in vertices) {
            if (vertex.Position.X < min.X) min.X = vertex.Position.X;
            if (vertex.Position.Y < min.Y) min.Y = vertex.Position.Y;
            if (vertex.Position.Z < min.Z) min.Z = vertex.Position.Z;
            if (vertex.Position.X > max.X) max.X = vertex.Position.X;
            if (vertex.Position.Y > max.Y) max.Y = vertex.Position.Y;
            if (vertex.Position.Z > max.Z) max.Z = vertex.Position.Z;
        }

        return new AABB(min, max);
    }

    public static AABB AABBFromVertices(List<Vertex> vertices) {
        return AABBFromVertices(vertices, (vector, _) => vector);
    }
}