using System.Data;
using OpenTK.Graphics;
using OpenTK.Mathematics;
using PathTracer.Helpers;

namespace PathTracer.Scene;

public class SceneLoader {
    private readonly List<GameObject> _gameObjects;
    private readonly int _maxCuboids;
    private readonly int _maxSpheres;
    private readonly BufferHandle _gameObjectsUbo;
    private readonly ModelHolder _modelHolder;

    private int _numCuboids;
    private int _numSpheres;

    /// <summary>
    ///     Creates a new scene loader, this is responsible for loading all primitives and models to a scene.
    /// </summary>
    /// <param name="maxCuboids">Max number of cuboids</param>
    /// <param name="maxSpheres">Max number of spheres</param>
    /// <param name="gameObjectsUbo">BufferHandle of the game object buffer</param>
    /// <param name="modelHolder">The ModelHolder in use</param>
    public SceneLoader(int maxCuboids, int maxSpheres, BufferHandle gameObjectsUbo, ModelHolder modelHolder) {
        _gameObjects = new List<GameObject>(maxCuboids + maxSpheres);
        _maxCuboids = maxCuboids;
        _maxSpheres = maxSpheres;
        _gameObjectsUbo = gameObjectsUbo;
        _modelHolder = modelHolder;
    }

    /// <summary>
    ///     Add a cuboid to the scene.
    /// </summary>
    /// <param name="min">Minimum corner of the cuboid</param>
    /// <param name="max">Maximum corner of the cuboid</param>
    /// <param name="material">Material of the cuboid</param>
    /// <exception cref="ConstraintException">If max number of cuboids is exceeded</exception>
    public void AddCuboid(Vector3 min, Vector3 max, Material material) {
        if (_numCuboids + 1 > _maxCuboids) throw new ConstraintException($"Max number of cuboids '{_maxCuboids}' has been exceeded.");
        _gameObjects.Add(new Cuboid(min, max, material, _numCuboids++));
    }

    /// <summary>
    ///     Adds a sphere to the scene.
    /// </summary>
    /// <param name="center">Position of the center of the sphere</param>
    /// <param name="radius">Radius of the sphere</param>
    /// <param name="material">Material of the sphere</param>
    /// <exception cref="ConstraintException">If max number of spheres is exceeded</exception>
    public void AddSphere(Vector3 center, float radius, Material material) {
        if (_numCuboids + 1 > _numSpheres) throw new ConstraintException($"Max number of spheres '{_maxSpheres}' has been exceeded.");
        _gameObjects.Add(new Sphere(center, radius, material, _numSpheres++));
    }

    /// <summary>
    ///     Adds a Waveform .obj model to the scene
    /// </summary>
    /// <param name="path">Path to the .obj file</param>
    /// <param name="material">Material of the model</param>
    /// <param name="position">Position of the model</param>
    /// <param name="scale">Size of the model</param>
    public void AddModel(string path, Material material, Vector3 position, Vector3 scale) {
        _modelHolder.AddModel(path, material, position, scale);
    }

    /// <summary>
    ///     Uploads the GameObjects to the gpu.
    /// </summary>
    public void Upload() {
        _gameObjects.ForEach(gameObject => gameObject.Upload(_gameObjectsUbo));
        _modelHolder.UploadModels();
    }

    /// <summary>
    ///     Gets the value for the basic data UBO.
    /// </summary>
    /// <returns></returns>
    public Vector2i GetBasicData() {
        return new Vector2i(_numSpheres, _numCuboids);
    }
}