using System.Numerics;
using PathTracer.Helpers;

namespace PathTracer; 

public class Sphere : GameObject {
    public Vector3 Position;
    public float Radius;
    public Material Material;
    
    public Sphere(Vector3 position, float radius, Material material) {
        Position = position;
        Radius = radius;
        Material = material;
    }
}