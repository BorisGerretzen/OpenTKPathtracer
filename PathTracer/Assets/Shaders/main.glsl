vec3 sampleLights(RayHit rayHit, bool reflected) {
    // Shadow ray
    int sphereLightIndex = GetRandomInt(0, lightsSize.x-1);
    Sphere lightSphere = lightsUBO.SphereLights[sphereLightIndex];
    vec3 target = CosineSampleHemisphere(normalize(rayHit.Position - lightSphere.Position));
    target *= lightSphere.Radius;
    target += lightSphere.Position;

    RayHit shadowRayHit;
    Ray shadowRay = Ray(rayHit.Position+rayHit.Normal*0.001f, normalize(target-rayHit.Position));
    vec3 returnVal = vec3(0);
    if (GetRayIntersection(shadowRay, shadowRayHit) && length(shadowRayHit.Material.Emission) > 0) {
        float angle = dot(shadowRay.Direction, rayHit.Normal);
        float pdf = 1/PI;
        if (reflected) {
            pdf = 1/2*PI;
        }

        returnVal = shadowRayHit.Material.Emission *
        rayHit.Material.Albedo *
        (1/(max(0.001f, shadowRayHit.Distance*shadowRayHit.Distance))) *
        angle *
        dot(shadowRayHit.Normal, -shadowRay.Direction) *
        pdf *
        lightsSize.x;
    }

    return returnVal;
}

// Gets the color of a given ray with all the bounces and stuff.
vec3 getColor(Ray ray)
{
    RayHit rayHit;
    vec3 color = vec3(0.0);
    vec3 throughput = vec3(1.0);

    for (int i = 0; i < basicDataUBO.numBounces; i++)
    {
        if (GetRayIntersection(ray, rayHit))
        {
            if (rayHit.FromInside) {
                rayHit.Normal = -rayHit.Normal;
            }

            bool refracted;
            bool reflected;
            Ray newRay;
            float rayProbability;
            rayProbability = BRDF(ray, rayHit, newRay, refracted, reflected);

            // No NEE in refractions
            if (!refracted && !rayHit.FromInside) {
                if (i == 0 || reflected) { // Sample light directly if reflected or first ray
                    color += rayHit.Material.Emission * throughput;
                }
                if (length(rayHit.Material.Emission) == 0) { // Only NEE on non lights
                    color += sampleLights(rayHit, reflected) * throughput;
                }
            } else {
                color += rayHit.Material.Emission * throughput;
            }

            // Get BSDF
            ray = newRay;
            throughput *= rayHit.Material.Albedo;
            throughput /= rayProbability;

            // Russian roulette
            float propagationChance = max(max(throughput.x, throughput.y), throughput.z);
            if (GetRandomFloat01() > propagationChance) {
                break;
            }
            throughput *= 1.0f/propagationChance;
        }
        else
        {
            color += texture(skybox, ray.Direction).rgb * throughput;
            break;
        }
    }
    return color;
}

void main()
{
    // random number stuff
    rndSeed = gl_GlobalInvocationID.x * 1973 + gl_GlobalInvocationID.y * 9277 + currentFrameIndex * 2699 | 1;

    // Get pixel coordinates as well as normalized between [-1,1]
    ivec2 pos = ivec2(gl_GlobalInvocationID.xy);
    // Shoot out a couple rays, the average color will be the color of the pixel
    vec3 color = vec3(0.0);
    for (int i = 0; i < RAYS_PER_PIXEL; i++)
    {
        vec2 offset = vec2(GetRandomFloat01(), GetRandomFloat01());
        vec2 normalizedDeviceCoordinates = vec2(pos+offset) / vec2(imageSize(imgOut)) * 2 - 1;
        Ray ray = rayFromCamera(basicDataUBO.InverseProjectionMatrix, basicDataUBO.InverseViewMatrix, basicDataUBO.ViewPosition.xyz, normalizedDeviceCoordinates);
        vec3 newC = getColor(ray);
        color += newC;
    }

    color /= RAYS_PER_PIXEL;

    // Progressive rendering
    vec3 lastColor = imageLoad(imgOut, pos).rgb;
    color = mix(lastColor, color, 1.0f/(currentFrameIndex+1));

    // Store it in the image and were done
    imageStore(imgOut, pos, vec4(color, 1.0));
}
