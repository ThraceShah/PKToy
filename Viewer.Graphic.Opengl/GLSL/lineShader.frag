#version 300 es

precision mediump float;

uniform vec4 objectColor;
out vec4 FragColor;

void main()
{
    FragColor = objectColor;
}
