#version 300 es

precision mediump float;

uniform vec4 objectColor;

in vec3 fs_normal;
in vec3 fs_posW;

out vec4 FragColor;

void main()
{

    vec3 lightPos=vec3(5,5,-10); 
    vec3 lightColor=vec3(1,1,1);
        // ambient
    float ambientStrength = 0.3;
    vec3 ambient = ambientStrength * lightColor;
  	
    // diffuse 
    vec3 lightDir = normalize(lightPos - objectColor.xyz);
    float diff = max(dot(fs_normal, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;
            
    vec3 result = (ambient + diffuse) * objectColor.xyz;
    FragColor = vec4(result, objectColor.w);
}
