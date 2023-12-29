#version 460 core

in vec2 vTexCoord;

out vec4 FragColor;

uniform sampler2D textureMap;

void main()
{
   vec4 eTexture;
   eTexture = vec4(texture(textureMap, vTexCoord));
   
   FragColor = eTexture;
}

