using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using JourneyOfThePrairieKing;
using CG_PR3;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using System.Drawing;
using System;

namespace JourneyOfThePrairieKing
{
   public enum GameState
   {
      Start,
      Run,
      Pause,
      GameOver,
   };

   public class Game : GameWindow
   {
      #region Constants

      private const float _gameSpeed = 1.0f;
      private const double _maxDeltaTime = 1.0 / 30.0;
      private const double _minDeltaTime = 1.0 / 120.0;

      #endregion

      private Vector2 WinRatio;
      private int _currentLevel;
      private GameState _gameState;
      private Stopwatch _gameRuntime;
      private Timer _levelTimer;
      private Dictionary<string, Texture> _textures;

      private Shader _textureShader;
      private Character _mainCharacter;
      private MainInterface _charInterface;

      private HashSet<Enemy> _enemies;
      private HashSet<Projectile> _projectiles;
      private HashSet<Obstacle> _boundaryObstacles;
      private List<Level> _levels;

      public Game(int width, int height, string title = "Game")
          : base(
                GameWindowSettings.Default,
                new NativeWindowSettings()
                {
                   Title = title,
                   ClientSize = (width, height),
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

         GL.Viewport(0, 0, Size.X, Size.Y);
      }

      protected override void OnLoad()
      {
         WindowState = WindowState.Fullscreen;
         IsVisible = true;
         GL.Enable(EnableCap.Blend);
         GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
         GL.PointSize(10.0f);
         GL.Enable(EnableCap.LineSmooth);
         GL.ClearColor(Color4.Black);

         _textureShader = new Shader("data/shaders/textureShader.vert", "data/shaders/textureShader.frag");
         _textures = new Dictionary<string, Texture>();

         _charInterface = new();
         _mainCharacter = new Character();
         _enemies = new HashSet<Enemy>();
         _projectiles = new HashSet<Projectile>();
         _levels = new List<Level>();

         {
            _boundaryObstacles = new HashSet<Obstacle>
            {
               // up
               new Obstacle(new Vector2(-0.5f, 0.8f), new Vector2(0.45f, 0.1f)),
               new Obstacle(new Vector2(-0.05f, 0.89f), new Vector2(0.2f, 0.01f)),
               new Obstacle(new Vector2(0.15f, 0.8f), new Vector2(0.4f, 0.1f)),

               // left
               new Obstacle(new Vector2(-0.5f, 0.1f), new Vector2(0.06f, 0.7f)),
               new Obstacle(new Vector2(-0.5f, -0.25f), new Vector2(0.01f, 0.35f)),
               new Obstacle(new Vector2(-0.5f, -0.85f), new Vector2(0.06f, 0.6f)),

               // bottom
               new Obstacle(new Vector2(-0.5f, -0.95f), new Vector2(0.45f, 0.1f)),
               new Obstacle(new Vector2(-0.05f, -0.95f), new Vector2(0.2f, 0.01f)),
               new Obstacle(new Vector2(0.15f, -0.95f), new Vector2(0.4f, 0.1f)),

               // right
               new Obstacle(new Vector2(0.48f, 0.1f), new Vector2(0.06f, 0.7f)),
               new Obstacle(new Vector2(0.53f, -0.25f), new Vector2(0.01f, 0.35f)),
               new Obstacle(new Vector2(0.48f, -0.85f), new Vector2(0.06f, 0.6f)),
            };

         }

         _currentLevel = 0;

         _gameRuntime = Stopwatch.StartNew();

         _gameState = GameState.Start;

         string[] allfiles = Directory.GetFiles("data/textures/");
         foreach (string filepath in allfiles)
         {
            _textures.Add(Path.GetFileNameWithoutExtension(filepath), Texture.LoadFromFile(filepath));
            Console.WriteLine(Path.GetFileName(filepath));
         }

         _levels.Add(new Level(new Vector2(ClientSize.X / 2.0f / ClientSize.X - 1.0f, -0.95f), new Vector2((ClientSize.Y - 80.0f) / ClientSize.X * 2.0f, (ClientSize.Y - 80.0f) / ClientSize.Y * 2.0f)));

         _levelTimer = new Timer(120000, new Vector2(-0.5f, 0.95f), new Vector2((ClientSize.Y - 80.0f) / ClientSize.X * 2.0f, 0.02f));

         WinRatio = new Vector2(ClientSize.Y, ClientSize.X);
         WinRatio.Normalize();

         base.OnLoad();
      }

      protected override void OnUnload()
      {

         base.OnUnload();
      }

      protected override void OnUpdateFrame(FrameEventArgs args)
      {
         if (_gameState is GameState.Pause
            || _gameState is GameState.Start
            || _gameState is GameState.GameOver)
         {
            return;
         }


         //Console.WriteLine((long)(args.Time * 1000));
         _levelTimer.Update((long)(args.Time * 1000));

         float deltaTime = (float)args.Time;

         //Console.WriteLine(deltaTime);
         deltaTime = (float)Math.Min(deltaTime, _maxDeltaTime);
         deltaTime = (float)Math.Max(deltaTime, _minDeltaTime);

         foreach (var enemy1 in _enemies)
         {
            var prevPos = enemy1.Position;
            var distanceToChar = _mainCharacter.Position - enemy1.Position;
            distanceToChar.Normalize();
            enemy1.Position += _gameSpeed * enemy1.Velocity * WinRatio * distanceToChar * deltaTime;

            foreach (var enemy2 in _enemies.Where(_ => _ != enemy1))
            {
               if (Entity.CheckCollision(enemy1, enemy2) is true)
               {
                  var vecToEnemy2 = enemy2.Position - enemy1.Position;
                  vecToEnemy2.Normalize();
                  enemy1.Position -= 2.0f * _gameSpeed * enemy1.Velocity * WinRatio * vecToEnemy2 * deltaTime;
                  //break;
               }
            }
         }

         var previousPosiiton = _mainCharacter.Position;

         if (_enemies.Count < 3)
         {
            SpawnEnemy();
         }

         //Console.WriteLine(_enemies.Count);

         #region Movement


         float moveX = 0.0f;
         float moveY = 0.0f;

         if (IsKeyDown(Keys.A))
         {
            moveX = -1;
            
         }
         if (IsKeyDown(Keys.D))
         {
            moveX = 1;
         }
         if (IsKeyDown(Keys.W))
         {
            moveY = 1;
         }
         if (IsKeyDown(Keys.S))
         {
            moveY = -1;
         }
         
         //Console.WriteLine(moveX + " " + moveY);

         if (moveX != 0.0f || moveY != 0.0f)
         {
            Vector2 moveDir = new Vector2(moveX, moveY);
            moveDir.Normalize();
            _mainCharacter.Position += _gameSpeed * _mainCharacter.Velocity * WinRatio * moveDir * deltaTime;

            foreach (var obstacle in _boundaryObstacles)
            {
               if (Entity.CheckCollision(_mainCharacter, obstacle) is true)
               {
                  //var vecToObstacle = (obstacle.Position + obstacle.Size / 2) - (_mainCharacter.Position + _mainCharacter.Size / 2);
                  //vecToObstacle.Normalize();
                  //_mainCharacter.Position -= 2.0f * _gameSpeed * _mainCharacter.Velocity * WinRatio * vecToObstacle * deltaTime;

                  _mainCharacter.Position -= 2.0f * _gameSpeed * _mainCharacter.Velocity * WinRatio * moveDir * deltaTime;

                  //Console.WriteLine("Collision with obstacle!");
               }
            }
         }

         foreach (var enemy in _enemies)
         {
            if(Entity.CheckCollision(_mainCharacter, enemy) is true)
            {
               _mainCharacter.GotDamage(1, _gameRuntime.ElapsedMilliseconds);
               if (_mainCharacter.HitPoints <= 0)
               {
                  _gameState = GameState.GameOver;
               }
               //Console.WriteLine("Collision!");
            }
         }

         #endregion


         #region Shoot

         //Console.WriteLine(_timer.ElapsedMilliseconds + " " + _lastShotTime);

         if (_gameRuntime.ElapsedMilliseconds - _mainCharacter.LastShotTime > _mainCharacter.ReloadTime)
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
               _mainCharacter.LastShotTime = _gameRuntime.ElapsedMilliseconds;
            }
         }

