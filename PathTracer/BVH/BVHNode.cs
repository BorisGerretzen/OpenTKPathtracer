using OpenTK.Mathematics;

namespace PathTracer.BVH;

public abstract class BVHNode : Uploadable {
    public static int SizeInBytes = Vector4.SizeInBytes * (3 + 255) + AABB.SizeInBytes;

    #region uploaded
    public readonly int NumTriangles;
    public int TriangleOffset;
    public int ParentIndex;
    public readonly int[] ChildIndices = new int[2];
    public AABB BoundingBox;
    #endregion

    public readonly BVHNode[] Children = new BVHNode[2];
    public readonly List<Triangle> Triangles;


    public abstract void Split();

    public BVHNode(AABB boundingBox, int numTriangles) {
        Triangles = new List<Triangle>();
        BoundingBox = boundingBox;
        NumTriangles = numTriangles;
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
    /// <param name="offset">Offset index</param>
    public List<BVHNode> Flatten(int offset) {
        var todo = new Queue<BVHNode>();
        var nodes = new List<BVHNode>();
        todo.Enqueue(this);
        var currentIndex = offset;

        while (todo.Count > 0) {
            var currentNode = todo.Dequeue();
            currentNode.ChildIndices[0] = currentIndex + todo.Count + 1;
            currentNode.Children[0].ParentIndex = currentIndex;
            currentNode.ChildIndices[1] = currentIndex + todo.Count + 2;
            currentNode.Children[1].ParentIndex = currentIndex;
            nodes.Add(currentNode);
            todo.Enqueue(currentNode.Children[0]);
            todo.Enqueue(currentNode.Children[1]);
            currentIndex++;
        }

        return nodes;
    }

    public override int BufferOffset => throw new NotSupportedException("Do not upload directly.");

    public override Vector4[] GetGPUData() {
        if (Triangles.Count > NumTriangles) throw new Exception("Node has too many triangles");
        var gpuData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        var AABBData = BoundingBox.GetGPUData();
        Array.Copy(AABBData, 0, gpuData, 0, AABBData.Length);
        gpuData[0].W = ChildIndices[0];
        gpuData[1].W = ChildIndices[1];
        
        return gpuData;
    }
}