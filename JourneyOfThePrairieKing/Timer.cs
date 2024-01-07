using JourneyOfThePrairieKing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JourneyOfThePrairieKing.Enemy;

namespace JourneyOfThePrairieKing
{
   public class Timer
   {
      private long _timeRemains;

      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public Vector2 Size { get; init; }
      public Vector2 Position { get; init; }
      public long LevelPlaytime { get; init; }

      public bool isTimeEnds;

      public Timer(long milliseconds, Vector2 position, Vector2 size)
      {
         isTimeEnds = false;
         LevelPlaytime = milliseconds;
         Size = size;
         Position = position;
         _timeRemains = milliseconds;

         _vertices = new VertexPositionTexture[]
         {
            new (new Vector2(0, 0), new Vector2(0.0f, 0.0f)),
            new (new Vector2(Size.X, 0), new Vector2(1.0f, 0.0f)),
            new (new Vector2(0, Size.Y), new Vector2(0.0f, 1.0f)),
            new (new Vector2(Size.X, Size.Y), new Vector2(1.0f, 1.0f))
         };

         _vbo = new VertexBufferObject(_vertices, BufferUsageHint.StreamDraw);
         _vbo.Bind();

         _vao = new VertexArrayObject();
         _vao.Bind();
      }

      public void Update(long elapsedMilliseconds)
      {
         //Console.WriteLine(_timeRemains);
         if (isTimeEnds is true)
            return;
         
         _timeRemains -= elapsedMilliseconds;

         if (_timeRemains <= 0)
            isTimeEnds = true;
      }

      public void Reset()
      {
         _timeRemains = LevelPlaytime;
         isTimeEnds = false;
      }

      public void Render(Shader shader, Texture texture)
      {
         var modelMatrix = Matrix4.CreateTranslation(new Vector3(Position.X, Position.Y, 0.0f));
         var scaleMatrix = Matrix4.CreateScale(new Vector3(ViewScale(), 1.0f, 1.0f));

         shader.SetMatrix4("model", scaleMatrix * modelMatrix);

         texture.Use(TextureUnit.Texture0);
         shader.SetInt("textureMap", 0);

         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
      }

      private float ViewScale()
      {
         return (float)_timeRemains / LevelPlaytime;
      }
   }
}
