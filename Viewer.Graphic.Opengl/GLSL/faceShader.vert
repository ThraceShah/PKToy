#version 300 es

precision mediump float;
layout (location = 0) in vec4 vIn;
layout (location = 1) in vec3 normalIn;

uniform mat4 g_World; 
uniform mat4 g_View;  
uniform mat4 g_Proj;  
uniform mat4 g_Translation;
uniform mat4 g_Origin;



// out VS_Out{
//     vec3 origW;
//     vec3 posW;
// } vout;

uniform mat3 g_WIT;


// out GS_Out{
//     vec3 gs_normal;
//     vec3 gs_posW;
// } vout;


out vec3 gs_normal;
out vec3 gs_posW;

void main()
{
    // vout.wit=mat3(g_WIT);
    vec3 posL=vIn.xyz;
    vec4 orig=g_Origin*vec4(posL,1.0);
    vec4 pos=g_World*orig;
    // vout.origW=orig.xyz;
    // vout.posW=pos.xyz;
    gs_normal = normalize(g_WIT*normalIn);
    gs_posW = pos.xyz;

    gl_Position=g_Proj*g_View*pos*g_Translation;
}