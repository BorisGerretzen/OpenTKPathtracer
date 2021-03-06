# OpenTK Pathtracer
This is a path tracing algorithm implemented as an OpenGL compute shader, with OpenTK providing the OpenGL bindings.
![Bunny render](https://user-images.githubusercontent.com/15902678/170845931-1dc8914a-5e61-4894-8c64-77d4ba2405e6.png)

## Capabilities
In the compute shader a couple ways to improve the rendering time and quality are implemented.
- Importance sampling is used to improve the convergence time of the rendering
- Next event estimation is used to significantly improve the convergence time of the rendering
- Russian roulette is used to terminate rays before they reach their depth limit, the chance for a path to be terminated becomes bigger with each bounce. The rays that do make it further are in turn weighted heavier than the ones that do not.
- Antialiasing is implemented by offsetting each ray by a tiny sub-pixel amount. this fuzzes the edges enough to make them seem smooth.
- The program supports progressive rendering by averaging the pixels from previous frames together.
- A skybox is included to provide global illumination and some nice scenery
- Bounding volume hierarchies are used as acceleration structures to decrease the amount of ray triangle intersections that have to be done for every ray.

Currently, the following material options are supported:
- Fully diffuse materials
- Fully specular materials
- Dielectric materials
- Glossy materials (mix of specular and diffuse)

## How to use
1. Use `.\PathTracer.exe --help` to get an overview of the command line arguments.
    - `--scene` is used to specify a scene file to load, they are located in `Assets/Scenes/`.
    - `--depth` is used to specify the inital ray depth.
    - `--debug` is used to render the left side of the screen with NEE and the right side without.
2. You can move around in the scene using the ```WASD``` buttons, the ```spacebar``` and ```left shift``` are used to go up and down respectively.
3. Once you have found a suitable location to render, press the `L` button to lock the camera into place.
4. After the camera is locked, press the `=` and `-` buttons to raise and lower the ray depth respectively.
5. Wait for the render to progress...
6. Press the `I` button to take a screenshot, be sure to smash it for a couple seconds because when the framerate is low it sometimes doesn't pick it up. To make sure the image is exported, take a look in the console.
7. Once you are done press the `ESC` key to exit the program.

## Future work
- [x] Next event estimation, to substantially reduce the convergence time.
- [x] Splitting the compute shader source code into multiple files. Currently the compute shader resides in one big file, this makes it hard to edit and is not very DRY.
- [ ] Different types of bounding volume hierarchy construction. Currently the bounding volumes are split using the spatial split method. A more optimized way would be to use the surface area heuristic.
