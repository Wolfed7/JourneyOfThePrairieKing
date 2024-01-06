using CG_PR3;
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
      #region Constants

      private const long _enemySpawnCooldown = 2000;
      private const long _levelPlayTime = 120000;
      private const int _maxEnemiesAtTime = 20;

      #endregion


      #region Privates

      private Vector2 _winSize;
      private Vector2 _winSizeToSquare;

      private bool _enemySpawnAllowed;
      private long _enemyLastSpawnTime;
      private Bonus? _activeBonus;

      private Timer _levelTimer;

      private Dictionary<string, Texture> _textures;
      private Shader _textureShader;

      private Character _character;
      private Interface _interface;

      private HashSet<Obstacle> _boundaryObstacles;

      private HashSet<Enemy> _enemies;
      private HashSet<Projectile> _projectiles;
      private HashSet<Bonus> _bonuses;
      private HashSet<Obstacle> _obstacles;

      #endregion

      #region Map parameters

      private VertexPositionTexture[] _MapVertices;
      private VertexBufferObject _MapVbo;
      private VertexArrayObject _MapVao;

      public Vector2 MapSize { get; init; }
      public Vector2 MapPosition { get; init; }

      #endregion


      public Level
         (
         Dictionary<string, Texture> textures,
         Shader textureShader,
         Vector2 winSize,
         Vector2 mapPosition,
         Vector2 mapSize,
         Character? character = null
         )
      {
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
         _levelTimer = new Timer(_levelPlayTime, Coordinates.PosInNDC(new Vector2(460, 1040), _winSize), Coordinates.SizeInNDC(new Vector2(1000, 10), _winSize));

         _textures = textures;
         _textureShader = textureShader;

         _enemies = new HashSet<Enemy>();
         _projectiles = new HashSet<Projectile>();
         _bonuses = new HashSet<Bonus>();


         Vector2 tileSize = Coordinates.SizeInNDC(new(1000 / 16.0f, 1000 / 16.0f), winSize);
         _obstacles = new HashSet<Obstacle>();
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(4 * tileSize.X, 3 * tileSize.Y), tileSize));
         _obstacles.Add(new Obstacle(MapPosition + new Vector2(9 * tileSize.X, 8 * tileSize.Y), tileSize));

         _boundaryObstacles = new HashSet<Obstacle>();
         // bottom
         for (int i = 0; i < 7; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * tileSize.X, 0), tileSize));
         for (int i = 7; i < 10; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * tileSize.X, -tileSize.Y), tileSize));
         for (int i = 10; i < 16; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * tileSize.X, 0), tileSize));

         // up
         for (int i = 0; i < 7; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * tileSize.X, MapSize.Y - tileSize.Y), tileSize));
         for (int i = 7; i < 10; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * tileSize.X, MapSize.Y), tileSize));
         for (int i = 10; i < 16; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(i * tileSize.X, MapSize.Y - tileSize.Y), tileSize));

         // left
         for (int i = 1; i < 6; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(0, i * tileSize.Y), tileSize));
         for (int i = 6; i < 9; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(-tileSize.X, i * tileSize.Y), tileSize));
         for (int i = 9; i < 15; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(0, i * tileSize.Y), tileSize));

         // right
         for (int i = 1; i < 6; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(MapSize.X - tileSize.X, i * tileSize.Y), tileSize));
         for (int i = 6; i < 9; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(MapSize.X, i * tileSize.Y), tileSize));
         for (int i = 9; i < 15; i++)
            _boundaryObstacles.Add(new Obstacle(MapPosition + new Vector2(MapSize.X - tileSize.X, i * tileSize.Y), tileSize));


         Vector2 characterSize = Coordinates.SizeInNDC(new Vector2(62, 62), _winSize);
         _character = character ?? new Character(characterSize, -characterSize / 2);
         _interface = new Interface(MapSize, MapPosition, _winSize);

         _activeBonus = null;
      }

      public void Update(FrameEventArgs args, Vector2 moveDir, Vector2 projectileDir, ref Stopwatch gameRunTime, ref GameState gameState)
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

            if (_enemies.Count == 0)
            {
               gameState = GameState.Win;
               return;
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

      public void Render(FrameEventArgs args, GameState gameState)
      {
         Texture.DrawTexturedRectangle(_textureShader, _textures["level1"], MapPosition, _MapVao);
         _levelTimer.Render(_textureShader, _textures["timebar"]);


         foreach (var obstacle in _boundaryObstacles)
         {
            obstacle.Render(_textureShader, _textures["invisible_obstacle"]);
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
                  texture = _textures["projectile3"];
                  break;

               case Character.ProjectileLevel.Burning:
                  texture = _textures["projectile2"];
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
         _interface.RenderHitpointIcon(_textureShader, _textures["hitpoint"]);
         _interface.RenderHitpointCounter(_textureShader, _textures["digits"], _character.HitPoints);
         _interface.RenderCoinIcon(_textureShader, _textures["coin"]);
         _interface.RenderCoinCounter(_textureShader, _textures["digits"], _character.CoinsCount);

         {
            Texture? texture = null;
            switch (gameState)
            {
               case GameState.Start:
                  texture = _textures["start"];
                  break;

               case GameState.Pause:
                  texture = _textures["pause"];
                  break;

               case GameState.GameOver:
                  texture = _textures["gameover"];
                  break;

               case GameState.Win:
                  texture = _textures["win"];
                  break;
            }
            _interface.RenderScreen(_textureShader, texture);
         }
      }


      private void SpawnProjectile(Vector2 position, Vector2 direction)
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

      private void SpawnEnemy()
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

      private void SpawnBonus(Vector2 position)
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

      public void Restart()
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

      public Character ExtractCharacter()
         => _character; 
   }
}