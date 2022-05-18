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

    /// <summary>
    ///     Creates a new mesh object
    /// </summary>
    /// <param name="vertices">Vertices of the mesh</param>
    /// <param name="triangles">Triangles of the mesh</param>
    /// <param name="material">Material of the mesh</param>
    public Mesh(List<Vertex> vertices, List<Triangle> triangles, Material material) {
        Vertices = vertices;
        Triangles = triangles;
        Material = material;
    }


    public override int BufferOffset => throw new NotSupportedException("Do not upload directly");

    public override Vector4[] GetGPUData() {
        if (BVHIndex == null) throw new Exception("BVH index is null, do not use this class directly.");

        var returnData = new Vector4[SizeInBytes / Vector4.SizeInBytes];
        returnData[0].X = (float)BVHIndex;
        var materialData = Material.GetGPUData();
        Array.Copy(materialData, 0, returnData, 1, materialData.Length);
        return returnData;
    }
}