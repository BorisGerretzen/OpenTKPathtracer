using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer.Scene;

public class SerializableMesh {
    public string Path { get; set; }
    public Material Material { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Scale { get; set; }

    public SerializableMesh(string path, Material material, Vector3 position, Vector3 scale) {
        Path = path;
        Material = material;
        Position = position;
        Scale = scale;
    }

    public SerializableMesh() { }
}