using CG_PR3;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JourneyOfThePrairieKing
{
   public class Interface
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

      #region coin Icon

      private VertexPositionTexture[] _verticesCoin;
      private VertexBufferObject _vboCoin;
      private VertexArrayObject _vaoCoin;

      public Vector2 SizeCoin { get; init; }
      public Vector2 PositionCoin { get; init; }

      #endregion

      #region Coin counter

      public Vector2 PositionCoinCounter { get; init; }

      #endregion

      #region Screens

      private VertexPositionTexture[] _verticesScreen;
      private VertexBufferObject _vboScreen;
      private VertexArrayObject _vaoScreen;

      public Vector2 SizeScreen { get; init; }
      public Vector2 PositionScreen { get; init; }

      #endregion

      public Interface(Vector2 mapSize, Vector2 mapPosition, Vector2 winSize)
      {
         SizeHitpoints = Coordinates.SizeInNDC(new Vector2(32, 64), winSize);
         PositionHitpoints = Coordinates.PosInNDC(new Vector2(360, 900), winSize);

         SizeDigit = Coordinates.SizeInNDC(new Vector2(16, 32), winSize);
         PositionDigit = Coordinates.PosInNDC(new Vector2(368, 860), winSize);

         SizeCoin = Coordinates.SizeInNDC(new Vector2(32, 32), winSize);
         PositionCoin = Coordinates.PosInNDC(new Vector2(360, 800), winSize);

         PositionCoinCounter = Coordinates.PosInNDC(new Vector2(368, 760), winSize);

         SizeScreen = mapSize;
         PositionScreen = mapPosition;

         _verticesHitpoints = new VertexPositionTexture[]
         {
            new (new Vector2(0, 0), new Vector2(0.0f, 0.0f)),
            new (new Vector2(SizeHitpoints.X, 0), new Vector2(1.0f, 0.0f)),
            new (new Vector2(0, SizeHitpoints.Y), new Vector2(0.0f, 1.0f)),
            new (new Vector2(SizeHitpoints.X, SizeHitpoints.Y), new Vector2(1.0f, 1.0f))
         };

         _vboHitpoints = new VertexBufferObject(_verticesHitpoints, BufferUsageHint.StaticDraw);
         _vboHitpoints.Bind();

         _vaoHitpoints = new VertexArrayObject();
         _vaoHitpoints.Bind();


         _verticesCoin = new VertexPositionTexture[]
         {
            new (new Vector2(0, 0), new Vector2(0.0f, 0.0f)),
            new (new Vector2(SizeCoin.X, 0), new Vector2(1.0f, 0.0f)),
            new (new Vector2(0, SizeCoin.Y), new Vector2(0.0f, 1.0f)),
            new (new Vector2(SizeCoin.X, SizeCoin.Y), new Vector2(1.0f, 1.0f))
         };

         _vboCoin = new VertexBufferObject(_verticesCoin, BufferUsageHint.StaticDraw);
         _vboCoin.Bind();

         _vaoCoin = new VertexArrayObject();
         _vaoCoin.Bind();

         _verticesNumX = new VertexPositionTexture[10][];
         _vboNumX = new VertexBufferObject[10];
         _vaoNumX = new VertexArrayObject[10];

         var sizeNumX = 1.0f / 10.0f;
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

      public void RenderHitpointIcon(Shader shader, Texture texture)
      {
         Texture.DrawTexturedRectangle(shader, texture, PositionHitpoints, _vaoHitpoints);
      }

      public void RenderHitpointCounter(Shader shader, Texture texture, int digit)
      {
         if (digit < 0)
         {
            digit = 0;
         }
         else if(digit > 9)
         {
            digit = 9;
         }

         Texture.DrawTexturedRectangle(shader, texture, PositionDigit, _vaoNumX[digit]);
      }

      public void RenderScreen(Shader shader, Texture? texture)
      {
         if (texture is not null)
            Texture.DrawTexturedRectangle(shader, texture, PositionScreen, _vaoScreen);
      }

      public void RenderCoinIcon(Shader shader, Texture texture)
      {
         Texture.DrawTexturedRectangle(shader, texture, PositionCoin, _vaoCoin);
      }

      public void RenderCoinCounter(Shader shader, Texture texture, int number)
      {
         int digit;
         int res = number;
         List<int> digits = new List<int>();

         while (res != 0)
         {
            digit = res % 10;
            digits.Add(digit);
            res /= 10;
         }

         if (number == 0)
            digits.Add(0);

         for (int i = digits.Count - 1; i >= 0; i--)
         {
            var position = PositionCoinCounter + new Vector2((float)((digits.Count - i  - 1) * SizeDigit.X), 0);
            Texture.DrawTexturedRectangle(shader, texture, position, _vaoNumX[digits[i]]);
         }
      }


   }
}
