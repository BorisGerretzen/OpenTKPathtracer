# OpenTK Pathtracer
This is a path tracing algorithm implemented as an OpenGL compute shader, with OpenTK providing the OpenGL bindings.
![perfect reflection](https://user-images.githubusercontent.com/15902678/164809199-aa79daee-b6f7-483a-9f56-3dd0e7b421e5.png)

## Capabilities
In the compute shader a couple ways to improve the rendering time and quality are implemented.
- Importance sampling is used to improve the convergence time of the rendering
- Russian roulette is used to terminate rays before they reach their depth limit, the chance for a path to be terminated becomes bigger with each bounce. The rays that do make it further are in turn weighted heavier than the ones that do not.
- Antialiasing is implemented by offsetting each ray by a tiny sub-pixel amount. this fuzzes the edges enough to make them seem smooth.
- The program supports progressive rendering by averaging the pixels from previous frames together.
- A skybox is included to provide global illumination and some nice scenery
- Bounding volume hierarchies are used as acceleration structures to decrease the amount of ray triangle intersections that have to be done for every ray.
  - The implemented BVH traversal algorithm is found in [Efficient Stack-less BVH Traversal for Ray Tracing](https://www.sci.utah.edu/~wald/Publications/2011/StackFree/sccg2011.pdf) by Hapala et al.

Currently, the following material options are supported:
- Fully diffuse materials
- Fully specular materials
- Dielectric materials
- Glossy materials (mix of specular and diffuse)
- 
## How to use
You can move around in the scene using the ```WASD``` buttons, the ```spacebar``` and ```left shift``` are used to go up and down respectively.\
To lock the camera in place and let the render progress even if you touch the keyboard or mouse accidentally, you can press the ```L``` button.\
To take a screenshot of the current frame you can press the ```I``` button.\
To exit the program press ```ESC```.

## Future work
- Next event estimation, to substantially reduce the convergence time.
- Different types of bounding volume hierarchy construction. Currently the bounding volumes are split using the spatial split method. A more optimized way would be to use the surface area heuristic.
- Splitting the compute shader source code into multiple files. Currently the compute shader resides in one big file, this makes it hard to edit and is not very DRY.
