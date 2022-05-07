using ObjLoader.Loader.Loaders;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer;

public class ModelHolder {
    private readonly List<Mesh> _meshes;
    private readonly List<Vertex> _vertices;
    private readonly List<Triangle> _triangles;

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
        _triangles = new List<Triangle>();
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
            if (vertex.X < minVertex.X) minVertex.X = vertex.X;
            if (vertex.Y < minVertex.Y) minVertex.Y = vertex.Y;
            if (vertex.Z < minVertex.Z) minVertex.Z = vertex.Z;
            if (vertex.X > minVertex.X) maxVertex.X = vertex.X;
            if (vertex.Y > minVertex.Y) maxVertex.Y = vertex.Y;
            if (vertex.Z > minVertex.Z) maxVertex.Z = vertex.Z;
            vertices.Add(new Vertex(new Vector3(vertex.X, vertex.Y, vertex.Z), Vector3.One, Vector2.One));
        }

        foreach (var group in result.Groups)
        foreach (var face in group.Faces) {
            var t = new Triangle(face[0].VertexIndex - 1, face[1].VertexIndex - 1, face[2].VertexIndex - 1);
            triangles.Add(t);
        }

        AddMesh(new Mesh(vertices, triangles, material, position, minVertex, maxVertex));
    }

    public void AddMesh(Mesh mesh) {
        _meshes.Add(mesh);
        mesh.VertexStartOffset = _vertices.Count;
        mesh.IndicesStartOffset = _triangles.Count / 3;
        _vertices.AddRange(mesh.Vertices);
        _triangles.AddRange(mesh.Triangles);
    }

    public void UploadModels() {
        var gpuMeshes = new Vector4[_meshes.Count * (Mesh.SizeInBytes / Vector4.SizeInBytes)];
        var gpuVertices = new Vector4[_vertices.Count * (Vertex.SizeInBytes / Vector4.SizeInBytes)];
        var gpuTriangles = new Vector4i[_triangles.Count * (Triangle.SizeInBytes / Vector4.SizeInBytes)];

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
        for (var i = 0; i < _triangles.Count; i++) {
            var triangle = _triangles[i];
            var gpuTriangle = triangle.GetGPUData();
            Array.Copy(gpuTriangle, 0, gpuTriangles, i * (Triangle.SizeInBytes / Vector4.SizeInBytes), gpuTriangle.Length);
        }

        // Beam em up
        var meshBufferSize = Vector4.SizeInBytes * gpuMeshes.Length;
        var vertexBufferSize = Vector4.SizeInBytes * gpuVertices.Length;
        var trianglesBufferSize = Vector4.SizeInBytes * gpuTriangles.Length;
        if (meshBufferSize == 0 || vertexBufferSize == 0 || trianglesBufferSize == 0) return;
        GL.NamedBufferStorage(_meshBufferHandle, meshBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 2, _meshBufferHandle, IntPtr.Zero, meshBufferSize);
        GL.NamedBufferSubData(_meshBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuMeshes.Length, gpuMeshes);

        GL.NamedBufferStorage(_vertexBufferHandle, vertexBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 3, _vertexBufferHandle, IntPtr.Zero, vertexBufferSize);
        GL.NamedBufferSubData(_vertexBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuVertices.Length, gpuVertices);

        GL.NamedBufferStorage(_indicesBufferHandle, trianglesBufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.BindBufferRange(BufferTargetARB.ShaderStorageBuffer, 4, _indicesBufferHandle, IntPtr.Zero, trianglesBufferSize);
        GL.NamedBufferSubData(_indicesBufferHandle, IntPtr.Zero, Vector4.SizeInBytes * gpuTriangles.Length, gpuTriangles);
    }
}