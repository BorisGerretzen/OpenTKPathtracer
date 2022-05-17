using System.Data;
using System.Xml.Serialization;
using OpenTK.Graphics;
using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer.Scene;

public class SceneLoader {
    public Scene Scene;
    private readonly BufferHandle _gameObjectsUbo;
    private readonly ModelHolder _modelHolder;
    
    /// <summary>
    ///     Creates a new scene loader, this is responsible for loading all primitives and models to a scene.
    /// </summary>
    /// <param name="maxCuboids">Max number of cuboids</param>
    /// <param name="maxSpheres">Max number of spheres</param>
    /// <param name="gameObjectsUbo">BufferHandle of the game object buffer</param>
    /// <param name="modelHolder">The ModelHolder in use</param>
    public SceneLoader(int maxCuboids, int maxSpheres, BufferHandle gameObjectsUbo, ModelHolder modelHolder) {
        Scene = new Scene();
        Scene.Cuboids = new List<Cuboid>(maxCuboids);
        Scene.Spheres = new List<Sphere>(maxSpheres);
        Scene.Meshes = new List<SerializableMesh>();
        _gameObjectsUbo = gameObjectsUbo;
        _modelHolder = modelHolder;
    }

    private SceneLoader() { }
    /// <summary>
    ///     Add a cuboid to the scene.
    /// </summary>
    /// <param name="min">Minimum corner of the cuboid</param>
    /// <param name="max">Maximum corner of the cuboid</param>
    /// <param name="material">Material of the cuboid</param>
    /// <exception cref="ConstraintException">If max number of cuboids is exceeded</exception>
    public void AddCuboid(Vector3 min, Vector3 max, Material material) {
        if (Scene.Cuboids.Count + 1 > Scene.Cuboids.Capacity) throw new ConstraintException($"Max number of cuboids '{Scene.Cuboids.Capacity}' has been exceeded.");
        Scene.Cuboids.Add(new Cuboid(min, max, material, Scene.Cuboids.Count));
    }

    /// <summary>
    ///     Adds a sphere to the scene.
    /// </summary>
    /// <param name="center">Position of the center of the sphere</param>
    /// <param name="radius">Radius of the sphere</param>
    /// <param name="material">Material of the sphere</param>
    /// <exception cref="ConstraintException">If max number of spheres is exceeded</exception>
    public void AddSphere(Vector3 center, float radius, Material material) {
        if (Scene.Spheres.Count + 1 > Scene.Spheres.Capacity) throw new ConstraintException($"Max number of spheres '{Scene.Spheres.Capacity}' has been exceeded.");
        Scene.Spheres.Add(new Sphere(center, radius, material, Scene.Spheres.Count));
    }

    /// <summary>
    ///     Adds a Waveform .obj model to the scene
    /// </summary>
    /// <param name="path">Path to the .obj file</param>
    /// <param name="material">Material of the model</param>
    /// <param name="position">Position of the model</param>
    /// <param name="scale">Size of the model</param>
    public void AddModel(string path, Material material, Vector3 position, Vector3 scale) {
        Scene.Meshes.Add(new SerializableMesh(path, material, position, scale));
    }

    /// <summary>
    ///     Uploads the GameObjects to the gpu.
    /// </summary>
    public void Upload() {
        Scene.Cuboids.ForEach(gameObject => gameObject.Upload(_gameObjectsUbo));
        Scene.Spheres.ForEach(gameObject => gameObject.Upload(_gameObjectsUbo));
        Scene.Meshes.ForEach(mesh => _modelHolder.AddModel(mesh.Path, mesh.Material, mesh.Position, mesh.Scale));
        _modelHolder.UploadModels();
    }

    /// <summary>
    ///     Gets the value for the basic data UBO.
    /// </summary>
    /// <returns></returns>
    public Vector2i GetBasicData() {
        return new Vector2i(Scene.Spheres.Count, Scene.Cuboids.Count);
    }

    public void LoadScene(string path) {
        var xml = File.ReadAllText(path);
        var serializer = new XmlSerializer(typeof(Scene));
        var reader = new StreamReader(path);
        Scene = (Scene)serializer.Deserialize(reader);
    }
}