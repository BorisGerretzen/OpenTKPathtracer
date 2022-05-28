bool rayIntersectSphere(Ray ray, Sphere sphere, out RayHit rayHit)
{
    float t = dot(sphere.Position - ray.Origin, ray.Direction);
    vec3 closestPointOnLine = ray.Origin + ray.Direction * t;
    float distanceFromLine = length(sphere.Position - closestPointOnLine);

    // If ray hits sphere, calculate ray hit coordinates
    if (distanceFromLine < sphere.Radius)
    {
        float x = sqrt(sphere.Radius * sphere.Radius - distanceFromLine * distanceFromLine);
        float t1 = t - x;
        float t2 = t + x;
        float target;
        if (t1 > 0 && t1 < t2) {
            target = t1;
        } else if (t2 > 0 && t2 < t1) {
            target = t2;
        } else {
            return false;
        }

        rayHit.Position = ray.Origin + ray.Direction * target;
        rayHit.Distance = target;
        rayHit.FromInside = target == t2;
        rayHit.Normal = normalize(rayHit.Position-sphere.Position);
        rayHit.Material = sphere.Material;
        return true;
    }
    return false;
}

bool rayIntersectCuboid(Ray ray, Cuboid cuboid, out RayHit rayHit) {
    vec3 a1 = (cuboid.Min - ray.Origin) / ray.Direction;
    vec3 a2 = (cuboid.Max - ray.Origin) / ray.Direction;

    vec3 tsmall = min(a1, a2);
    vec3 tlarge = max(a1, a2);

    float t1 = max(-3.4028235e+38, max(tsmall.x, max(tsmall.y, tsmall.z)));
    float t2 = min(3.4028235e+38, min(tlarge.x, min(tlarge.y, tlarge.z)));

    float target = t1;

    // AABB behind us
    if (t2 < 0) {
        return false;
    }

    // if tmin > tmax, ray doesn't intersect AABB
    if (t1 > t2) {
        return false;
    }

    if (t1 > 0) {
        target = t1;
    } else {
        target = t2;
    }
    vec3 position = ray.Origin + ray.Direction*target;

    vec3 halfSize = (cuboid.Max - cuboid.Min) * 0.5;
    vec3 centerSurface = position - (cuboid.Max + cuboid.Min) * 0.5;

    vec3 normal = vec3(0.0);
    normal += vec3(sign(centerSurface.x), 0.0, 0.0) * step(abs(abs(centerSurface.x) - halfSize.x), 0.001f);
    normal += vec3(0.0, sign(centerSurface.y), 0.0) * step(abs(abs(centerSurface.y) - halfSize.y), 0.001f);
    normal += vec3(0.0, 0.0, sign(centerSurface.z)) * step(abs(abs(centerSurface.z) - halfSize.z), 0.001f);

    rayHit.Distance = target;
    rayHit.Material = cuboid.Material;
    rayHit.FromInside = target == t2;
    rayHit.Position = position;
    rayHit.Normal = normalize(normal);
    return true;
}

bool rayIntersectTriangle(Ray ray, Vertex v1, Vertex v2, Vertex v3, Material material, out RayHit rayHit) {
    vec3 v1v2 = v2.Position - v1.Position;
    vec3 v1v3 = v3.Position - v1.Position;
    vec3 normal = cross(v1v2, v1v3);
    float dotNormalDirection = dot(ray.Direction, normal);

    // Ray parallel to the plane -> no intersection
    if (abs(dotNormalDirection) < 0.001f) {
        return false;
    }

    float t = (dot(v1.Position, normal)-dot(ray.Origin, normal))/dotNormalDirection;
    if (t<0) {
        return false;
    }
    vec3 planeIntersection = ray.Origin + ray.Direction*t;

    if (dot(cross((v2.Position-v1.Position), (planeIntersection-v1.Position)), normal) < 0) {
        return false;
    }
    if (dot(cross((v3.Position-v2.Position), (planeIntersection-v2.Position)), normal) < 0) {
        return false;
    }
    if (dot(cross((v1.Position-v3.Position), (planeIntersection-v3.Position)), normal) < 0) {
        return false;
    }

    rayHit.Distance = t;
    rayHit.Position = planeIntersection;
    rayHit.FromInside = false;
    rayHit.Normal = normalize(normal);
    rayHit.Material = material;
    return true;
}

