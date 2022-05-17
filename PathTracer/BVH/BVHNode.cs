using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer.BVH;

public abstract class BVHNode {
    public static int SizeInBytes = Vector4.SizeInBytes * 1 + AABB.SizeInBytes;

    #region uploaded
    public int? TriangleOffset;
    public int ParentIndex;
    public readonly int[] ChildIndices = new int[2];
    public AABB BoundingBox;
    public Axis SplitAxis = Axis.None;
    #endregion

    public readonly int MaxNumTriangles;
    public readonly BVHNode[] Children = new BVHNode[2];
    public readonly List<Triangle> Triangles;

    /// <summary>
    ///     Splits the current node into child nodes if needed according to the max number of triangles.
    /// </summary>
    public abstract void Split();

    /// <summary>
    ///     Create a new bhv node from a list of triangles.
    /// </summary>
    /// <param name="maxNumTriangles">Max number of triangles allowed in a leaf node</param>
    /// <param name="triangles">List of triangles</param>
    public BVHNode(int maxNumTriangles, List<Triangle> triangles) {
        Triangles = new List<Triangle>();
        Triangles.AddRange(triangles);
        MaxNumTriangles = maxNumTriangles;
        UpdateBounds();
    }

    /// <summary>
    ///     Sets the children of this node.
    /// </summary>
    /// <param name="node1">Left child</param>
    /// <param name="node2">Right child</param>
    public void SetChildren(BVHNode node1, BVHNode node2) {
        Children[0] = node1;
        Children[1] = node2;
    }

    /// <summary>
    ///     Updates the bounds of this node according to the triangles stored in it.
    /// </summary>
    private void UpdateBounds() {
        var vertices = Triangles.SelectMany(triangle => triangle.Vertices).ToList();
        BoundingBox = BBHelpers.AABBFromVertices(vertices);
    }
    
    /// <summary>
    ///     Flattens the current tree and build the indices so the nodes can reference each other without direct references
    ///     Needed for sending to gpu cause it cant handle stacks/recursion
    /// </summary>
    /// <param name="nodeOffset">Offset of the first node in the final array</param>
    /// <param name="triangleOffset">Offset of the triangles in the final array</param>
    public (List<BVHNode>, List<Triangle>) Flatten(int nodeOffset, int triangleOffset) {
        var todo = new Queue<BVHNode>();
        var nodes = new List<BVHNode>();
        var triangles = new List<Triangle>();
        
        todo.Enqueue(this);
        var currentIndex = nodeOffset;
        while (todo.Count > 0) {
            var currentNode = todo.Dequeue();
            // If node is a leaf node but has no triangles, set children to -1
            if (currentNode.Triangles.Count == 0 && currentNode.Children[0] == null && currentNode.Children[1] == null) {
                currentNode.ChildIndices[0] = -1;
                currentNode.ChildIndices[1] = -1;
            }

            // If node is not a leaf node set child indices and add children to queue
            if (currentNode.Triangles.Count == 0 && currentNode.Children[0] != null && currentNode.Children[1] != null) {
                currentNode.ChildIndices[0] = currentIndex + todo.Count + 1;
                currentNode.Children[0].ParentIndex = currentIndex;
                currentNode.ChildIndices[1] = currentIndex + todo.Count + 2;
                currentNode.Children[1].ParentIndex = currentIndex;
                todo.Enqueue(currentNode.Children[0]);
                todo.Enqueue(currentNode.Children[1]);
            }
            
            currentNode.TriangleOffset = triangleOffset + triangles.Count;

            nodes.Add(currentNode);
            triangles.AddRange(currentNode.Triangles);
            currentIndex++;
        }

        return (nodes, triangles);
    }

    public int BufferOffset => throw new NotSupportedException("Do not upload directly.");

    public Vector4[] GetGPUData() {
        if (Triangles.Count > MaxNumTriangles) throw new Exception("Node has too many triangles");
        if (!TriangleOffset.HasValue) throw new NullReferenceException("Some values are null, flatten the tree before uploading.");
        var gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        var AABBData = BoundingBox.GetGPUData();
        Array.Copy(AABBData, 0, gpuData, 0, AABBData.Length);
        gpuData[2].X = AxisHelpers.AxisToInt(SplitAxis);
        gpuData[2].Y = Triangles.Count;
        gpuData[2].Z = TriangleOffset.Value;
        gpuData[2].W = ParentIndex;
        gpuData[0].W = ChildIndices[0];
        gpuData[1].W = ChildIndices[1];
        return gpuData;
    }
}