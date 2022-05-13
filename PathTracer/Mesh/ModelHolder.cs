using ObjLoader.Loader.Loaders;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using PathTracer.BVH;
using PathTracer.Helpers;

namespace PathTracer;

public class ModelHolder {
    private readonly List<Mesh> _meshes;
    private readonly List<Vertex> _vertices;
    private readonly List<BVHNode> _bvhNodes;

    private readonly BufferHandle _vertexBufferHandle;
    private readonly BufferHandle _indicesBufferHandle;
    private readonly BufferHandle _meshBufferHandle;
    private readonly BufferHandle _bvhMetadataHandle;
    private readonly BufferHandle _bvhBufferHandle;
    
    private readonly IObjLoader _objLoader;

    private int _triangleCount;
    public ModelHolder(BufferHandle vertexBufferHandle, BufferHandle indicesBufferHandle, BufferHandle meshBufferHandle, BufferHandle bvhMetadataHandle, BufferHandle bvhBufferHandle) {
        _vertexBufferHandle = vertexBufferHandle;
        _indicesBufferHandle = indicesBufferHandle;
        _meshBufferHandle = meshBufferHandle;
        _bvhBufferHandle = bvhBufferHandle;
        _bvhMetadataHandle = bvhMetadataHandle;
        _triangleCount = 0;
        _meshes = new List<Mesh>();
        _vertices = new List<Vertex>();
        _bvhNodes = new List<BVHNode>();
        _objLoader = new ObjLoaderFactory().Create();
    }


    public void AddModel(string path, Material material, Vector3 position) {
        var fileStream = File.Open("models/teapot.obj", FileMode.Open);
        var result = _objLoader.Load(fileStream);
        var vertices = new List<Vertex>();
        var triangles = new List<Triangle>();
        var minVertex = Vector3.PositiveInfinity;
        var maxVertex = Vector3.NegativeInfinity;
        foreach (var vertex in result.Vertices) {
            var vx = vertex.X + position.X;
            var vy = vertex.Y + position.Y;
            var vz = vertex.Z + position.Z;
            if (vx < minVertex.X) minVertex.X = vx;
            if (vy < minVertex.Y) minVertex.Y = vy;
            if (vz < minVertex.Z) minVertex.Z = vz;
            if (vx > maxVertex.X) maxVertex.X = vx;
            if (vy > maxVertex.Y) maxVertex.Y = vy;
            if (vz > maxVertex.Z) maxVertex.Z = vz;
            vertices.Add(new Vertex(new Vector3(vx, vy, vz), Vector3.One, Vector2.One));
        }

        foreach (var group in result.Groups)
        foreach (var face in group.Faces) {
            var idx1 = face[0].VertexIndex - 1;
            var idx2 = face[1].VertexIndex - 1;
            var idx3 = face[2].VertexIndex - 1;
            var t = new Triangle(idx1, idx2, idx3, vertices[idx1], vertices[idx2], vertices[idx3]);
            triangles.Add(t);
        }

        AddMesh(new Mesh(vertices, triangles, material, minVertex, maxVertex));
    }

    public void AddMesh(Mesh mesh) {
        _meshes.Add(mesh);
        _vertices.AddRange(mesh.Vertices);
        _triangleCount += mesh.Triangles.Count;
        mesh.BVHIndex = _bvhNodes.Count;
        var builder = new BVHBuilder(mesh.Vertices, mesh.Triangles, BVHType.SpatialSplit);
        _bvhNodes.Add(builder.Build(255));
    }

