#version 460 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;

out vec2 vTexCoord;

uniform mat4 model;

void main()
{
   gl_Position = vec4(aPosition.x, aPosition.y, 0.0f, 1.0f) * model;
   vTexCoord = aTexCoord;
}