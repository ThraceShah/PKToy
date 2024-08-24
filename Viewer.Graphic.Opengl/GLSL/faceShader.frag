#version 330 core

// uniform vec3 lightPos; 
// uniform vec3 lightColor;
uniform vec4 objectColor;
// uniform mat3 normalModel;


in GS_Out{
    vec3 gs_normal;
    vec3 gs_posW;
} gout;


out vec4 FragColor;

void main()
{

    vec3 lightPos=vec3(5,5,-10); 
    vec3 lightColor=vec3(1,1,1);
    // vec3 objectColor=vec3(0.5882353, 0.5882353, 0.5882353);

        // ambient
    float ambientStrength = 0.3;
    vec3 ambient = ambientStrength * lightColor;
  	
    // diffuse 
    vec3 lightDir = normalize(lightPos - gout.gs_posW);
    float diff = max(dot(gout.gs_normal, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;
            
    vec3 result = (ambient + diffuse) * objectColor.xyz;
    FragColor = vec4(result, objectColor.w);
}