    public void UploadModels() {
        var gpuMeshes = new Vector4[_meshes.Count * (Mesh.SizeInBytes / Vector4.SizeInBytes)];
        var gpuVertices = new Vector4[_vertices.Count * (Vertex.SizeInBytes / Vector4.SizeInBytes)];
        var gpuTriangles = new Vector4i[_triangleCount * (Triangle.SizeInBytes / Vector4.SizeInBytes)];

        var bvhNodesFlat = new List<BVHNode>();
        var triangles = new List<Triangle>();
        foreach (var node in _bvhNodes) {
            var flat = node.Flatten(bvhNodesFlat.Count, triangles.Count);
            bvhNodesFlat.AddRange(flat.Item1);
            triangles.AddRange(flat.Item2);
        }

        var gpuNodes = new Vector4[bvhNodesFlat.Count * (BVHNode.SizeInBytes / Vector4.SizeInBytes)];

        // Convert meshes into vector4 array
        for (var i = 0; i < _meshes.Count; i++) {
            var mesh = _meshes[i];
            var gpuMesh = mesh.GetGPUData();
            Array.Copy(gpuMesh, 0, gpuMeshes, i * (Mesh.SizeInBytes / Vector4.SizeInBytes), gpuMesh.Length);
        }

        // Convert vertices into vector4 array
        for (var i = 0; i < _vertices.Count; i++) {
            var vertex = _vertices[i];
            var gpuVertex = vertex.GetGPUData();
            Array.Copy(gpuVertex, 0, gpuVertices, i * (Vertex.SizeInBytes / Vector4.SizeInBytes), gpuVertex.Length);
        }

        // Convert triangles into vector4 array
        for (var i = 0; i < triangles.Count; i++) {
            var triangle = triangles[i];
            var gpuTriangle = triangle.GetGPUData();
            Array.Copy(gpuTriangle, 0, gpuTriangles, i * (Triangle.SizeInBytes / Vector4.SizeInBytes), gpuTriangle.Length);
        }

        // Convert BVHs into vector4 array
        for (var i = 0; i < bvhNodesFlat.Count; i++) {
            var node = bvhNodesFlat[i];
            var gpuNode = node.GetGPUData();
            Array.Copy(gpuNode, 0, gpuNodes, i * (BVHNode.SizeInBytes / Vector4.SizeInBytes), gpuNode.Length);
        }
        
        // Beam em up
        var meshBufferSize = Vector4.SizeInBytes * gpuMeshes.Length;
        var vertexBufferSize = Vector4.SizeInBytes * gpuVertices.Length;
        var trianglesBufferSize = Vector4.SizeInBytes * gpuTriangles.Length;
        var bvhBufferSize = Vector4.SizeInBytes * gpuNodes.Length;
        if (meshBufferSize == 0 || vertexBufferSize == 0 || trianglesBufferSize == 0 || bvhBufferSize == 0) return;
        GL.NamedBufferStorage(_meshBufferHandle, meshBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 2, _meshBufferHandle, IntPtr.Zero, meshBufferSize);
        GL.NamedBufferSubData(_meshBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuMeshes.Length, gpuMeshes);

        GL.NamedBufferStorage(_vertexBufferHandle, vertexBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 3, _vertexBufferHandle, IntPtr.Zero, vertexBufferSize);
        GL.NamedBufferSubData(_vertexBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuVertices.Length, gpuVertices);

        GL.NamedBufferStorage(_indicesBufferHandle, trianglesBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 4, _indicesBufferHandle, IntPtr.Zero, trianglesBufferSize);
        GL.NamedBufferSubData(_indicesBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuTriangles.Length, gpuTriangles);

        GL.NamedBufferStorage(_bvhMetadataHandle, Vector4.SizeInBytes, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.UniformBuffer, 5, _bvhMetadataHandle, IntPtr.Zero, Vector4.SizeInBytes);
        GL.NamedBufferSubData(_bvhMetadataHandle, IntPtr.Zero, Vector4.SizeInBytes, new Vector4(bvhNodesFlat.Count, 0, 0, 0));

        GL.NamedBufferStorage(_bvhBufferHandle, bvhBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 6, _bvhBufferHandle, IntPtr.Zero, bvhBufferSize);
        GL.NamedBufferSubData(_bvhBufferHandle, IntPtr.Zero, bvhBufferSize, gpuNodes);
    }
}