using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using JourneyOfThePrairieKing;
using CG_PR3;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace JourneyOfThePrairieKing
{
   public class Game : GameWindow
   {
      private const int _gameWidth = 1920;
      private const int _gameHeight = 1080;
      private const double _maxDeltaTime = 1.0 / 30.0;
      private const double _minDeltaTime = 1.0 / 120.0;


      public Shader _textureShader;
      public Texture _textureMap0;
      public Character _mainCharacter;

      public Enemy enemy1;

      Texture _textureMap1;

      private double _lastFrameTime = 0;



      public Game(int width = _gameWidth, int height = _gameHeight, string title = "CG_PR1")
          : base(
                GameWindowSettings.Default,
                new NativeWindowSettings()
                {
                   Title = title,
                   //Size = (width, height),
                   WindowState = WindowState.Fullscreen,
                   //WindowBorder = WindowBorder.Fixed,
                   StartVisible = false,
                   StartFocused = true,
                   API = ContextAPI.OpenGL,
                   Profile = ContextProfile.Core,
                   APIVersion = new Version(4, 6)
                })
      {
         CenterWindow();
      }

      protected override void OnResize(ResizeEventArgs e)
      {
         base.OnResize(e);

         //GL.Viewport((e.Width - gameWidthInPixels) / 2, (e.Height - gameHeightInPixels) / 2, gameWidthInPixels, gameHeightInPixels);
         GL.Viewport(0, 0, _gameWidth, _gameHeight);
      }

      protected override void OnLoad()
      {
         IsVisible = true;
         GL.Enable(EnableCap.Blend);
         GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
         GL.PointSize(10.0f);
         GL.Enable(EnableCap.LineSmooth);
         GL.ClearColor(Color4.Gray);

         _textureShader = new Shader("data/shaders/textureShader.vert", "data/shaders/textureShader.frag");
         _textureMap0 = Texture.LoadFromFile("data/textures/char1.png");
         _textureMap1 = Texture.LoadFromFile("data/textures/enemy1.png");

         _mainCharacter = new Character();

         enemy1 = new Enemy();

         base.OnLoad();
      }

      protected override void OnUnload()
      {


         base.OnUnload();
      }

      protected override void OnUpdateFrame(FrameEventArgs args)
      {
         double currentTime = args.Time;
         double deltaTime = currentTime - _lastFrameTime;

         //Console.WriteLine(deltaTime);
         deltaTime = Math.Min(deltaTime, _maxDeltaTime);
         deltaTime = Math.Max(deltaTime, _minDeltaTime);

         var previousPosiiton = _mainCharacter.Position;

         if (IsKeyDown(Keys.A))
         {
            _mainCharacter.Position += _mainCharacter.MoveSpeed * new Vector2(-2000.0f / 1920, 0) * (float)deltaTime;
         }
         if (IsKeyDown(Keys.D))
         {
            _mainCharacter.Position += _mainCharacter.MoveSpeed * new Vector2(2000.0f / 1920, 0) * (float)deltaTime;
         }
         if (IsKeyDown(Keys.W))
         {
            _mainCharacter.Position += _mainCharacter.MoveSpeed * new Vector2(0, 2000.0f / 1080) * (float)deltaTime;
         }
         if (IsKeyDown(Keys.S))
         {
            _mainCharacter.Position += _mainCharacter.MoveSpeed * new Vector2(0, -2000.0f / 1080) * (float)deltaTime;
         }
;
         if (Collision.Compute(_mainCharacter, enemy1) is true)
         {
            _mainCharacter.Position = previousPosiiton;
            Console.WriteLine("Collision!");
         }

         _lastFrameTime = currentTime;
         base.OnUpdateFrame(args);
      }

      protected override void OnRenderFrame(FrameEventArgs args)
      {
         #region Character

         GL.Clear(ClearBufferMask.ColorBufferBit);
         _textureShader.Use();
         var modelMatrix = Matrix4.CreateTranslation(new Vector3(_mainCharacter.Position.X, _mainCharacter.Position.Y, 0.0f));

         _textureShader.SetMatrix4("model", modelMatrix);
         _textureMap0.Use(TextureUnit.Texture0);
         _textureShader.SetInt("textureMap", 0);

         _mainCharacter.Draw();

         #endregion Character


         #region Enemies
         enemy1.Position = new Vector2(-0.5f, -0.8f);

         var modelMatrixEnemy = Matrix4.CreateTranslation(new Vector3(enemy1.Position.X, enemy1.Position.Y, 0.0f));

         _textureShader.SetMatrix4("model", modelMatrixEnemy);
         _textureMap1.Use(TextureUnit.Texture1);
         _textureShader.SetInt("textureMap", 1);

         enemy1.Draw();
         #endregion Enemies

         SwapBuffers();
         base.OnRenderFrame(args);
      }

      protected override void OnMouseDown(MouseButtonEventArgs e)
      {

         base.OnMouseDown(e);
      }

      protected override void OnKeyDown(KeyboardKeyEventArgs e)
      {

         base.OnKeyDown(e);
      }
   }
}