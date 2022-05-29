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
            //            if(refracted && reflected) {
            //                color = vec3(1.0, 0, 0);
            //                return color;
            //            }
            color += rayHit.Material.Emission * throughput;

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
