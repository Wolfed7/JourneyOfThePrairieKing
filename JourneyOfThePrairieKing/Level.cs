using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace JourneyOfThePrairieKing
{
   public class Level
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public Vector2 Size { get; init; }
      public Vector2 Position { get; init; }

      public Level(Vector2 position, Vector2 size)
      {
         Size = size;
         Position = position;

         _vertices = new VertexPositionTexture[]
         {
            new (new Vector2(0, 0), new Vector2(0.0f, 0.0f)),
            new (new Vector2(Size.X, 0), new Vector2(1.0f, 0.0f)),
            new (new Vector2(0, Size.Y), new Vector2(0.0f, 1.0f)),
            new (new Vector2(Size.X, Size.Y), new Vector2(1.0f, 1.0f))
         };

         _vbo = new VertexBufferObject(_vertices, BufferUsageHint.StaticDraw);
         _vbo.Bind();

         _vao = new VertexArrayObject();
         _vao.Bind();
      }

      public void Draw()
      {
         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertices.Length);
      }
   }
}