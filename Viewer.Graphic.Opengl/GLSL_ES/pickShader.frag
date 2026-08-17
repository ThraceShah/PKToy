#version 300 es

precision highp float;

in vec4 vout;
out vec4 FragColor;

void main()
{
    FragColor = vout;
}
