using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JourneyOfThePrairieKing
{
   public class MainInterface
   {
      #region HitPoints

      private VertexPositionTexture[] _verticesHitpoints;
      private VertexBufferObject _vboHitpoints;
      private VertexArrayObject _vaoHitpoints;

      public Vector2 SizeHitpoints { get; init; }
      public Vector2 PositionHitpoints { get; init; }

      #endregion

      #region digits

      private VertexPositionTexture[][] _verticesNumX;
      private VertexBufferObject[] _vboNumX;
      private VertexArrayObject[] _vaoNumX;

      public Vector2 SizeDigit { get; init; }
      public Vector2 PositionDigit { get; init; }

      #endregion

      #region Screens

      private VertexPositionTexture[] _verticesScreen;
      private VertexBufferObject _vboScreen;
      private VertexArrayObject _vaoScreen;

      public Vector2 SizeScreen { get; init; }
      public Vector2 PositionScreen { get; init; }

      #endregion

      public MainInterface()
      {
         SizeHitpoints = new Vector2(64.0f / 1920, 128.0f / 1080);
         PositionHitpoints = new Vector2(-0.7f, 0.8f);

         SizeDigit = new Vector2(32.0f / 1920, 64.0f / 1080);
         PositionDigit = new Vector2(-0.7f, 0.7f);

         SizeScreen = new Vector2(1000.0f / 1920 * 2.0f, 1000.0f / 1080 * 2.0f);
         PositionScreen = new Vector2(-0.5f, -0.95f);

         _verticesHitpoints = new VertexPositionTexture[]
         {
            new (new Vector2(0, 0), new Vector2(0.0f, 0.0f)),
            new (new Vector2(SizeHitpoints.X, 0), new Vector2(1.0f, 0.0f)),
            new (new Vector2(0, SizeHitpoints.Y), new Vector2(0.0f, 1.0f)),
            new (new Vector2(SizeHitpoints.X, SizeHitpoints.Y), new Vector2(1.0f, 1.0f))
         };

         _vboHitpoints = new VertexBufferObject(_verticesHitpoints, BufferUsageHint.StreamDraw);
         _vboHitpoints.Bind();

         _vaoHitpoints = new VertexArrayObject();
         _vaoHitpoints.Bind();

         _verticesNumX = new VertexPositionTexture[10][];
         _vboNumX = new VertexBufferObject[10];
         _vaoNumX = new VertexArrayObject[10];

         var sizeNumX = 1.0f / 10;
         for (int i = 1; i < _verticesNumX.Length; i++)
         {
            _verticesNumX[i] = new VertexPositionTexture[]
            {
               new (new Vector2(0, 0), new Vector2(sizeNumX * (i - 1), 0.0f)),
               new (new Vector2(SizeDigit.X, 0), new Vector2(sizeNumX * i, 0.0f)),
               new (new Vector2(0, SizeDigit.Y), new Vector2(sizeNumX * (i - 1), 1.0f)),
               new (new Vector2(SizeDigit.X, SizeDigit.Y), new Vector2(sizeNumX * i, 1.0f))
            };

            _vboNumX[i] = new VertexBufferObject(_verticesNumX[i], BufferUsageHint.StreamDraw);
            _vboNumX[i].Bind();

            _vaoNumX[i] = new VertexArrayObject();
            _vaoNumX[i].Bind();
         }

         _verticesNumX[0] = new VertexPositionTexture[]
         {
               new (new Vector2(0, 0), new Vector2(1.0f - sizeNumX, 0.0f)),
               new (new Vector2(SizeDigit.X, 0), new Vector2(1.0f, 0.0f)),
               new (new Vector2(0, SizeDigit.Y), new Vector2(1.0f - sizeNumX, 1.0f)),
               new (new Vector2(SizeDigit.X, SizeDigit.Y), new Vector2(1.0f, 1.0f))
         };


         _vboNumX[0] = new VertexBufferObject(_verticesNumX[0], BufferUsageHint.StreamDraw);
         _vboNumX[0].Bind();

         _vaoNumX[0] = new VertexArrayObject();
         _vaoNumX[0].Bind();




         _verticesScreen = new VertexPositionTexture[]
         {
            new (new Vector2(0, 0), new Vector2(0.0f, 0.0f)),
            new (new Vector2(SizeScreen.X, 0), new Vector2(1.0f, 0.0f)),
            new (new Vector2(0, SizeScreen.Y), new Vector2(0.0f, 1.0f)),
            new (new Vector2(SizeScreen.X, SizeScreen.Y), new Vector2(1.0f, 1.0f))
         };


         _vboScreen = new VertexBufferObject(_verticesScreen, BufferUsageHint.StreamDraw);
         _vboScreen.Bind();

         _vaoScreen = new VertexArrayObject();
         _vaoScreen.Bind();

      }

      public void DrawHitPoints()
      {
         _vaoHitpoints.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _verticesHitpoints.Length);
      }

      public void DrawDigit(int digit)
      {
         if (digit < 0 || digit > 9)
            return;

         _vaoNumX[digit].Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _verticesNumX[digit].Length);
      }

      public void DrawScreen()
      {
         _vaoScreen.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _verticesScreen.Length);
      }
   }
}
