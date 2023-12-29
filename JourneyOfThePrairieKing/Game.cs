using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using JourneyOfThePrairieKing;
using CG_PR3;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace JourneyOfThePrairieKing
{
   public class Game : GameWindow
   {
      #region Constants

      private const float _gameSpeed = 1500.0f;
      private const int _gameWidth = 1920;
      private const int _gameHeight = 1080;
      private const double _maxDeltaTime = 1.0 / 30.0;
      private const double _minDeltaTime = 1.0 / 120.0;

      #endregion

      private Stopwatch _timer;

      private long _lastShotTime;

      private Shader _textureShader;
      private Texture _textureMap0;
      private Character _mainCharacter;

      private HashSet<Enemy> _enemies;
      private HashSet<Projectile> _projectiles;

      private Texture _textureMap1;

      private double _lastFrameTime = 0;

      private Texture _textureMap2;


      public Game(int width = _gameWidth, int height = _gameHeight, string title = "CG_PR1")
          : base(
                GameWindowSettings.Default,
                new NativeWindowSettings()
                {
                   Title = title,
                   //Size = (width, height),
                   WindowState = WindowState.Fullscreen,
                   WindowBorder = WindowBorder.Fixed,
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
         WindowState = WindowState.Fullscreen;
         IsVisible = true;
         GL.Enable(EnableCap.Blend);
         GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
         GL.PointSize(10.0f);
         GL.Enable(EnableCap.LineSmooth);
         GL.ClearColor(Color4.Gray);

         _textureShader = new Shader("data/shaders/textureShader.vert", "data/shaders/textureShader.frag");
         _textureMap0 = Texture.LoadFromFile("data/textures/char1.png");
         _textureMap1 = Texture.LoadFromFile("data/textures/enemy2.png");
         _textureMap2 = Texture.LoadFromFile("data/textures/projectile1.png");

         _mainCharacter = new Character();
         _enemies = new HashSet<Enemy>();
         _projectiles = new HashSet<Projectile>();

         _timer = Stopwatch.StartNew();
         _lastShotTime = 0;

         base.OnLoad();
      }

      protected override void OnUnload()
      {

         base.OnUnload();
      }

      protected override void OnUpdateFrame(FrameEventArgs args)
      {
         // TODO: fix delta time
         double currentTime = args.Time;
         double deltaTime = currentTime - _lastFrameTime;

         //Console.WriteLine(deltaTime);
         deltaTime = Math.Min(deltaTime, _maxDeltaTime);
         deltaTime = Math.Max(deltaTime, _minDeltaTime);

         foreach (var enemy in _enemies)
         {
            var distanceToChar = _mainCharacter.Position - enemy.Position;
            distanceToChar.Normalize();
            enemy.Position += enemy.MoveSpeed * distanceToChar * (float)deltaTime;
         }

         var previousPosiiton = _mainCharacter.Position;

         if (_enemies.Count < 10)
         {
            SpawnEnemy();
         }


         #region Movement


         float moveX = 0f;
         float moveY = 0f;

         if (IsKeyDown(Keys.A))
         {
            moveX = -2000.0f;
            
         }
         if (IsKeyDown(Keys.D))
         {
            moveX = 2000.0f;
         }
         if (IsKeyDown(Keys.W))
         {
            moveY = 2000.0f;
         }
         if (IsKeyDown(Keys.S))
         {
            moveY = -2000.0f;
         }
         
         //Console.WriteLine(moveX + " " + moveY);

         if (moveX != 0f || moveY != 0f)
         {
            Vector2 moveDir = new Vector2(moveX, moveY);
            moveDir.Normalize();
            _mainCharacter.Position += moveDir * _mainCharacter.MoveSpeed * (float)deltaTime;
         }

         foreach (var enemy in _enemies)
         {
            if (Collision.Compute(_mainCharacter, enemy) is true)
            {
               _mainCharacter.Position = previousPosiiton;
               //Console.WriteLine("Collision!");
            }
         }

         #endregion


         #region Shoot

         //Console.WriteLine(_timer.ElapsedMilliseconds + " " + _lastShotTime);

         if (_timer.ElapsedMilliseconds - _lastShotTime > _mainCharacter.ReloadTime)
         {
            float projDirX = 0.0f;
            float projDirY = 0.0f;

            if (IsKeyDown(Keys.Left))
            {
               projDirX = -1.0f;
            }
            if (IsKeyDown(Keys.Right))
            {
               projDirX = 1.0f;
            }
            if (IsKeyDown(Keys.Up))
            {
               projDirY = 1.0f;
            }
            if (IsKeyDown(Keys.Down))
            {
               projDirY = -1.0f;
            }

            if (projDirX != 0.0f || projDirY != 0.0f)
            {
               var projectileDir = new Vector2(projDirX, projDirY);
               projectileDir.Normalize();
               SpawnProjectile(_mainCharacter.Position, projectileDir);
               _lastShotTime = _timer.ElapsedMilliseconds;
            }
         }

         Console.WriteLine(_projectiles.Count);

         // TODO: long living projectiles bug
         foreach (var projectile in _projectiles)
         {
            projectile.Position += projectile.MoveSpeed * projectile.Direction;

            if (projectile.Position.X < -1.0f || projectile.Position.X > 1.0f
               || projectile.Position.Y < -1.0f || projectile.Position.Y > 1.0f)
            {
               _projectiles.Remove(projectile);
               break;
            }

            foreach (var enemy in _enemies)
            {
               if (Collision.Compute(projectile, enemy) is true)
               {
                  _enemies.Remove(enemy);
                  _projectiles.Remove(projectile);
                  //Console.WriteLine("enemy down!");
                  break;
               }
            }
         }

         #endregion 

         _lastFrameTime = currentTime;
         base.OnUpdateFrame(args);
      }

      protected override void OnRenderFrame(FrameEventArgs args)
      {
         #region Character

         {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            _textureShader.Use();
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(_mainCharacter.Position.X, _mainCharacter.Position.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrix);
            _textureMap0.Use(TextureUnit.Texture0);
            _textureShader.SetInt("textureMap", 0);

            _mainCharacter.Draw();
         }

         #endregion

         #region Enemies
         foreach (var enemy in _enemies)
         {
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(enemy.Position.X, enemy.Position.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrix);
            _textureMap1.Use(TextureUnit.Texture1);
            _textureShader.SetInt("textureMap", 1);

            enemy.Draw();
         }
         #endregion

         #region Projectiles
         foreach (var projectile in _projectiles)
         {
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(projectile.Position.X, projectile.Position.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrix);
            _textureMap2.Use(TextureUnit.Texture2);
            _textureShader.SetInt("textureMap", 2);

            projectile.Draw();
         }
         #endregion

         SwapBuffers();
         base.OnRenderFrame(args);
      }

      protected override void OnMouseDown(MouseButtonEventArgs e)
      {

         base.OnMouseDown(e);
      }

      protected override void OnKeyDown(KeyboardKeyEventArgs e)
      {

         #region Window management
         switch (e.Key)
         {
            case Keys.F:
            {
               WindowState = WindowState == WindowState.Fullscreen ? WindowState.Maximized : WindowState.Fullscreen;
               Console.WriteLine(WindowState);
               break;
            }

            case Keys.Escape:
            {
               this.Close();
               break;
            }
         }
         #endregion

         base.OnKeyDown(e);
      }



      private void SpawnProjectile(Vector2 position, Vector2 direction)
      {
         //_projectiles.Add(new Projectile(position, direction, 1));

         if (true)
         {
            for (int i = -1; i < 2; i++)
            {
               for (int j = -1; j < 2; j++)
               {
                  direction = new Vector2(i, j);
                  direction.Normalize();
                  _projectiles.Add(new Projectile(position, direction, 1));
               }
            }
         }
      }


      private void SpawnEnemy()
      {
         var spawnPositions = new List<Vector2>()
         {
            // bottom
            new (0.0f, -0.9f),
            new (-0.1f, -0.9f),
            new (0.1f, -0.9f),

            // left
            new (-0.9f, 0.0f),
            new (-0.9f, -0.1f),
            new (-0.9f, 0.1f),

            // up
            new (0.0f, 0.9f),
            new (-0.1f, 0.9f),
            new (0.1f, 0.9f),

            // right
            new (0.9f, 0.0f),
            new (0.9f, -0.1f),
            new (0.9f, 0.1f),
         };

         for (int i = 0; i < spawnPositions.Count; i++)
         {
            Enemy enemy = new Enemy();
            enemy.Position = spawnPositions[i];

            _enemies.Add(enemy);
         }
      }
   }
}