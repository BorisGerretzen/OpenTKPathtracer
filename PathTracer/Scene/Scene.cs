using System.Runtime.Serialization;

namespace PathTracer.Scene;

[DataContract]
public class Scene {
    [DataMember] public List<Cuboid> Cuboids { get; set; }
    [DataMember] public List<Sphere> Spheres { get; set; }
    [DataMember] public List<SerializableMesh> Meshes { get; set; }
}