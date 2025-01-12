#version 300 es

precision mediump float;
layout (location = 0) in vec4 vIn;

uniform mat4 g_World; 
uniform mat4 g_View;  
uniform mat4 g_Proj;  
uniform mat4 g_Translation;
uniform mat4 g_Origin;


void main()
{
    vec3 posL=vIn.xyz;
    vec4 orig=g_Origin*vec4(posL,1.0);
    vec4 pos=g_World*orig;
    gl_Position=g_Proj*g_View*pos*g_Translation;
}