using OpenTK.Mathematics;

namespace PathTracer.Helpers; 

public class Material {
    public Vector3 Albedo;
    public Vector3 Emission;
    
    public Material(Vector3 albedo, Vector3 emission) {
        Albedo = albedo;
        Emission = emission;
    }
}