         //Console.WriteLine(_projectiles.Count);

         foreach (var projectile in _projectiles)
         {
            bool isProjectileAlive = true;
            projectile.Position += projectile.Velocity * projectile.Direction;


            foreach (var obstacle in _boundaryObstacles)
            {
               if (Entity.CheckCollision(projectile, obstacle) is true)
               {
                  _projectiles.Remove(projectile);
                  isProjectileAlive = false;
                  break;
               }
            }

            if (isProjectileAlive is false)
            {
               break;
            }

            foreach (var enemy in _enemies)
            {
               if(Entity.CheckCollision(projectile, enemy) is true)
               //if (Collision.Compute(projectile, enemy) is true)
               {
                  enemy.GotDamage(_mainCharacter.Damage + projectile.Damage);
                  if (enemy.HitPoints <= 0)
                  {
                     _enemies.Remove(enemy);
                  }
                  _projectiles.Remove(projectile);
                  //Console.WriteLine("enemy down!");
                  break;
               }
            }
         }

         #endregion 

         base.OnUpdateFrame(args);
      }

      protected override void OnRenderFrame(FrameEventArgs args)
      {
         GL.Clear(ClearBufferMask.ColorBufferBit);
         _textureShader.Use();

         {
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(_levels[_currentLevel].Position.X, _levels[_currentLevel].Position.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrix);
            _textures["level1"].Use(TextureUnit.Texture0);
            _textureShader.SetInt("textureMap", 0);

            _levels[_currentLevel].Draw();
         }

         #region Character
         {
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(_mainCharacter.Position.X, _mainCharacter.Position.Y, 0.0f));
            //var rotateMatrix = Matrix4.CreateRotationY((float)Math.PI);

            _textureShader.SetMatrix4("model", modelMatrix);
            _textures["char1"].Use(TextureUnit.Texture0);
            _textureShader.SetInt("textureMap", 0);

            _mainCharacter.Draw();
         }
         #endregion

