using ObjLoader.Loader.Loaders;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer;

public class ModelHolder {
    private readonly List<Mesh> _meshes;
    private readonly List<Vertex> _vertices;
    private readonly List<int> _indices;

    private readonly BufferHandle _vertexBufferHandle;
    private readonly BufferHandle _indicesBufferHandle;
    private readonly BufferHandle _meshBufferHandle;
    private readonly IObjLoader _objLoader;

    public ModelHolder(BufferHandle vertexBufferHandle, BufferHandle indicesBufferHandle, BufferHandle meshBufferHandle) {
        _vertexBufferHandle = vertexBufferHandle;
        _indicesBufferHandle = indicesBufferHandle;
        _meshBufferHandle = meshBufferHandle;
        _meshes = new List<Mesh>();
        _vertices = new List<Vertex>();
        _indices = new List<int>();
        _objLoader = new ObjLoaderFactory().Create();
    }


    public void AddModel(string path, Material material, Vector3 position) {
        var fileStream = File.Open("models/teapot.obj", FileMode.Open);
        var result = _objLoader.Load(fileStream);
        var vertices = new List<Vertex>();
        var indices = new List<int>();
        var MinVertex = Vector3.PositiveInfinity;
        var MaxVertex = Vector3.NegativeInfinity;
        foreach (var vertex in result.Vertices) {
            if (vertex.X < MinVertex.X) MinVertex.X = vertex.X;
            if (vertex.Y < MinVertex.Y) MinVertex.Y = vertex.Y;
            if (vertex.Z < MinVertex.Z) MinVertex.Z = vertex.Z;
            if (vertex.X > MinVertex.X) MaxVertex.X = vertex.X;
            if (vertex.Y > MinVertex.Y) MaxVertex.Y = vertex.Y;
            if (vertex.Z > MinVertex.Z) MaxVertex.Z = vertex.Z;
            vertices.Add(new Vertex(new Vector3(vertex.X, vertex.Y, vertex.Z), Vector3.One, Vector2.One));
        }

        foreach (var group in result.Groups)
        foreach (var face in group.Faces) {
            indices.Add(face[0].VertexIndex - 1);
            indices.Add(face[1].VertexIndex - 1);
            indices.Add(face[2].VertexIndex - 1);
        }

        AddMesh(new Mesh(vertices, indices, material, position, MinVertex, MaxVertex));
    }

    public void AddMesh(Mesh mesh) {
        _meshes.Add(mesh);
        mesh.VertexStartOffset = (uint)_vertices.Count;
        mesh.IndicesStartOffset = (uint)_indices.Count;
        _vertices.AddRange(mesh.Vertices);
        _indices.AddRange(mesh.Indices);
    }

    public void UploadModels() {
        var gpuMeshes = new Vector4[_meshes.Count * (Mesh.SizeInBytes / Vector4.SizeInBytes)];
        var gpuVertices = new Vector4[_vertices.Count * (Vertex.SizeInBytes / Vector4.SizeInBytes)];
        var gpuIndices = new Vector4i[(int)Math.Ceiling(_indices.Count / 3.0)];

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

        // Convert indices into vector4 array
        var indexIndex = 0;
        for (var i = 0; i < (int)Math.Ceiling(_indices.Count / 3.0); i++) {
            gpuIndices[i].X = _indices[indexIndex];
            gpuIndices[i].Y = _indices[indexIndex + 1];
            gpuIndices[i].Z = _indices[indexIndex + 2];
            indexIndex += 3;
        }

        // Beam em up
        var meshBufferSize = Vector4.SizeInBytes * gpuMeshes.Length;
        var vertexBufferSize = Vector4.SizeInBytes * gpuVertices.Length;
        var indicesBufferSize = Vector4.SizeInBytes * gpuIndices.Length;
        if (meshBufferSize == 0 || vertexBufferSize == 0 || indicesBufferSize == 0) return;
        GL.NamedBufferStorage(_meshBufferHandle, meshBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 2, _meshBufferHandle, IntPtr.Zero, meshBufferSize);
        GL.NamedBufferSubData(_meshBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuMeshes.Length, gpuMeshes);

        GL.NamedBufferStorage(_vertexBufferHandle, vertexBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 3, _vertexBufferHandle, IntPtr.Zero, vertexBufferSize);
        GL.NamedBufferSubData(_vertexBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuVertices.Length, gpuVertices);

        GL.NamedBufferStorage(_indicesBufferHandle, indicesBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 4, _indicesBufferHandle, IntPtr.Zero, indicesBufferSize);
        GL.NamedBufferSubData(_indicesBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuIndices.Length, gpuIndices);
    }
}