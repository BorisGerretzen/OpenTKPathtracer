using System.Data;
using System.Xml.Serialization;
using OpenTK.Graphics;
using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer.Scene;

public class SceneLoader {
    public Scene _scene;
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
        _scene = new Scene();
        _scene.Cuboids = new List<Cuboid>(maxCuboids);
        _scene.Spheres = new List<Sphere>(maxSpheres);
        _scene.Meshes = new List<SerializableMesh>();
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
        if (_scene.Cuboids.Count + 1 > _scene.Cuboids.Capacity) throw new ConstraintException($"Max number of cuboids '{_scene.Cuboids.Capacity}' has been exceeded.");
        _scene.Cuboids.Add(new Cuboid(min, max, material, _scene.Cuboids.Count));
    }

    /// <summary>
    ///     Adds a sphere to the scene.
    /// </summary>
    /// <param name="center">Position of the center of the sphere</param>
    /// <param name="radius">Radius of the sphere</param>
    /// <param name="material">Material of the sphere</param>
    /// <exception cref="ConstraintException">If max number of spheres is exceeded</exception>
    public void AddSphere(Vector3 center, float radius, Material material) {
        if (_scene.Spheres.Count + 1 > _scene.Spheres.Capacity) throw new ConstraintException($"Max number of spheres '{_scene.Spheres.Capacity}' has been exceeded.");
        _scene.Spheres.Add(new Sphere(center, radius, material, _scene.Spheres.Count));
    }

    /// <summary>
    ///     Adds a Waveform .obj model to the scene
    /// </summary>
    /// <param name="path">Path to the .obj file</param>
    /// <param name="material">Material of the model</param>
    /// <param name="position">Position of the model</param>
    /// <param name="scale">Size of the model</param>
    public void AddModel(string path, Material material, Vector3 position, Vector3 scale) {
        _scene.Meshes.Add(new SerializableMesh(path, material, position, scale));
    }

    /// <summary>
    ///     Uploads the GameObjects to the gpu.
    /// </summary>
    public void Upload() {
        _scene.Cuboids.ForEach(gameObject => gameObject.Upload(_gameObjectsUbo));
        _scene.Spheres.ForEach(gameObject => gameObject.Upload(_gameObjectsUbo));
        _scene.Meshes.ForEach(mesh => _modelHolder.AddModel(mesh.Path, mesh.Material, mesh.Position, mesh.Scale));
        _modelHolder.UploadModels();
    }

    /// <summary>
    ///     Gets the value for the basic data UBO.
    /// </summary>
    /// <returns></returns>
    public Vector2i GetBasicData() {
        return new Vector2i(_scene.Spheres.Count, _scene.Cuboids.Count);
    }

    public void LoadScene(string path) {
        var xml = File.ReadAllText(path);
        var serializer = new XmlSerializer(typeof(Scene));
        var reader = new StreamReader(path);
        _scene = (Scene)serializer.Deserialize(reader);
    }
}