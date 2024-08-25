#version 300 es

precision mediump float;

in vec4 vout;
out vec4 FragColor;

void main()
{
    FragColor = vout;
}
