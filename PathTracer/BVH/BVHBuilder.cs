using System.ComponentModel;
using PathTracer.Helpers;

namespace PathTracer.BVH;

public class BVHBuilder {
    public readonly List<Vertex> Vertices;
    public readonly List<Triangle> Triangles;
    public readonly BVHType BvhType;

    public BVHBuilder(List<Vertex> vertices, List<Triangle> triangles, BVHType bvhType) {
        Vertices = vertices;
        Triangles = triangles;
        BvhType = bvhType;
    }

    public BVHNode Build(int numLeafTriangles) {
        BVHNode root;

        if (BvhType == BVHType.SpatialSplit)
            root = new BVHNodeSpatialSplit(BBHelpers.AABBFromVertices(Vertices), numLeafTriangles);
        else
            throw new InvalidEnumArgumentException("Invalid enum value specified");
        root.AddTriangles(Triangles);
        // Depth first BVH construction
        var todo = new Stack<BVHNode>();
        todo.Push(root);
        while (todo.Count > 0) {
            var currentNode = todo.Pop();

            // No split needed if triangle count low enough
            if (currentNode.Triangles.Count <= currentNode.NumTriangles) continue;

            currentNode.Split();
            todo.Push(currentNode.Children[0]);
            todo.Push(currentNode.Children[1]);
        }

        return root;
    }
}