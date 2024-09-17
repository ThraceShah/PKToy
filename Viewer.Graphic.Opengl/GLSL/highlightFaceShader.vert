#version 300 es

precision mediump float;
layout (location = 0) in vec4 vIn;
layout (location = 1) in vec3 normalIn;

uniform mat4 g_World; 
uniform mat4 g_View;  
uniform mat4 g_Proj;  
uniform mat4 g_Translation;
uniform mat4 g_Origin;


uniform mat3 g_WIT;


out vec3 fs_normal;
out vec3 fs_posW;

void main()
{
    vec3 posL=vIn.xyz;
    vec4 orig=g_Origin*vec4(posL,1.0);
    vec4 pos=g_World*orig;
    fs_normal = normalize(g_WIT*normalIn);
    fs_posW = pos.xyz;
    gl_Position=g_Proj*g_View*pos*g_Translation;
}