float BRDF(Ray ray, RayHit rayHit, out Ray newRay, out bool refracted, out bool reflected) {
    float rayProbability;
    vec3 newRayDirection;

    float reflectRoll = GetRandomFloat01();
    if (rayHit.Material.Specularity > reflectRoll) {
        newRayDirection = reflect(ray.Direction, rayHit.Normal);
        rayProbability = rayHit.Material.Specularity;
        reflected = true;
    } else if (rayHit.Material.Specularity + rayHit.Material.Refractivity > reflectRoll) {
        newRayDirection = refract(ray.Direction, rayHit.Normal, rayHit.FromInside ? rayHit.Material.IndexOfRefraction:1.0f/rayHit.Material.IndexOfRefraction);
        rayProbability = rayHit.Material.Refractivity;

        if (length(newRayDirection) <= 0.01f) {
            newRayDirection = reflect(ray.Direction, rayHit.Normal);
            rayProbability = 1;
            reflected = true;
        } else {
            refracted = true;
        }
    }
    else {
        newRayDirection = CosineSampleHemisphere(rayHit.Normal);
        rayProbability = 1.0f - rayHit.Material.Specularity - rayHit.Material.Refractivity;
    }

    newRay.Direction = normalize(newRayDirection);
    newRay.Origin = rayHit.Position + newRay.Direction * 0.001f;

    return max(rayProbability, 0.001f);
}