using JourneyOfThePrairieKing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;

namespace JourneyOfThePrairieKing
{
   public readonly struct FrameRate
   {
      public const int MinFPS = 30;
      public const int MaxFPS = 120;

      public const float MaxDeltaTime = 1.0f / MinFPS;
      public const float MinDeltaTime = 1.0f / MaxFPS;
   }

   public enum GameState
   {
      Start,
      Run,
      Pause,
      GameOver,
      Win,
   };

   public class Game : GameWindow
   {
      private int _currentLevel;
      private GameState _gameState;
      private Stopwatch _gameRunTime;
      private Dictionary<string, Texture> _textures;
      private Shader _textureShader;

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
                }
                )
      {
         CenterWindow();
      }

      protected override void OnResize(ResizeEventArgs e)
      {
         GL.Viewport(0, 0, Size.X, Size.Y);

         base.OnResize(e);
      }

      protected override void OnLoad()
      {
         //WindowState = WindowState.Fullscreen;
         IsVisible = true;
         GL.Enable(EnableCap.Blend);
         GL.BlendFunc((BlendingFactor)BlendingFactorSrc.SrcAlpha, (BlendingFactor)BlendingFactorDest.OneMinusSrcAlpha);
         GL.Enable(EnableCap.LineSmooth);
         GL.ClearColor(Color4.Black);

         _textureShader = new Shader("data/shaders/textureShader.vert", "data/shaders/textureShader.frag");

         _textures = new Dictionary<string, Texture>();
         string[] textureFiles = Directory.GetFiles("data/textures/");
         foreach (string filepath in textureFiles)
         {
            _textures.Add(Path.GetFileNameWithoutExtension(filepath), Texture.LoadFromFile(filepath));
            Console.WriteLine($"{Path.GetFileName(filepath)} loaded successful.");
         }

         var mapPosition = new Vector2(460, 20);
         var mapSize = new Vector2(1000, 1000);
         var winSize = new Vector2(ClientSize.X, ClientSize.Y);
         _levels = new List<Level>
         {
            new Level1(_textures, "level1", _textureShader, winSize, mapPosition, mapSize),
            new Shop(_textures, "level1", _textureShader, winSize, mapPosition, mapSize),
            new Level2(_textures, "level2",_textureShader, winSize, mapPosition, mapSize),
            new Shop(_textures,"level2", _textureShader, winSize, mapPosition, mapSize),
            new BossLevel(_textures,"level3", _textureShader, winSize, mapPosition, mapSize),
         };

         _currentLevel = 0;

         _gameRunTime = Stopwatch.StartNew();
         _gameRunTime.Stop();
         _gameState = GameState.Start;

         base.OnLoad();
      }

      protected override void OnUnload()
      {
         foreach (var level in _levels)
         {
            //level.Dispose();
         }
         base.OnUnload();
      }

      protected override void OnUpdateFrame(FrameEventArgs args)
      {
         if(_gameState is GameState.Win)
         {
            if (_currentLevel + 1 > _levels.Count)
            {
               if (IsKeyDown(Keys.Enter))
               {
                  _currentLevel = 0;
                  _levels[_currentLevel].Restart();
                  _gameState = GameState.Start;
               }
               return;
            }
            _currentLevel++;
            _levels[_currentLevel].SetCharacter(_levels[_currentLevel - 1].ExtractCharacter());
            _gameState = GameState.Run;
         }

         if (_gameState is not GameState.Run)
         {
            return;
         }

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
         Vector2 moveDir = new Vector2(moveX, moveY);


         float projX = 0.0f;
         float projY = 0.0f;

         if (IsKeyDown(Keys.Left))
         {
            projX = -1;
         }
         if (IsKeyDown(Keys.Right))
         {
            projX = 1;
         }
         if (IsKeyDown(Keys.Up))
         {
            projY = 1;
         }
         if (IsKeyDown(Keys.Down))
         {
            projY = -1;
         }

         Vector2 projDir = new Vector2(projX, projY);

         _levels[_currentLevel].Update(args, moveDir, projDir, ref _gameRunTime, ref _gameState);

         base.OnUpdateFrame(args);
      }

      protected override void OnRenderFrame(FrameEventArgs args)
      {
         GL.Clear(ClearBufferMask.ColorBufferBit);
         _textureShader.Use();
         _levels[_currentLevel].Render(args, _gameState);

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
               WindowState = IsFullscreen ? WindowState.Maximized : WindowState.Fullscreen;
               break;
            }

            case Keys.Escape:
            {
               Close();
               break;
            }
         }

         #endregion

         if (e.Key is Keys.Enter)
         {
            switch (_gameState)
            {
               case GameState.Start:
                  _gameState = GameState.Run;
                  _gameRunTime.Start();
                  break;

               case GameState.Run:
                  _gameState = GameState.Pause;
                  _gameRunTime.Stop();
                  break;

               case GameState.Pause:
                  _gameState = GameState.Run;
                  _gameRunTime.Start();
                  break;

               case GameState.GameOver:
                  _levels[_currentLevel].Restart();
                  _gameState = GameState.Run;
                  break;

               //case GameState.Win:
               //   if (_currentLevel + 1 > _levels.Count)
               //   {
               //      Close(); // temp
               //      return;
               //   }
               //   _currentLevel++;
               //   _levels[_currentLevel].SetCharacter(_levels[_currentLevel - 1].ExtractCharacter());
               //   _gameState = GameState.Run;
               //   break;

               default:
                  break;
            }
         }

         base.OnKeyDown(e);
      }
   }
}