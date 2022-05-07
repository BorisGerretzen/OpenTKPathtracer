# OpenTK Pathtracer
This is a path tracing algorithm implemented as an OpenGL compute shader, with OpenTK providing the OpenGL bindings.

## Capabilities
In the compute shader a couple ways to improve the rendering time and quality are implemented.
- Importance sampling is used to improve the convergence time of the rendering
- Russian roulette is used to terminate rays before they reach their depth limit, the chance for a path to be terminated becomes bigger with each bounce. The rays that do make it further are in turn weighted heavier than the ones that do not.
- Antialiasing is implemented by offsetting each ray by a tiny sub-pixel amount. this fuzzes the edges enough to make them seem smooth.
- The program supports progressive rendering by averaging the pixels from previous frames together.
- A skybox is included to provide global illumination and some nice scenery

Currently, the following material options are supported:
- Fully diffuse materials
- Fully specular materials
- Glossy materials (mix of specular and diffuse)
- Refractive materials

## How to use
You can move around in the scene using the ```WASD``` buttons, the ```spacebar``` and ```left shift``` are used to go up and down respectively.\
To lock the camera in place and let the render progress even if you touch the keyboard or mouse accidentally, you can press the ```L``` button.\
To take a screenshot of the current frame you can press the ```I``` button.\
To exit the program press ```ESC```.

