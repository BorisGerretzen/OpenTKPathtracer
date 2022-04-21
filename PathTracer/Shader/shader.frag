#version 330 core
out vec4 FragColor;

void main()
{
    vec3 color = vec3(0.08f, 0.39f, 0.89f);
    FragColor = vec4(color, 1.0f);
}