bool rayIntersectBVHLeaf(Ray ray, BVHNode node, Material material, out RayHit closestRayHit) {
    RayHit loopRayHit;
    closestRayHit.Distance = RAY_MAX;
    for (int i = 0; i < int(node.NumTriangles); i++) {
        Triangle triangle = trianglesSSBO.Triangles[i+int(node.TriangleOffset)];
        Vertex v1 = verticesSSBO.Vertices[triangle.index1];
        Vertex v2 = verticesSSBO.Vertices[triangle.index2];
        Vertex v3 = verticesSSBO.Vertices[triangle.index3];

        if (rayIntersectTriangle(ray, v1, v2, v3, material, loopRayHit)) {
            if (loopRayHit.Distance < closestRayHit.Distance) {
                closestRayHit = loopRayHit;
            }
        }
    }

    return closestRayHit.Distance < RAY_MAX;
}

bool rayIntersectMesh(Ray ray, Mesh mesh, out RayHit rayHit) {
    RayHit loopRayHit;
    rayHit.Distance = RAY_MAX;
    int stack[32];
    int stackPointer = 0;
    stack[0] = int(mesh.BVHIndex);

    while (stackPointer >= 0) {
        BVHNode node = GetBVH(stack[stackPointer]);

        if (int(node.Child1) == -1) {
            stackPointer--;
            continue;
        }

        if (rayIntersectCuboid(ray, Cuboid(node.AABBMin, node.AABBMax, mesh.Material), loopRayHit)) {
            if (loopRayHit.Distance > rayHit.Distance) {
                stackPointer--;
                continue;
            }

            // Process leaf
            if (int(node.NumTriangles) > 0) {
                if (rayIntersectBVHLeaf(ray, node, mesh.Material, loopRayHit) && loopRayHit.Distance < rayHit.Distance) {
                    rayHit = loopRayHit;
                }
                stackPointer--;
                continue;
            }

            int nearNode = GetNearChild(ray, node);
            int farNode = GetFarChild(ray, node);
            stack[stackPointer] = farNode;
            stack[stackPointer+1] = nearNode;
            stackPointer++;
        } else {
            stackPointer--;
        }
    }

    return rayHit.Distance < RAY_MAX;
}

// Checks if a given ray intersects any gameobject in the scene.
// If so, rayHit will be populated with the data of the intersection closest to the camera.
bool GetRayIntersection(Ray ray, out RayHit rayHit)
{
    RayHit loopRayHit;
    rayHit.Distance = RAY_MAX;
    for (int i = 0; i < gameObjectsSize.x; i++) {
        Sphere sphere = gameObjectsUBO.Spheres[i];

        if (rayIntersectSphere(ray, sphere, loopRayHit)) {
            if (loopRayHit.Distance < rayHit.Distance) {
                rayHit = loopRayHit;
            }
        }
    }

    for (int i = 0; i < lightsSize.x; i++) {
        Sphere sphere = lightsUBO.SphereLights[i];

        if (rayIntersectSphere(ray, sphere, loopRayHit)) {
            if (loopRayHit.Distance < rayHit.Distance) {
                rayHit = loopRayHit;
            }
        }
    }

    for (int i = 0; i < gameObjectsSize.y; i++) {
        Cuboid cuboid = gameObjectsUBO.Cuboids[i];

        if (rayIntersectCuboid(ray, cuboid, loopRayHit)) {
            if (loopRayHit.Distance < rayHit.Distance) {
                rayHit = loopRayHit;
            }
        }
    }

    for (int m = 0; m < meshSSBO.Meshes.length(); m++) {
        Mesh mesh = meshSSBO.Meshes[m];
        if (rayIntersectMesh(ray, mesh, loopRayHit)) {
            if (loopRayHit.Distance < rayHit.Distance) {
                rayHit = loopRayHit;
            }
        }
    }
    return rayHit.Distance < RAY_MAX;
}
