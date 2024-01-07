using JourneyOfThePrairieKing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Threading;

namespace JourneyOfThePrairieKing
{
   public static class Coordinates
   {
      public static Vector2 PosInNDC(Vector2 posInPixels, Vector2 screenSize)
      {
         return new Vector2(posInPixels.X / screenSize.X * 2.0f - 1.0f, posInPixels.Y / screenSize.Y * 2.0f - 1.0f);
      }

      public static Vector2 SizeInNDC(Vector2 sizeInPixels, Vector2 screenSize)
      {
         return new Vector2(sizeInPixels.X / screenSize.X * 2.0f, sizeInPixels.Y / screenSize.Y * 2.0f);
      }
   }

   public class Level
   {
      #region Privates

      protected Vector2 _winSize;
      protected Vector2 _winSizeToSquare;

      protected Vector2 _tileSize;

      protected bool _enemySpawnAllowed;
      protected long _enemyLastSpawnTime;
      protected Bonus? _activeBonus;

      protected Timer _levelTimer;

      protected Dictionary<string, Texture> _textures;
      protected Shader _textureShader;

      protected Character _character;
      protected Dictionary<string, LevelInterface> _Interfaces;

      protected HashSet<Obstacle> _boundaryObstacles;
      protected HashSet<Obstacle> _exitColiders;

      protected HashSet<Enemy> _enemies;
      protected HashSet<Projectile> _projectiles;
      protected HashSet<Bonus> _bonuses;
      protected HashSet<Obstacle> _obstacles;

      #endregion

      #region Map parameters

      protected string _mapTextureName;
      protected VertexPositionTexture[] _MapVertices;
      protected VertexBufferObject _MapVbo;
      protected VertexArrayObject _MapVao;

      protected Vector2 MapSize { get; init; }
      protected Vector2 MapPosition { get; init; }

      #endregion


