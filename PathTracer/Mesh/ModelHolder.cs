using ObjLoader.Loader.Loaders;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using PathTracer.BVH;
using PathTracer.Helpers;
using PathTracer.Scene;

namespace PathTracer;

public class ModelHolder {
    private readonly List<Mesh> _meshes;
    private readonly List<Triangle> _triangles;
    private readonly List<Vertex> _vertices;
    private readonly List<BVHNode> _bvhNodes;

    private readonly BufferHandle _vertexBufferHandle;
    private readonly BufferHandle _trianglesBufferHandle;
    private readonly BufferHandle _meshBufferHandle;
    private readonly BufferHandle _bvhMetadataHandle;
    private readonly BufferHandle _bvhBufferHandle;

    private readonly ObjLoaderFactory _objLoaderFactory;
    
    /// <summary>
    ///     Creates a new model holder
    ///     This object holds all models in the scene and transfers them into buffers on the gpu
    /// </summary>
    /// <param name="vertexBufferHandle">Vertex buffer handle</param>
    /// <param name="trianglesBufferHandle">Triangles buffer handle</param>
    /// <param name="meshBufferHandle">Mesh buffer handle</param>
    /// <param name="bvhMetadataHandle">BVH Metadata buffer handle</param>
    /// <param name="bvhBufferHandle">BVH buffer handle</param>
    public ModelHolder(BufferHandle vertexBufferHandle, BufferHandle trianglesBufferHandle, BufferHandle meshBufferHandle, BufferHandle bvhMetadataHandle, BufferHandle bvhBufferHandle) {
        _vertexBufferHandle = vertexBufferHandle;
        _trianglesBufferHandle = trianglesBufferHandle;
        _meshBufferHandle = meshBufferHandle;
        _bvhBufferHandle = bvhBufferHandle;
        _bvhMetadataHandle = bvhMetadataHandle;
        _meshes = new List<Mesh>();
        _vertices = new List<Vertex>();
        _bvhNodes = new List<BVHNode>();
        _triangles = new List<Triangle>();
        _objLoaderFactory = new ObjLoaderFactory();
    }

    /// <summary>
    ///     Loads the vertices and triangles from a Waveform .obj file.
    /// </summary>
    /// <param name="path">Path to the file</param>
    /// <param name="material">Material of the mesh</param>
    /// <param name="position">Position in the world</param>
    /// <param name="scale">Scale of the model</param>
    public void AddModel(string path, Material material, Vector3 position, Vector3 scale) {
        LoadResult result;
        using (var fileStream = File.Open(path, FileMode.Open)) {
            result = _objLoaderFactory.Create().Load(fileStream);
        }

        var indicesOffset = _vertices.Count;
        var vertices = new List<Vertex>();
        var triangles = new List<Triangle>();

        // Add vertices and apply object position
        foreach (var vertex in result.Vertices) {
            vertices.Add(new Vertex(
                new Vector3(
                    vertex.X * scale.X + position.X,
                    vertex.Y * scale.Y + position.Y,
                    vertex.Z * scale.Y + position.Z),
                Vector3.One,
                Vector2.One));
        }

        // Add triangles
        foreach (var group in result.Groups)
        foreach (var face in group.Faces) {
            var idx1 = face[0].VertexIndex - 1;
            var idx2 = face[1].VertexIndex - 1;
            var idx3 = face[2].VertexIndex - 1;
            var t = new Triangle(idx1 + indicesOffset, idx2 + indicesOffset, idx3 + indicesOffset, vertices[idx1], vertices[idx2], vertices[idx3]);
            triangles.Add(t);
        }

        AddMesh(new Mesh(vertices, triangles, material));
    }

    public void AddModel(SerializableMesh serializableMesh) {
        AddModel(serializableMesh.Path, serializableMesh.Material, serializableMesh.Position, serializableMesh.Scale);
    }
    
    /// <summary>
    ///     Adds a Mesh to the MeshHolder.
    /// </summary>
    /// <param name="mesh">The mesh to be added</param>
    private void AddMesh(Mesh mesh) {
        // Add to lists
        _meshes.Add(mesh);
        _vertices.AddRange(mesh.Vertices);

        // Build BVH
        mesh.BVHIndex = _bvhNodes.Count;
        var builder = new BVHBuilder(mesh.Vertices, mesh.Triangles, BVHType.SpatialSplit);
        var bvhRoot = builder.Build(25);
        var flat = bvhRoot.Flatten(_bvhNodes.Count, _triangles.Count);
        _bvhNodes.AddRange(flat.Item1);
        _triangles.AddRange(flat.Item2);
    }

    /// <summary>
    ///     Uploads all models stored to the buffers specified in the constructor.
    /// </summary>
    public void UploadModels() {
        var gpuMeshes = new Vector4[_meshes.Count * (Mesh.SizeInBytes / Vector4.SizeInBytes)];
        var gpuVertices = new Vector4[_vertices.Count * (Vertex.SizeInBytes / Vector4.SizeInBytes)];
        var gpuTriangles = new Vector4i[_triangles.Count * (Triangle.SizeInBytes / Vector4.SizeInBytes)];
        var gpuNodes = new Vector4[_bvhNodes.Count * (BVHNode.SizeInBytes / Vector4.SizeInBytes)];

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
        for (var i = 0; i < _triangles.Count; i++) {
            var triangle = _triangles[i];
            var gpuTriangle = triangle.GetGPUData();
            Array.Copy(gpuTriangle, 0, gpuTriangles, i * (Triangle.SizeInBytes / Vector4.SizeInBytes), gpuTriangle.Length);
        }

        // Convert BVHs into vector4 array
        for (var i = 0; i < _bvhNodes.Count; i++) {
            var node = _bvhNodes[i];
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

        GL.NamedBufferStorage(_trianglesBufferHandle, trianglesBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 4, _trianglesBufferHandle, IntPtr.Zero, trianglesBufferSize);
        GL.NamedBufferSubData(_trianglesBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuTriangles.Length, gpuTriangles);

        GL.NamedBufferStorage(_bvhMetadataHandle, Vector4.SizeInBytes, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.UniformBuffer, 5, _bvhMetadataHandle, IntPtr.Zero, Vector4.SizeInBytes);
        GL.NamedBufferSubData(_bvhMetadataHandle, IntPtr.Zero, Vector4.SizeInBytes, new Vector4(_bvhNodes.Count, 0, 0, 0));

        GL.NamedBufferStorage(_bvhBufferHandle, bvhBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 6, _bvhBufferHandle, IntPtr.Zero, bvhBufferSize);
        GL.NamedBufferSubData(_bvhBufferHandle, IntPtr.Zero, bvhBufferSize, gpuNodes);
    }
}