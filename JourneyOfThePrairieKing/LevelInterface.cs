using ImGuiNET;
using JourneyOfThePrairieKing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JourneyOfThePrairieKing
{
   public class LevelInterface
   {
      protected Texture _texture;

      protected VertexPositionTexture[][] _vertices;
      protected VertexBufferObject[] _vbo;
      protected VertexArrayObject[] _vao;

      public Vector2 Size { get; init; }
      public Vector2 Position { get; init; }

      public LevelInterface(Vector2 size, Vector2 position, Texture texture, int ElementsInTexture = 1)
      {
         Size = size;
         Position = position;
         _texture = texture;

         _vertices = new VertexPositionTexture[ElementsInTexture][];
         _vbo = new VertexBufferObject[ElementsInTexture];
         _vao = new VertexArrayObject[ElementsInTexture];

         float elementSize = 1.0f / ElementsInTexture;
         for (int i = 0; i < ElementsInTexture; i++)
         {
            _vertices[i] = new VertexPositionTexture[]
            {
               new (new Vector2(0, 0), new Vector2(elementSize * i, 0.0f)),
               new (new Vector2(Size.X, 0), new Vector2(elementSize * (i + 1), 0.0f)),
               new (new Vector2(0, Size.Y), new Vector2(elementSize * i, 1.0f)),
               new (new Vector2(Size.X, Size.Y), new Vector2(elementSize * (i + 1), 1.0f))
            };

            _vbo[i] = new VertexBufferObject(_vertices[i], BufferUsageHint.StaticDraw);
            _vbo[i].Bind();

            _vao[i] = new VertexArrayObject();
            _vao[i].Bind();
         }
      }

      public virtual void Render(Shader shader, int elementNumber = 0)
      {
         Texture.DrawTexturedRectangle(shader, _texture, Position, _vao[elementNumber]);
      }
   }

   public class Counter : LevelInterface
   {
      public Counter(Vector2 position, Vector2 size, Texture texture, int ElementsInTexture = 0) 
         : base(position, size, texture, ElementsInTexture)
      {

      }

      public override void Render(Shader shader, int elementNumber = 0)
      {
         if (elementNumber < 0)
         {
            Texture.DrawTexturedRectangle(shader, _texture, Position, _vao[0]);
            return;
         }
         int digit;
         int res = elementNumber;
         List<int> digits = new List<int>();

         while (res != 0)
         {
            digit = res % 10;
            digits.Add(digit);
            res /= 10;
         }

         if (elementNumber == 0)
            digits.Add(0);

         for (int i = digits.Count - 1; i >= 0; i--)
         {
            var position = Position + new Vector2((float)((digits.Count - i - 1) * Size.X), 0);
            Texture.DrawTexturedRectangle(shader, _texture, position, _vao[digits[i]]);
         }
      }
   }
}
