using System.ComponentModel;

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

    /// <summary>
    ///     Builds a BVH from the triangles in the object.
    /// </summary>
    /// <param name="numLeafTriangles">Max number of triangles in the leaf nodes</param>
    /// <returns>The root node of the tree</returns>
    /// <exception cref="InvalidEnumArgumentException">If invalid bvhType is specified in the constructor</exception>
    public BVHNode Build(int numLeafTriangles) {
        BVHNode root;

        if (BvhType == BVHType.SpatialSplit)
            root = new BVHNodeSpatialSplit(numLeafTriangles, Triangles);
        else
            throw new InvalidEnumArgumentException("Invalid enum value specified");
        // Depth first BVH construction
        var todo = new Stack<BVHNode>();
        todo.Push(root);
        while (todo.Count > 0) {
            var currentNode = todo.Pop();

            // No split needed if triangle count low enough
            if (currentNode.Triangles.Count <= currentNode.MaxNumTriangles) continue;

            currentNode.Split();
            todo.Push(currentNode.Children[0]);
            todo.Push(currentNode.Children[1]);
        }

        return root;
    }
}