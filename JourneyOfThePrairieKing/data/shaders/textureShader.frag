﻿#version 460 core

in vec2 vTexCoord;

out vec4 FragColor;

uniform sampler2D textureMap;

void main()
{
   vec4 eTexture;
   eTexture = vec4(texture(textureMap, vTexCoord));
   
   FragColor = eTexture;// * vec4(1.0, 0.0, 0.0, 1.0);

//   vec3 eTexture;
//   eTexture = vec3(texture(textureMap, vTexCoord));
//   
//   FragColor = vec4(eTexture, 1.0);
}

