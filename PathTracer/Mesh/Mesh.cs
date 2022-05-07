﻿using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer;

public class Mesh : Uploadable {
    public static int SizeInBytes => Vector4.SizeInBytes * 3 + Material.SizeInBytes;

    public List<Vertex> Vertices;

    public List<int> Indices;
    //public List<GameTexture> Textures;

    public Material Material;
    public Vector3 AABBMin;
    public Vector3 AABBMax;

    public uint? VertexStartOffset;
    public uint? IndicesStartOffset;

    public Mesh(List<Vertex> vertices, List<int> indices, Material material, Vector3 position, Vector3 aabbMin, Vector3 aabbMax) {
        Vertices = vertices;
        Indices = indices;
        Material = material;
        AABBMin = aabbMin;
        AABBMax = aabbMax;
        vertices.ForEach(vertex => vertex.Position += position);
    }


    public override int BufferOffset => throw new NotSupportedException("Do not upload directly");

    public override Vector4[] GetGPUData() {
        if (VertexStartOffset == null || IndicesStartOffset == null) throw new Exception("Start offsets are 0, do not use this class directly.");

        var returnData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        returnData[1].Xyz = AABBMin;
        returnData[2].Xyz = AABBMax;
        var materialData = Material.GetGPUData();
        Array.Copy(materialData, 0, returnData, 3, materialData.Length);
        return returnData;
    }
}