         #region Enemies
         foreach (var enemy in _enemies)
         {
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(enemy.Position.X, enemy.Position.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrix);

            string enemyTexture = string.Empty;
            switch (enemy.Type)
            {
               case Enemy.EnemyType.Log:
                  enemyTexture = "enemy2";
                  break;
               case Enemy.EnemyType.Knight:
                  enemyTexture = "enemy1";
                  break;
               case Enemy.EnemyType.Ghost:
                  enemyTexture = "enemy3";
                  break;
               default:
                  break;
            }

            _textures[enemyTexture].Use(TextureUnit.Texture1);
            _textureShader.SetInt("textureMap", 1);

            enemy.Draw();
         }
         #endregion

         #region Projectiles
         foreach (var projectile in _projectiles)
         {
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(projectile.Position.X, projectile.Position.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrix);
            _textures["projectile1"].Use(TextureUnit.Texture2);
            _textureShader.SetInt("textureMap", 2);

            projectile.Draw();
         }
         #endregion

         #region Level timer
         {
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(_levelTimer.Position.X, _levelTimer.Position.Y, 0.0f));
            var scaleMatrix = Matrix4.CreateScale(new Vector3(_levelTimer.ViewScale(), 1.0f, 1.0f));

            _textureShader.SetMatrix4("model", scaleMatrix * modelMatrix);

            _textures["timebar"].Use(TextureUnit.Texture1);
            _textureShader.SetInt("textureMap", 1);
            _levelTimer.Draw();
         }
         #endregion


         foreach (var obstacle in _boundaryObstacles)
         {
            var modelMatrix = Matrix4.CreateTranslation(new Vector3(obstacle.Position.X, obstacle.Position.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrix);
            _textures["invisible_obstacle"].Use(TextureUnit.Texture2);
            _textureShader.SetInt("textureMap", 2);

            obstacle.Draw();
         }


         #region Bonuses
         {
            var modelMatrixHP = Matrix4.CreateTranslation(new Vector3(0.22f, 0.44f, 0.0f));
            var scaleMatrix = Matrix4.CreateScale(new Vector3(0.6f, 0.6f, 1.0f));
            _textureShader.SetMatrix4("model", scaleMatrix * modelMatrixHP);

            _textures["hitpoint"].Use(TextureUnit.Texture1);
            _textureShader.SetInt("textureMap", 1);
            _charInterface.DrawHitPoints();
         }
         #endregion

         #region Interface
         {
            var modelMatrixHP = Matrix4.CreateTranslation(new Vector3(_charInterface.PositionHitpoints.X, _charInterface.PositionHitpoints.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrixHP);

            _textures["hitpoint"].Use(TextureUnit.Texture1);
            _textureShader.SetInt("textureMap", 1);
            _charInterface.DrawHitPoints();


            var modelMatrixDigit = Matrix4.CreateTranslation(new Vector3(_charInterface.PositionDigit.X, _charInterface.PositionDigit.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrixDigit);

            _textures["digits"].Use(TextureUnit.Texture1);
            _textureShader.SetInt("textureMap", 1);
            _charInterface.DrawDigit(_mainCharacter.HitPoints);

            string screenType = string.Empty;
            switch (_gameState)
            {
               case GameState.Start:
                  screenType = "start";
                  break;
               case GameState.Run:
                  screenType = "invisible_obstacle";
                  break;
               case GameState.Pause:
                  screenType = "pause";
                  break;
               case GameState.GameOver:
                  screenType = "gameover";
                  break;
            }

            var modelMatrixScreen = Matrix4.CreateTranslation(new Vector3(_charInterface.PositionScreen.X, _charInterface.PositionScreen.Y, 0.0f));

            _textureShader.SetMatrix4("model", modelMatrixScreen);

            _textures[screenType].Use(TextureUnit.Texture1);
            _textureShader.SetInt("textureMap", 1);
            _charInterface.DrawScreen();
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

         if (e.Key is Keys.Enter)
         {
            if (_gameState is GameState.Start)
            {
               _gameState = GameState.Run;
            }
         }

         base.OnKeyDown(e);
      }



      private void SpawnProjectile(Vector2 position, Vector2 direction)
      {
         _projectiles.Add(new Projectile(position, direction, 1));

         if (false)
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
            new (-0.043f, -0.93f),
            new (0.02f, -0.93f),
            new (0.084f, -0.93f),

            // left
            new (-0.48f, -0.239f),
            new (-0.48f, -0.121f),
            new (-0.48f, -0.009f),

            // up
            new (-0.043f, 0.78f),
            new (0.02f, 0.78f),
            new (0.084f, 0.78f),

            // right
            new (0.468f, -0.239f),
            new (0.468f, -0.121f),
            new (0.468f, -0.009f),
         };

         for (int i = 0; i < spawnPositions.Count; i++)
         {
            Enemy enemy = new Enemy((Enemy.EnemyType)(i % 3));
            
            enemy.Position = spawnPositions[i];

            _enemies.Add(enemy);
         }
      }
   }
}