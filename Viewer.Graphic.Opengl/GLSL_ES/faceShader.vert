#version 300 es

precision mediump float;
layout(location = 0) in vec3 vIn;
layout(location = 2) in vec3 normalIn;
layout(location = 3) in uint colorIn;

uniform mat4 g_World;
uniform mat4 g_View;
uniform mat4 g_Proj;
uniform mat4 g_Translation;
uniform mat4 g_Origin;

uniform mat3 g_WIT;

out vec3 fs_normal;
out vec3 fs_posW;
flat out uint fs_color;

void main()
{
    vec3 posL = vIn.xyz;
    vec4 orig = g_Origin * vec4(posL, 1.0);
    vec4 pos = g_World * orig;
    fs_normal = normalize(g_WIT * normalIn);
    fs_posW = pos.xyz;
    fs_color = colorIn;
    gl_Position = g_Proj * g_View * pos * g_Translation;
}
