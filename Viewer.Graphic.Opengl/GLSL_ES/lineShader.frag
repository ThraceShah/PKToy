#version 300 es

precision highp float;

uniform vec4 objectColor;
out vec4 FragColor;

void main()
{
    FragColor = objectColor;
}
