layout (local_size_x=8, local_size_y=4, local_size_z=1) in;
layout (binding=0, rgba32f) uniform image2D imgOut;
layout (binding=1) uniform samplerCube skybox;
layout (location=0) uniform uint currentFrameIndex;
layout (location=1) uniform ivec2 gameObjectsSize;
layout (location=2) uniform ivec2 lightsSize;

struct Material
{
    vec3 Emission;
    float Specularity;
    vec3 Albedo;
    float Refractivity;
    float IndexOfRefraction;
};

struct Sphere
{
    vec3 Position;
    float Radius;
    Material Material;
};

struct Cuboid
{
    vec3 Min;
    vec3 Max;
    Material Material;
};

struct Ray
{
    vec3 Origin;
    vec3 Direction;
};

struct RayHit
{
    vec3 Position;
    vec3 Normal;
    float Distance;
    bool FromInside;
    Material Material;
};

struct Vertex {
    vec3 Position;
    vec3 Normal;
    vec2 TextureCoordinates;
};

struct Triangle {
    int index1;
    int index2;
    int index3;
    int usedForFillerDoNotUseThisVariable;
};

struct BVHNode {
    vec3 AABBMin;
    float Child1;// Child 1 smallest on the split axis
    vec3 AABBMax;
    float Child2;
    float SplitAxis;
    float NumTriangles;
    float TriangleOffset;
    float ParentIndex;
};

struct Mesh {
    float BVHIndex;
    Material Material;
//sampler2D DiffuseMap;
//sampler2D SpecularityMap;
//sampler2D RefractMap;
};

// Some linear algebra magic
layout(std140, binding = 0) uniform BasicDataUBO {
    mat4 InverseProjectionMatrix;
    mat4 InverseViewMatrix;
    vec4 ViewPosition;
    float numBounces;
} basicDataUBO;

// Game objects that will be rendered
layout(std140, binding = 1) uniform GameObjectsUBO {
    Sphere Spheres[256];
    Cuboid Cuboids[64];
} gameObjectsUBO;
layout(std140, binding=2) buffer MeshSSBO {
    Mesh Meshes[];
} meshSSBO;
layout(std140, binding=3) buffer VerticesSSBO {
    Vertex Vertices[];
} verticesSSBO;
layout(std430, binding=4) buffer TrianglesSSBO {
    Triangle Triangles[];
} trianglesSSBO;

// BVH
layout(std140, binding = 5) uniform BVHMetadata {
    int NumBVs;
} bvhMetaData;
layout(std140, binding=6) buffer BVHSSBO {
    BVHNode BVHNodes[];
} bvhSSBO;
layout(std140, binding=7) uniform LightsUBO {
    Sphere SphereLights[256];
} lightsUBO;
