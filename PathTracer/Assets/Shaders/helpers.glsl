uint rndSeed;

// ---------- Random number stuff idk whats going on here ---------- // 
// https://blog.demofox.org/2020/05/25/casual-shadertoy-path-tracing-1-basic-camera-diffuse-emissive/
// https://github.com/BoyBaykiller/OpenTK-PathTracer/blob/master/OpenTK-PathTracer/res/shaders/PathTracing/compute.glsl
uint GetPCGHash(inout uint seed)
{
    seed = seed * 747796405u + 2891336453u;
    uint word = ((seed >> ((seed >> 28u) + 4u)) ^ seed) * 277803737u;
    return (word >> 22u) ^ word;
}

float GetRandomFloat01()
{
    return float(GetPCGHash(rndSeed)) / 4294967296.0;
}

int GetRandomInt(int min, int max) {
    return int(round(GetRandomFloat01() * (max-min))) + min;
}

vec3 CosineSampleHemisphere(vec3 normal)
{
    float z = GetRandomFloat01() * 2.0 - 1.0;
    float a = GetRandomFloat01() * 2.0 * PI;
    float r = sqrt(1.0 - z * z);
    float x = r * cos(a);
    float y = r * sin(a);
    return normalize(normal + vec3(x, y, z));
}

vec3 CosineSampleSphere(Sphere sphere) {
    float z = GetRandomFloat01() * 2.0 - 1.0;
    float a = GetRandomFloat01() * 2.0 * PI;
    float r = sqrt(1.0 - z * z);
    float x = r * cos(a);
    float y = r * sin(a);
    return normalize(vec3(x, y, z))*(sphere.Radius-0.01f)+sphere.Position;
}

Ray rayFromCamera(mat4 inverseProjectionMatrix, mat4 inverseViewMatrix, vec3 viewPos, vec2 normalizedDeviceCoords)
{
    // https://antongerdelan.net/opengl/raycasting.html
    // 4d Homogeneous Clip Coordinates
    vec4 rayClip = vec4(normalizedDeviceCoords.xy, -1.0, 1.0);

    // 4d camera coordinates
    vec4 rayCamera = inverseProjectionMatrix * vec4(normalizedDeviceCoords, -1.0, 0.0);
    rayCamera.zw = vec2(-1.0, 0.0);

    // 3d World Coordinates
    vec3 rayWorld = normalize((inverseViewMatrix * rayCamera).xyz);
    return Ray(viewPos, rayWorld);
}

int GetNearChild(Ray ray, BVHNode node) {
    if (int(node.SplitAxis) == SPLIT_X) {
        if (ray.Direction.x > 0) {
            return int(node.Child1);
        } else {
            return int(node.Child2);
        }
    } else if (int(node.SplitAxis) == SPLIT_Y) {
        if (ray.Direction.y > 0) {
            return int(node.Child1);
        } else {
            return int(node.Child2);
        }
    } else {
        if (ray.Direction.z > 0) {
            return int(node.Child1);
        } else {
            return int(node.Child2);
        }
    }
}

int GetFarChild(Ray ray, BVHNode node) {
    if (node.SplitAxis == SPLIT_X) {
        if (ray.Direction.x < 0) {
            return int(node.Child1);
        } else {
            return int(node.Child2);
        }
    } else if (node.SplitAxis == SPLIT_Y) {
        if (ray.Direction.y < 0) {
            return int(node.Child1);
        } else {
            return int(node.Child2);
        }
    } else {
        if (ray.Direction.z < 0) {
            return int(node.Child1);
        } else {
            return int(node.Child2);
        }
    }
}
BVHNode GetBVH(int BVHIndex) {
    return bvhSSBO.BVHNodes[BVHIndex];
}