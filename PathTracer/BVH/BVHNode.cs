using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer.BVH;

public abstract class BVHNode : Uploadable {
    public static int SizeInBytes = Vector4.SizeInBytes * 2 + AABB.SizeInBytes;

    #region uploaded

    public int? TriangleOffset;
    public int ParentIndex;
    public readonly int[] ChildIndices = new int[2];
    public readonly AABB BoundingBox;
    public Axis SplitAxis = Axis.None;
    #endregion

    public readonly int MaxNumTriangles;
    public readonly BVHNode[] Children = new BVHNode[2];
    public readonly List<Triangle> Triangles;


    public abstract void Split();

    public BVHNode(AABB boundingBox, int maxNumTriangles) {
        Triangles = new List<Triangle>();
        BoundingBox = boundingBox;
        MaxNumTriangles = maxNumTriangles;
    }

    public BVHNode(AABB boundingBox, BVHNode node1, BVHNode node2, int numTriangles) : this(boundingBox, numTriangles) {
        SetChildren(node1, node2);
    }

    public void AddTriangles(List<Triangle> triangles) {
        Triangles.AddRange(triangles);
    }

    public void SetTriangles(List<Triangle> triangles) {
        triangles.Clear();
        AddTriangles(triangles);
    }

    public void SetChildren(BVHNode node1, BVHNode node2) {
        Children[0] = node1;
        Children[1] = node2;
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

    public override int BufferOffset => throw new NotSupportedException("Do not upload directly.");

    public override Vector4[] GetGPUData() {
        if (Triangles.Count > MaxNumTriangles) throw new Exception("Node has too many triangles");
        if (!TriangleOffset.HasValue) throw new NullReferenceException("Some values are null, flatten the tree before uploading.");
        var gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        var AABBData = BoundingBox.GetGPUData();
        Array.Copy(AABBData, 0, gpuData, 0, AABBData.Length);
        gpuData[2].X = AxisHelpers.AxisToInt(SplitAxis);
        gpuData[2].Y = Triangles.Count;
        gpuData[2].Z = TriangleOffset.Value;
        gpuData[2].W = ParentIndex;
        gpuData[3].X = ChildIndices[0];
        gpuData[3].Y = ChildIndices[1];
        return gpuData;
    }
}