      public Level
         (
         Dictionary<string, Texture> textures,
         string mapTextureName,
         Shader textureShader,
         Vector2 winSize,
         Vector2 mapPosition,
         Vector2 mapSize,
         Character? character = null
         )
      {
         _mapTextureName = mapTextureName;
         _winSize = winSize;
         (_winSizeToSquare.X, _winSizeToSquare.Y) = (winSize.Y, winSize.X);
         _winSizeToSquare.Normalize();
         MapPosition = Coordinates.PosInNDC(mapPosition, winSize);
         MapSize = Coordinates.SizeInNDC(mapSize, winSize);

         //Console.WriteLine($"{MapSize.X} {MapSize.Y}");
         //Console.WriteLine($"{MapPosition.X} {MapPosition.Y}");

         _MapVertices = new VertexPositionTexture[]
         {
            new (new Vector2(0, 0), new Vector2(0.0f, 0.0f)),
            new (new Vector2(MapSize.X, 0), new Vector2(1.0f, 0.0f)),
            new (new Vector2(0, MapSize.Y), new Vector2(0.0f, 1.0f)),
            new (new Vector2(MapSize.X, MapSize.Y), new Vector2(1.0f, 1.0f))
         };
         _MapVbo = new VertexBufferObject(_MapVertices, BufferUsageHint.StaticDraw);
         _MapVbo.Bind();
         _MapVao = new VertexArrayObject();
         _MapVao.Bind();


         _enemySpawnAllowed = true;
         _enemyLastSpawnTime = 0;

         _textures = textures;
         _textureShader = textureShader;

         _enemies = new HashSet<Enemy>();
         _projectiles = new HashSet<Projectile>();
         _bonuses = new HashSet<Bonus>();

         _tileSize = MapSize / 16.0f;
         _obstacles = new HashSet<Obstacle>();
         _boundaryObstacles = new HashSet<Obstacle>();
         _exitColiders = new HashSet<Obstacle>();
         // bottom
         for (int i = 0; i < 7; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * _tileSize.X, 0), _tileSize));
         for (int i = 7; i < 10; i++)
         {
            var obstacle = new Obstacle(MapPosition + new Vector2(i * _tileSize.X, -_tileSize.Y), _tileSize);
            _boundaryObstacles.Add(obstacle);
            _exitColiders.Add(obstacle);
         }
         for (int i = 10; i < 16; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * _tileSize.X, 0), _tileSize));

         // up
         for (int i = 0; i < 7; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * _tileSize.X, MapSize.Y - _tileSize.Y), _tileSize));
         for (int i = 7; i < 10; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * _tileSize.X, MapSize.Y), _tileSize));
         for (int i = 10; i < 16; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * _tileSize.X, MapSize.Y - _tileSize.Y), _tileSize));

         // left
         for (int i = 1; i < 6; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(0, i * _tileSize.Y), _tileSize));
         for (int i = 6; i < 9; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(-_tileSize.X, i * _tileSize.Y), _tileSize));
         for (int i = 9; i < 15; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(0, i * _tileSize.Y), _tileSize));

         // right
         for (int i = 1; i < 6; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(MapSize.X - _tileSize.X, i * _tileSize.Y), _tileSize));
         for (int i = 6; i < 9; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(MapSize.X, i * _tileSize.Y), _tileSize));
         for (int i = 9; i < 15; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(MapSize.X - _tileSize.X, i * _tileSize.Y), _tileSize));


         Vector2 characterSize = Coordinates.SizeInNDC(new Vector2(62, 62), _winSize);
         _character = character ?? new Character(characterSize, -characterSize / 2);

         _Interfaces = new Dictionary<string, LevelInterface>();
         var digitSize = Coordinates.SizeInNDC(new Vector2(12, 24), _winSize);

         var HitPointsIcon = new LevelInterface(Coordinates.SizeInNDC(new Vector2(24, 48), _winSize), new Vector2(MapPosition.X - MapSize.X / 10, MapPosition.Y + MapSize.Y * 0.8f), _textures["hitpoint"]);
         var HitPointsCounter = new Counter(digitSize, new Vector2(HitPointsIcon.Position.X + HitPointsIcon.Size.X + digitSize.X, HitPointsIcon.Position.Y + HitPointsIcon.Size.Y / 2 - digitSize.Y / 2), _textures["digits"], 10);

         var CoinIcon = new LevelInterface(Coordinates.SizeInNDC(new Vector2(24, 24), _winSize), new Vector2(MapPosition.X - MapSize.X / 10, MapPosition.Y + MapSize.Y * 0.75f), _textures["coin"]);
         var CoinCounter = new Counter(digitSize, new Vector2(CoinIcon.Position.X + CoinIcon.Size.X + digitSize.X, CoinIcon.Position.Y + CoinIcon.Size.Y / 2 - digitSize.Y / 2), _textures["digits"], 10);

         var gameStart = new LevelInterface(MapSize, MapPosition, _textures["start"]);
         var gameWin = new LevelInterface(MapSize, MapPosition, _textures["win"]);
         var gameOver = new LevelInterface(MapSize, MapPosition, _textures["gameover"]);
         var gamePause = new LevelInterface(MapSize, MapPosition, _textures["pause"]);

         var arrow = new LevelInterface(_tileSize, MapPosition + new Vector2(8 * _tileSize.X, 0), _textures["arrow"]);

         var bonusholder = new LevelInterface(Coordinates.SizeInNDC(new Vector2(80, 80), _winSize), new Vector2(MapPosition.X - MapSize.X / 10, MapPosition.Y + MapSize.Y * 0.88f), _textures["bonusholder"]);
         var wheel = new LevelInterface(Coordinates.SizeInNDC(new Vector2(64, 64), _winSize), new Vector2(MapPosition.X - MapSize.X / 10 + 0.009f, MapPosition.Y + MapSize.Y * 0.887f), _textures["wheel"]);
         var shotgun = new LevelInterface(Coordinates.SizeInNDC(new Vector2(90, 30), _winSize), new Vector2(MapPosition.X - MapSize.X / 10 - 0.01f, MapPosition.Y + MapSize.Y * 0.90f), _textures["shotgun"]);

         _Interfaces.Add("HitPointsIcon", HitPointsIcon);
         _Interfaces.Add("HitPointsCounter", HitPointsCounter);
         _Interfaces.Add("CoinIcon", CoinIcon);
         _Interfaces.Add("CoinCounter", CoinCounter);

         _Interfaces.Add("gameStart", gameStart);
         _Interfaces.Add("gameWin", gameWin);
         _Interfaces.Add("gameOver", gameOver);
         _Interfaces.Add("gamePause", gamePause);

         _Interfaces.Add("arrow", arrow);

         _Interfaces.Add("bonusholder", bonusholder);
         _Interfaces.Add("wheel", wheel);
         _Interfaces.Add("shotgun", shotgun);

         _activeBonus = null;
      }

      public virtual void Update(FrameEventArgs args, Vector2 moveDir, Vector2 projectileDir, ref Stopwatch gameRunTime, ref GameState gameState)
      {
         long MillisecLastUpdate = (long)(args.Time * 1000);

         float deltaTime = (float)args.Time;

         //Console.WriteLine(deltaTime);
         deltaTime = (float)Math.Min(deltaTime, FrameRate.MaxDeltaTime);
         deltaTime = (float)Math.Max(deltaTime, FrameRate.MinDeltaTime);

         #region Movement

         if (moveDir.X != 0.0f || moveDir.Y != 0.0f)
         {
            moveDir.Normalize();
            _character.ChangePosition(_character.Position + _character.Velocity * _winSizeToSquare * moveDir * deltaTime);

            if (_enemies.Count == 0 && _enemySpawnAllowed is false)
            {
               foreach (var exitColider in _exitColiders)
               {
                  if (Entity.CheckCollision(exitColider, _character) is true)
                  {
                     //Console.WriteLine("Exit from level");
                     gameState = GameState.Win;
                     return;
                  }
               }
            }

            foreach (var obstacle in _boundaryObstacles.Union(_obstacles))
            {
               if (Entity.CheckCollision(_character, obstacle) is true)
               {
                  var vecToObstacle = obstacle.Position - _character.Position;
                  vecToObstacle.Normalize();

                  _character.ChangePosition(_character.Position - 1.0f * _character.Velocity * _winSizeToSquare * vecToObstacle * deltaTime);
                  Console.WriteLine("Collision with obstacle!");
               }
            }
         }

         var previousPosiiton = _character.Position;

         // character taking bonuses
         foreach (var bonus in _bonuses)
         {
            if (bonus.IsTTLEnded(MillisecLastUpdate) is true)
            {
               bonus.Dispose();
               _bonuses.Remove(bonus);
            }

            if (Entity.CheckCollision(_character, bonus) is true)
            {
               if (bonus.Duration != 0)
                  _activeBonus = bonus;

               switch (bonus.Type)
               {
                  case Bonus.BonusType.Wheel:
                     bonus.Dispose();
                     _bonuses.Remove(bonus);
                     break;

                  case Bonus.BonusType.ShotGun:
                     bonus.Dispose();
                     _bonuses.Remove(bonus);
                     break;

                  case Bonus.BonusType.Nuke:
                     _enemies.Clear();
                     bonus.Dispose();
                     _bonuses.Remove(bonus);
                     break;

                  case Bonus.BonusType.HitPoint:
                     if (_character.HitPoints < 9)
                     {
                        _character.HitPoints++;
                        //bonus.Dispose();
                        _bonuses.Remove(bonus);
                     }
                     break;

                  case Bonus.BonusType.Coin:
                     if (_character.CoinsCount < 99)
                     {
                        _character.GotCoin(1);
                        bonus.Dispose();
                        _bonuses.Remove(bonus);
                     }
                     break;
               }


               //Console.WriteLine("Collision with bonus!");
            }
         }

         // character getting damage when collision with enemy
         foreach (var enemy in _enemies)
         {
            if (Entity.CheckCollision(_character, enemy) is true)
            {
               _character.GotDamage(1, gameRunTime.ElapsedMilliseconds);
               if (_character.HitPoints < 0)
               {
                  gameState = GameState.GameOver;
               }
               //Console.WriteLine("Collision with enemy!");
            }
         }

         //enemies makes collisions
         foreach (var enemy1 in _enemies)
         {
            var prevPos = enemy1.Position;
            var distanceToChar = _character.Position - enemy1.Position;
            distanceToChar.Normalize();
            enemy1.ChangePosition(enemy1.Position + enemy1.Velocity * _winSizeToSquare * distanceToChar * deltaTime);

            foreach (var enemy2 in _enemies.Where(_ => _ != enemy1))
            {
               if (Entity.CheckCollision(enemy1, enemy2) is true)
               {
                  var vecToEnemy2 = enemy2.Position - enemy1.Position;
                  vecToEnemy2.Normalize();
                  enemy1.ChangePosition(enemy1.Position - 2.0f * enemy1.Velocity * _winSizeToSquare * vecToEnemy2 * deltaTime);
               }
            }


            foreach (var obstacle in _boundaryObstacles.Union(_obstacles))
            {
               if (Entity.CheckCollision(enemy1, obstacle) is true)
               {
                  if (enemy1.Type == Enemy.EnemyType.Ghost)
                  {
                     continue;
                  }
                  var vecToObstacle = obstacle.Position - enemy1.Position;
                  vecToObstacle.Normalize();
                  enemy1.ChangePosition(enemy1.Position - 2.0f * enemy1.Velocity * _winSizeToSquare * vecToObstacle * deltaTime);
               }
            }
         }

         #endregion


         #region Shoot

         //Console.WriteLine(_timer.ElapsedMilliseconds + " " + _lastShotTime);

         // character shooting
         if (gameRunTime.ElapsedMilliseconds - _character.LastShotTime > _character.ReloadTime)
         {
            if (projectileDir.X != 0.0f || projectileDir.Y != 0.0f)
            {
               projectileDir.Normalize();
               SpawnProjectile(_character.Position + _character.Size / 2, projectileDir);
               _character.LastShotTime = gameRunTime.ElapsedMilliseconds;
            }
         }

         // process projectiles 
         foreach (var projectile in _projectiles)
         {
            bool projectileAlive = true;
            projectile.ChangePosition(projectile.Position + projectile.Velocity * _winSizeToSquare * projectile.Direction * deltaTime);

            // remove projectile if it hits obstacle
            foreach (var obstacle in _boundaryObstacles.Union(_obstacles))
            {
               if (Entity.CheckCollision(projectile, obstacle) is true)
               {
                  _projectiles.Remove(projectile);
                  projectileAlive = false;
                  break;
               }
            }

            if (projectileAlive is false)
            {
               break;
            }

            // if projectile hits enemy
            foreach (var enemy in _enemies)
            {
               if (Entity.CheckCollision(projectile, enemy) is true)
               {
                  enemy.GotDamage(projectile.Damage);
                  if (enemy.HitPoints <= 0)
                  {
                     SpawnBonus(enemy.Position + enemy.Size / 2.0f);
                     _enemies.Remove(enemy);
                  }
                  _projectiles.Remove(projectile);
                  //Console.WriteLine("Enemy hit!");
                  break;
               }
            }
         }

         #endregion
      }

      public virtual void Render(FrameEventArgs args, GameState gameState)
      {
         Texture.DrawTexturedRectangle(_textureShader, _textures[_mapTextureName], MapPosition, _MapVao);
         _levelTimer.Render(_textureShader, _textures["timebar"]);

         if (_enemies.Count == 0 && _enemySpawnAllowed is false)
         {
            _Interfaces["arrow"].Render(_textureShader);
         }
         foreach (var obstacle in _obstacles)
         {
            obstacle.Render(_textureShader, _textures["obstacle"]);
         }

         foreach (var projectile in _projectiles)
         {
            Texture texture;
            switch (_character.Ammo)
            {
               case Character.ProjectileLevel.Stone:
                  texture = _textures["projectile1"];
                  break;

               case Character.ProjectileLevel.Metal:
                  texture = _textures["projectile2"];
                  break;

               case Character.ProjectileLevel.Burning:
                  texture = _textures["projectile3"];
                  break;

               default:
                  texture = _textures["projectile1"];
                  break;
            }

            projectile.Render(_textureShader, texture);
         }

         _character.Render(_textureShader, _textures["char1"]);

         foreach (var enemy in _enemies)
         {
            Texture texture;
            switch (enemy.Type)
            {
               case Enemy.EnemyType.Log:
                  texture = _textures["enemy2"];
                  break;

               case Enemy.EnemyType.Ghost:
                  texture = _textures["enemy3"];
                  break;

               case Enemy.EnemyType.Knight:
                  texture = _textures["enemy1"];
                  break;

               default:
                  texture = _textures["enemy1"];
                  break;
            }
            enemy.Render(_textureShader, texture);
         }


         // Bonuses
         {
            Texture texture;
            foreach (var bonus in _bonuses)
            {
               // switch bonustype
               switch (bonus.Type)
               {
                  case Bonus.BonusType.Wheel:
                     texture = _textures["wheel"];
                     break;

                  case Bonus.BonusType.ShotGun:
                     texture = _textures["shotgun"];
                     break;

                  case Bonus.BonusType.Nuke:
                     texture = _textures["nuke"];
                     break;

                  case Bonus.BonusType.HitPoint:
                     texture = _textures["hitpoint"];
                     break;

                  case Bonus.BonusType.Coin:
                     texture = _textures["coin"];
                     break;

                  default:
                     texture = _textures["coin"];
                     break;
               }

               
               bonus.Render(_textureShader, texture);
            }
         }



         // Interface objects
         _Interfaces["HitPointsIcon"].Render(_textureShader);
         _Interfaces["HitPointsCounter"].Render(_textureShader, _character.HitPoints);
         _Interfaces["CoinIcon"].Render(_textureShader);
         _Interfaces["CoinCounter"].Render(_textureShader, _character.CoinsCount);
         _Interfaces["bonusholder"].Render(_textureShader);

         switch (_activeBonus?.Type)
         {
            case Bonus.BonusType.Wheel:
               _Interfaces["wheel"].Render(_textureShader);
               break;

            case Bonus.BonusType.ShotGun:
               _Interfaces["shotgun"].Render(_textureShader);
               break;
         }

         switch (gameState)
         {
            case GameState.Start:
               _Interfaces["gameStart"].Render(_textureShader);
               break;

            case GameState.Pause:
               _Interfaces["gamePause"].Render(_textureShader);
               break;

            case GameState.GameOver:
               _Interfaces["gameOver"].Render(_textureShader);
               break;

            case GameState.Win:
               //_Interfaces["gameWin"].Render(_textureShader);
               break;
         }
      }


      protected virtual void SpawnProjectile(Vector2 position, Vector2 direction)
      {
         Vector2 projectileSize = Coordinates.SizeInNDC(new Vector2(16, 16), _winSize);

         if (_activeBonus?.Type == Bonus.BonusType.Wheel)
         {
            for (int i = -1; i < 2; i++)
            {
               for (int j = -1; j < 2; j++)
               {
                  direction = new Vector2(i, j);
                  direction.Normalize();
                  _projectiles.Add(new Projectile(projectileSize, position, direction, 1));
               }
            }
         }
         else if (_activeBonus?.Type == Bonus.BonusType.ShotGun)
         {
            Vector2 rotatedDirection;
            _projectiles.Add(new Projectile(projectileSize, position, direction, 1));
            rotatedDirection = Matrix2.CreateRotation((float)Math.PI / 9) * direction;
            _projectiles.Add(new Projectile(projectileSize, position, rotatedDirection, 1));
            rotatedDirection = Matrix2.CreateRotation(-(float)Math.PI / 9) * direction;
            _projectiles.Add(new Projectile(projectileSize, position, rotatedDirection, 1));
         }
         else
         {
            _projectiles.Add(new Projectile(projectileSize, position, direction, 1));
         }
      }

      protected virtual void SpawnEnemy()
      {
         Vector2 enemySize = Coordinates.SizeInNDC(new Vector2(60, 60), _winSize);
         Vector2 tileSize = Coordinates.SizeInNDC(new Vector2(1000 / 16.0f, 1000 / 16.0f), _winSize);


         var prob0 = 0.5f;
         var prob1 = 0.35f;
         var prob2 = 0.15f;

         var spawnPositions = new List<Vector2>()
         {
            // bottom
            MapPosition + new Vector2(7 * tileSize.X, 0),
            MapPosition + new Vector2(8 * tileSize.X, 0),
            MapPosition + new Vector2(9 * tileSize.X, 0),

            // left
            MapPosition + new Vector2(0, 6 * tileSize.Y),
            MapPosition + new Vector2(0, 7 * tileSize.Y),
            MapPosition + new Vector2(0, 8 * tileSize.Y),

            // up
            MapPosition + new Vector2(7 * tileSize.X, MapSize.Y - tileSize.Y),
            MapPosition + new Vector2(8 * tileSize.X, MapSize.Y - tileSize.Y),
            MapPosition + new Vector2(9 * tileSize.X, MapSize.Y - tileSize.Y),

            // right
            MapPosition + new Vector2(MapSize.X - tileSize.X, 6 * tileSize.Y),
            MapPosition + new Vector2(MapSize.X - tileSize.X, 7 * tileSize.Y),
            MapPosition + new Vector2(MapSize.X - tileSize.X, 8 * tileSize.Y),
         };

         var rand = new Random();
         int enemiesCount = rand.Next(1, 6);
         for (int i = 0; i < enemiesCount; i++)
         {
            int typeI = 0;
            double probType = rand.NextDouble();
            if (probType < prob2) // knight
               typeI = 2;
            else if (probType < prob2 + prob1) // log
               typeI = 1;
            else if (probType < prob2 + prob1 + prob0) // ghost
               typeI = 0;

            int posI = rand.Next(spawnPositions.Count);
            Enemy enemy = new Enemy(enemySize, spawnPositions[posI], (Enemy.EnemyType)(typeI));

            _enemies.Add(enemy);
         }
      }

      protected virtual void SpawnBonus(Vector2 position)
      {
         const long bonusTTL = 10_000;
         var rand = new Random();
         var prob = 0.9f;
         if (rand.NextDouble() > prob)
         {
            return;
         }

         var prob0 = 0.5f; // coin
         var prob1 = 0.3f; // shotgun
         var prob2 = 0.15f; // wheel
         var prob3 = 0.1f; // life
         var prob4 = 0.05f; // bomb

         
         Bonus.BonusType bonusType = Bonus.BonusType.Coin;
         long Duration = 0;
         double probType = rand.NextDouble();
         var sizeVec = Coordinates.SizeInNDC(new Vector2(24, 24), _winSize);
         if (probType < prob4) // nuke
         {
            bonusType = Bonus.BonusType.Nuke;
            sizeVec = Coordinates.SizeInNDC(new Vector2(32, 32), _winSize);
         }

         else if (probType < prob4 + prob3) // life
         {
            bonusType = Bonus.BonusType.HitPoint;
            sizeVec = Coordinates.SizeInNDC(new Vector2(24, 48), _winSize);
         }
         else if (probType < prob4 + prob3 + prob2) // wheel
         {
            bonusType = Bonus.BonusType.Wheel;
            Duration = 5000;
            sizeVec = Coordinates.SizeInNDC(new Vector2(32, 32), _winSize);
         }

         else if (probType < prob4 + prob3 + prob2 + prob1) // shotgun
         {
            bonusType = Bonus.BonusType.ShotGun;
            Duration = 8000;
            sizeVec = Coordinates.SizeInNDC(new Vector2(72, 24), _winSize);

         }
         else if (probType < prob4 + prob3 + prob2 + prob1 + prob0) // coin
         {
            bonusType = Bonus.BonusType.Coin;
            sizeVec = Coordinates.SizeInNDC(new Vector2(24, 24), _winSize);
         }

         Bonus bonus = new Bonus(position, sizeVec, bonusType, Duration, bonusTTL);
         _bonuses.Add(bonus);
      }

      public virtual void Restart()
      {
         _enemySpawnAllowed = true;
         _enemyLastSpawnTime = 0;
         _enemies.Clear();
         _bonuses.Clear();
         _projectiles.Clear();
         _character.Reset();
         _levelTimer.Reset();
         _activeBonus = null;
      }

      public virtual Character ExtractCharacter()
         => _character;

      public virtual void SetCharacter(Character character)
      {
         _character = character;
         OnCharacterSet(character);
      }

      public virtual void OnCharacterSet(Character character)
      {
         _character.ChangePosition(MapPosition + MapSize / 2);
      }
   }

   public sealed class Level1 : Level
   {
      #region Constants

      private const long _enemySpawnCooldown = 3_000;
      private const long _levelPlayTime = 120_00;
      private const int _maxEnemiesAtTime = 15;

      #endregion


      #region Privates


      #endregion

      public Level1
         (
         Dictionary<string, Texture> textures,
         string mapTextureName,
         Shader textureShader,
         Vector2 winSize, 
         Vector2 mapPosition,
         Vector2 mapSize,
         Character? character = null
         ) : base(textures, mapTextureName, textureShader, winSize, mapPosition, mapSize, character)
      {

         _levelTimer = new Timer(_levelPlayTime, Coordinates.PosInNDC(new Vector2(460, 1040), _winSize), new Vector2(MapSize.X, MapSize.Y / 100));

         _obstacles.Add(new Obstacle(MapPosition + new Vector2(3 * _tileSize.X, 3 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(3 * _tileSize.X, 4 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(4 * _tileSize.X, 3 * _tileSize.Y), _tileSize));

         _obstacles.Add(new Obstacle(MapPosition + new Vector2(9 * _tileSize.X, 9 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(9 * _tileSize.X, 10 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(10 * _tileSize.X, 9 * _tileSize.Y), _tileSize));

         _obstacles.Add(new Obstacle(MapPosition + new Vector2(3 * _tileSize.X, 9 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(3 * _tileSize.X, 10 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(4 * _tileSize.X, 9 * _tileSize.Y), _tileSize));

         _obstacles.Add(new Obstacle(MapPosition + new Vector2(9 * _tileSize.X, 3 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(9 * _tileSize.X, 4 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(10 * _tileSize.X, 3 * _tileSize.Y), _tileSize));
      }

      public override void Update(FrameEventArgs args, Vector2 moveDir, Vector2 projectileDir, ref Stopwatch gameRunTime, ref GameState gameState)
      {
         long MillisecLastUpdate = (long)(args.Time * 1000);

         _levelTimer.Update(MillisecLastUpdate);
         if (_activeBonus?.IsDurationEnded(MillisecLastUpdate) is true)
         {
            _activeBonus = null;
         }

         if (_levelTimer.isTimeEnds is true)
         {
            if (_enemySpawnAllowed is true)
            {
               _enemySpawnAllowed = false;
            }
         }

         if (gameRunTime.ElapsedMilliseconds - _enemyLastSpawnTime > _enemySpawnCooldown
            && _enemies.Count < _maxEnemiesAtTime
            && _enemySpawnAllowed is true
            )
         {
            _enemyLastSpawnTime = gameRunTime.ElapsedMilliseconds;
            SpawnEnemy();
         }

         base.Update(args, moveDir, projectileDir, ref gameRunTime, ref gameState);
      }
   }

   public sealed class Shop1 : Level
   {
      public Shop1
         (
         Dictionary<string, Texture> textures,
         string mapTextureName,
         Shader textureShader,
         Vector2 winSize,
         Vector2 mapPosition,
         Vector2 mapSize,
         Character? character = null
         ) : base(textures, mapTextureName, textureShader, winSize, mapPosition, mapSize, character)
      {
         _levelTimer = new Timer(1, Coordinates.PosInNDC(new Vector2(460, 1040), _winSize), new Vector2(MapSize.X, MapSize.Y / 100));
      }

      public override void Update(FrameEventArgs args, Vector2 moveDir, Vector2 projectileDir, ref Stopwatch gameRunTime, ref GameState gameState)
      {
         _character.SetAmmo(Character.ProjectileLevel.Metal);
         _character.SetBoots(Character.BootsLevel.Medium);
         _character.SetGun(Character.GunLevel.Medium);

         long MillisecLastUpdate = (long)(args.Time * 1000);

         float deltaTime = (float)args.Time;

         //Console.WriteLine(deltaTime);
         deltaTime = (float)Math.Min(deltaTime, FrameRate.MaxDeltaTime);
         deltaTime = (float)Math.Max(deltaTime, FrameRate.MinDeltaTime);

         #region Movement

         if (moveDir.X != 0.0f || moveDir.Y != 0.0f)
         {
            moveDir.Normalize();
            _character.ChangePosition(_character.Position + _character.Velocity * _winSizeToSquare * moveDir * deltaTime);

            foreach (var obstacle in _boundaryObstacles.Union(_obstacles))
            {
               if (Entity.CheckCollision(_character, obstacle) is true)
               {
                  var vecToObstacle = obstacle.Position - _character.Position;
                  vecToObstacle.Normalize();

                  _character.ChangePosition(_character.Position - 1.0f * _character.Velocity * _winSizeToSquare * vecToObstacle * deltaTime);
                  //Console.WriteLine("Collision with obstacle!");
               }
            }
         }

         // character taking bonuses
         foreach (var bonus in _bonuses)
         {
            if (bonus.IsTTLEnded(MillisecLastUpdate) is true)
            {
               bonus.Dispose();
               _bonuses.Remove(bonus);
            }

            if (Entity.CheckCollision(_character, bonus) is true)
            {
               if (bonus.Duration != 0)
                  _activeBonus = bonus;

               switch (bonus.Type)
               {
                  case Bonus.BonusType.Wheel:
                     bonus.Dispose();
                     _bonuses.Remove(bonus);
                     break;

                  case Bonus.BonusType.ShotGun:
                     bonus.Dispose();
                     _bonuses.Remove(bonus);
                     break;

                  case Bonus.BonusType.Nuke:
                     _enemies.Clear();
                     bonus.Dispose();
                     _bonuses.Remove(bonus);
                     break;

                  case Bonus.BonusType.HitPoint:
                     if (_character.HitPoints < 9)
                     {
                        _character.HitPoints++;
                        //bonus.Dispose();
                        _bonuses.Remove(bonus);
                     }
                     break;

                  case Bonus.BonusType.Coin:
                     if (_character.CoinsCount < 99)
                     {
                        _character.GotCoin(1);
                        bonus.Dispose();
                        _bonuses.Remove(bonus);
                     }
                     break;
               }


               //Console.WriteLine("Collision with bonus!");
            }
         }

         #endregion

         #region Shoot

         //Console.WriteLine(_timer.ElapsedMilliseconds + " " + _lastShotTime);

         // character shooting
         if (gameRunTime.ElapsedMilliseconds - _character.LastShotTime > _character.ReloadTime)
         {
            if (projectileDir.X != 0.0f || projectileDir.Y != 0.0f)
            {
               projectileDir.Normalize();
               SpawnProjectile(_character.Position + _character.Size / 2, projectileDir);
               _character.LastShotTime = gameRunTime.ElapsedMilliseconds;
            }
         }

         // process projectiles 
         foreach (var projectile in _projectiles)
         {
            projectile.ChangePosition(projectile.Position + projectile.Velocity * _winSizeToSquare * projectile.Direction * deltaTime);

            // remove projectile if it hits obstacle
            foreach (var obstacle in _boundaryObstacles.Union(_obstacles))
            {
               if (Entity.CheckCollision(projectile, obstacle) is true)
               {
                  _projectiles.Remove(projectile);
                  break;
               }
            }
         }

         #endregion
      }

      public override void Render(FrameEventArgs args, GameState gameState)
      {
         base.Render(args, gameState);


      }
   }

   public sealed class Level2 : Level
   {
      #region Constants

      private const long _enemySpawnCooldown = 2_000;
      private const long _levelPlayTime = 120_00;
      private const int _maxEnemiesAtTime = 20;

      #endregion


      #region Privates


      #endregion

      public Level2
         (
         Dictionary<string, Texture> textures,
         string mapTextureName,
         Shader textureShader,
         Vector2 winSize,
         Vector2 mapPosition,
         Vector2 mapSize,
         Character? character = null
         ) : base(textures, mapTextureName, textureShader, winSize, mapPosition, mapSize, character)
      {

         _levelTimer = new Timer(_levelPlayTime, Coordinates.PosInNDC(new Vector2(460, 1040), _winSize), new Vector2(MapSize.X, MapSize.Y / 100));

         _obstacles.Add(new Obstacle(MapPosition + new Vector2(3 * _tileSize.X, 3 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(3 * _tileSize.X, 4 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(4 * _tileSize.X, 3 * _tileSize.Y), _tileSize));


         _obstacles.Add(new Obstacle(MapPosition + new Vector2(9 * _tileSize.X, 3 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(9 * _tileSize.X, 4 * _tileSize.Y), _tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(10 * _tileSize.X, 3 * _tileSize.Y), _tileSize));
      }

      public override void Update(FrameEventArgs args, Vector2 moveDir, Vector2 projectileDir, ref Stopwatch gameRunTime, ref GameState gameState)
      {
         long MillisecLastUpdate = (long)(args.Time * 1000);

         _levelTimer.Update(MillisecLastUpdate);
         if (_activeBonus?.IsDurationEnded(MillisecLastUpdate) is true)
         {
            _activeBonus = null;
         }

         if (_levelTimer.isTimeEnds is true)
         {
            if (_enemySpawnAllowed is true)
            {
               _enemySpawnAllowed = false;
            }
         }

         if (gameRunTime.ElapsedMilliseconds - _enemyLastSpawnTime > _enemySpawnCooldown
            && _enemies.Count < _maxEnemiesAtTime
            && _enemySpawnAllowed is true
            )
         {
            _enemyLastSpawnTime = gameRunTime.ElapsedMilliseconds;
            SpawnEnemy();
         }

         base.Update(args, moveDir, projectileDir, ref gameRunTime, ref gameState);

      }
   }

   public sealed class BossLevel : Level
   {
      private Boss _boss;
      private HashSet<Projectile> _bossProjectiles;


      public BossLevel
         (
         Dictionary<string, Texture> textures,
         string mapTextureName,
         Shader textureShader,
         Vector2 winSize,
         Vector2 mapPosition,
         Vector2 mapSize,
         Character? character = null
         ) : base(textures, mapTextureName, textureShader, winSize, mapPosition, mapSize, character)
      {
         _boss = new Boss(new Vector2(MapSize.X / 8.0f, MapSize.Y / 8.0f), Coordinates.PosInNDC(new Vector2(_winSize.X / 2 - 2 * _tileSize.X, 200), _winSize));
         _levelTimer = new Timer(_boss.HitPoints, Coordinates.PosInNDC(new Vector2(460, 1040), _winSize), new Vector2(MapSize.X, MapSize.Y / 100));
         _bossProjectiles = new HashSet<Projectile>();
      }

      public override void OnCharacterSet(Character character)
      {

         character.ChangePosition(Coordinates.PosInNDC(new Vector2(_winSize.X / 2 - 31, 800), _winSize));
      }

      public override void Update(FrameEventArgs args, Vector2 moveDir, Vector2 projectileDir, ref Stopwatch gameRunTime, ref GameState gameState)
      {
         base.Update(args, moveDir, projectileDir, ref gameRunTime, ref gameState);

         if (_levelTimer.isTimeEnds)
         {
            _boss.IsDead = true;
         }

         if (_boss.IsDead is false)
         {
            foreach (var projectile in _projectiles)
            {
               if (Entity.CheckCollision(_boss, projectile))
               {
                  projectile.Dispose();
                  _projectiles.Remove(projectile);
                  _levelTimer.Update(projectile.Damage);
               }
            }
         }
      }


      public override void Render(FrameEventArgs args, GameState gameState)
      {
         base.Render(args, gameState);

         _levelTimer.Render(_textureShader, _textures["bossbar"]);

         if (_boss.IsDead is false)
         {
            _boss.Render(_textureShader, _textures["boss"]);
         }

         foreach (var projectile in _bossProjectiles)
         {
            projectile.Render(_textureShader, _textures["fireball"]);
         }
      }


      private void SpawnBossProjectiles()
      {

      }
   }
}