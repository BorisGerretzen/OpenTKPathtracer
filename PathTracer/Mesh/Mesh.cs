using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer;

public class Mesh : Uploadable {
    public static int SizeInBytes => Vector4.SizeInBytes * 1 + Material.SizeInBytes;

    public List<Vertex> Vertices;

    public List<Triangle> Triangles;
    //public List<GameTexture> Textures;

    public Material Material;
    public int? BVHIndex;

    public Mesh(List<Vertex> vertices, List<Triangle> triangles, Material material, Vector3 aabbMin, Vector3 aabbMax) {
        Vertices = vertices;
        Triangles = triangles;
        Material = material;
        //Vertices.ForEach(vertex => vertex.Position += position);
    }


    public override int BufferOffset => throw new NotSupportedException("Do not upload directly");

    public override Vector4[] GetGPUData() {
        if (BVHIndex == null) throw new Exception("BVH index is null, do not use this class directly.");

        var returnData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        returnData[0].X = Triangles.Count;
        returnData[0].Y = (float)BVHIndex;
        var materialData = Material.GetGPUData();
        Array.Copy(materialData, 0, returnData, 3, materialData.Length);
        return returnData;
    }
}