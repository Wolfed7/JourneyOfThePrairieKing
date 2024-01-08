using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System.Diagnostics;

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
      protected VertexArrayObject _mapVao;

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
         _mapVao = new VertexArrayObject();
         _mapVao.Bind();


         _enemySpawnAllowed = true;
         _enemyLastSpawnTime = 0;

         _textures = textures;
         _textureShader = textureShader;

         _enemies = new HashSet<Enemy>();
         _projectiles = new HashSet<Projectile>();
         _bonuses = new HashSet<Bonus>();

         #region Obstacles declaration

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

         #endregion

         Vector2 characterSize = Coordinates.SizeInNDC(new Vector2(62, 62), _winSize);
         _character = character ?? new Character(characterSize, -characterSize / 2);

         #region Interface declaration

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

         var w = _tileSize * 0.6f;
         var ammo2 = new LevelInterface(w, new Vector2(MapPosition.X - MapSize.X / 10, MapPosition.Y + MapSize.Y * 0.65f), _textures["ammo2"]);
         var ammo3 = new LevelInterface(w, new Vector2(MapPosition.X - MapSize.X / 10, MapPosition.Y + MapSize.Y * 0.65f), _textures["ammo3"]);

         var gun2 = new LevelInterface(w, new Vector2(MapPosition.X - MapSize.X / 10, MapPosition.Y + MapSize.Y * 0.6f), _textures["gun2"]);
         var gun3 = new LevelInterface(w, new Vector2(MapPosition.X - MapSize.X / 10, MapPosition.Y + MapSize.Y * 0.6f), _textures["gun3"]);

         var boots2 = new LevelInterface(w, new Vector2(MapPosition.X - MapSize.X / 10, MapPosition.Y + MapSize.Y * 0.55f), _textures["shoes"]);

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

         _Interfaces.Add("ammo2", ammo2);
         _Interfaces.Add("ammo3", ammo3);

         _Interfaces.Add("gun2", gun2);
         _Interfaces.Add("gun3", gun3);

         _Interfaces.Add("boots2", boots2);

         #endregion 


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
                     Console.WriteLine("Exit from level");
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
               _bonuses.Remove(bonus);
               bonus.Dispose();
            }

            if (Entity.CheckCollision(_character, bonus) is true)
            {
               if (bonus.Duration != 0)
                  _activeBonus = bonus;

               switch (bonus.Type)
               {
                  case Bonus.BonusType.Wheel:
                     _bonuses.Remove(bonus);
                     bonus.Dispose();
                     break;

                  case Bonus.BonusType.ShotGun:
                     _bonuses.Remove(bonus);
                     bonus.Dispose();
                     break;

                  case Bonus.BonusType.Nuke:
                     _enemies.Clear();
                     _bonuses.Remove(bonus);
                     bonus.Dispose();
                     break;

                  case Bonus.BonusType.HitPoint:
                     if (_character.HitPoints < 9)
                     {
                        _character.HitPoints++;
                        _bonuses.Remove(bonus);
                        bonus.Dispose();
                     }
                     break;

                  case Bonus.BonusType.Coin:
                     if (_character.CoinsCount < 99)
                     {
                        _character.GotCoin(1);
                        _bonuses.Remove(bonus);
                        bonus.Dispose();
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
                  projectile.Dispose();
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
                     enemy.Dispose();
                  }
                  _projectiles.Remove(projectile);
                  projectile.Dispose();
                  //Console.WriteLine("Enemy hit!");
                  break;
               }
            }
         }

         #endregion
      }

      public virtual void Render(FrameEventArgs args, GameState gameState)
      {
         Texture.DrawTexturedRectangle(_textureShader, _textures[_mapTextureName], MapPosition, _mapVao);
         _levelTimer.Render(_textureShader, _textures["timebar"]);

         if (_enemies.Count == 0 && _enemySpawnAllowed is false)
         {
            _Interfaces["arrow"].Render(_textureShader);
         }
         foreach (var obstacle in _obstacles)
         {
            obstacle.Render(_textureShader, _textures["obstacle"]);
         }

         DrawProjectiles();

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


         DrawBonuses();

         DrawInterface(gameState);
      }

      protected virtual void DrawBonuses()
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

      protected virtual void DrawProjectiles()
      {
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
      }

      protected virtual void DrawInterface(GameState gameState)
      {
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
               _Interfaces["gameWin"].Render(_textureShader);
               break;
         }

         switch (_character.Gun)
         {
            case Character.GunLevel.Medium:
               _Interfaces["gun2"].Render(_textureShader);
               break;

            case Character.GunLevel.High:
               _Interfaces["gun3"].Render(_textureShader);
               break;
         }

         switch (_character.Ammo)
         {
            case Character.ProjectileLevel.Metal:
               _Interfaces["ammo2"].Render(_textureShader);
               break;

            case Character.ProjectileLevel.Burning:
               _Interfaces["ammo3"].Render(_textureShader);
               break;
         }

         switch (_character.Boots)
         {
            case Character.BootsLevel.Medium:
               _Interfaces["boots2"].Render(_textureShader);
               break;
         }
      }

      protected virtual void SpawnProjectile(Vector2 position, Vector2 direction)
      {
         Vector2 projectileSize = Coordinates.SizeInNDC(new Vector2(16, 16), _winSize);
         int projDamage = (int)_character.Ammo;


         if (_activeBonus?.Type == Bonus.BonusType.Wheel)
         {
            for (int i = -1; i < 2; i++)
            {
               for (int j = -1; j < 2; j++)
               {
                  direction = new Vector2(i, j);
                  direction.Normalize();
                  _projectiles.Add(new Projectile(projectileSize, position, direction, projDamage));
               }
            }
         }
         else if (_activeBonus?.Type == Bonus.BonusType.ShotGun)
         {
            Vector2 rotatedDirection;
            _projectiles.Add(new Projectile(projectileSize, position, direction, projDamage));
            rotatedDirection = Matrix2.CreateRotation((float)Math.PI / 9) * direction;
            _projectiles.Add(new Projectile(projectileSize, position, rotatedDirection, projDamage));
            rotatedDirection = Matrix2.CreateRotation(-(float)Math.PI / 9) * direction;
            _projectiles.Add(new Projectile(projectileSize, position, rotatedDirection, projDamage));
         }
         else
         {
            _projectiles.Add(new Projectile(projectileSize, position, direction, projDamage));
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
         var prob = 0.35f;
         if (rand.NextDouble() > prob)
         {
            return;
         }

         var prob0 = 0.7f; // coin
         var prob1 = 0.1f; // shotgun
         var prob2 = 0.1f; // wheel
         var prob3 = 0.05f; // life
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
      private const long _levelPlayTime = 120_000;
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

         if (_levelTimer.IsTimeEnded is true)
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

   public sealed class Shop : Level
   {
      private List<Obstacle> _shopList; 
      private Dictionary<string, LevelInterface> _pricesInterface;
      private List<int> _prices;
      private List<Obstacle> _itemHolders;
      private List<bool> _available;

      public Shop
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
         _enemySpawnAllowed = false;
         _levelTimer = new Timer(1, Coordinates.PosInNDC(new Vector2(460, 1040), _winSize), new Vector2(MapSize.X, MapSize.Y / 100));
         _shopList = new List<Obstacle>();
         _itemHolders = new List<Obstacle>();

         _pricesInterface = new Dictionary<string, LevelInterface>();
         _available = new List<bool> { false, false, false };

         _prices = new List<int> { 0, 0, 0 };
      }

      public override void OnCharacterSet(Character character)
      {
         base.OnCharacterSet(character);

         var shopStartPosition = new Vector2(MapPosition.X + MapSize.X * 0.2f, MapPosition.Y + MapSize.Y * 0.3f);
         var itemSize = Coordinates.SizeInNDC(new Vector2(48, 48), _winSize);
         var itemHolderSize = Coordinates.SizeInNDC(new Vector2(64, 64), _winSize);
         var coinSize = Coordinates.SizeInNDC(new Vector2(16, 16), _winSize);
         var digitSize = Coordinates.SizeInNDC(new Vector2(12, 24), _winSize);

         for (int i = 0; i < 3; i++)
         {
            var itemPosition = shopStartPosition + new Vector2(itemSize.X * i + itemSize.X / 2 * i, 0);
            _shopList.Add(new Obstacle(itemPosition, itemSize));

            _itemHolders.Add(new Obstacle(itemPosition - new Vector2(itemSize.X / 5, itemSize.Y / 4), itemHolderSize));
         }


         string interfaceName = "";
         for (int k = 0; k < 3; k++)
         {
            switch (k)
            {
               case 0:
                  switch (_character.Ammo)
                  {
                     case Character.ProjectileLevel.Stone:
                        _prices[k] = 15;
                        _available[k] = true;
                        break;
                     case Character.ProjectileLevel.Metal:
                        _prices[k] = 25;
                        _available[k] = true;
                        break;
                     case Character.ProjectileLevel.Burning:
                        continue;
                  }
                  break;
               case 1:
                  switch (_character.Gun)
                  {
                     case Character.GunLevel.Default:
                        _prices[k] = 10;
                        _available[k] = true;
                        break;
                     case Character.GunLevel.Medium:
                        _prices[k] = 20;
                        _available[k] = true;
                        break;
                     case Character.GunLevel.High:
                        continue;
                  }
                  break;
               case 2:
                  switch (_character.Boots)
                  {
                     case Character.BootsLevel.Default:
                        _prices[k] = 25;
                        _available[k] = true;
                        break;
                     case Character.BootsLevel.Medium:
                        continue;
                  }

                  break;

            }


            interfaceName = "item" + $"{k}" + "_coin";
            var CoinIcon = new LevelInterface(coinSize, new Vector2(shopStartPosition.X + itemSize.X * k + itemSize.X / 2 * k + 2.3f * digitSize.X, shopStartPosition.Y - 1.0f * itemSize.Y + coinSize.Y / 4), _textures["coin"]);
            _pricesInterface.Add(interfaceName, CoinIcon);

            interfaceName = "item" + $"{k}" + "_counter";
            var CostCounter = new Counter(digitSize, new Vector2(shopStartPosition.X + itemSize.X * k + itemSize.X / 2 * k, shopStartPosition.Y - 1.0f * itemSize.Y), _textures["digits"], 10);
            _pricesInterface.Add(interfaceName, CostCounter);
         }
      }

      public override void Update(FrameEventArgs args, Vector2 moveDir, Vector2 projectileDir, ref Stopwatch gameRunTime, ref GameState gameState)
      {
         base.Update(args, moveDir, projectileDir, ref gameRunTime, ref gameState);

         for (int i = 0; i < 3; i++)
         {
            if (_available[i] is true)
            {
               if (Entity.CheckCollision(_shopList[i], _character) is true)
               {
                  if (_prices[i] > _character.CoinsCount)
                  {
                     continue;
                  }

                  switch (i)
                  {
                     case 0:
                        switch (_character.Ammo)
                        {
                           case Character.ProjectileLevel.Stone:
                              _character.SetAmmo(Character.ProjectileLevel.Metal);
                              break;
                           case Character.ProjectileLevel.Metal:
                              _character.SetAmmo(Character.ProjectileLevel.Burning);
                              break;
                           case Character.ProjectileLevel.Burning:
                              continue;
                        }
                        break;
                     case 1:
                        switch (_character.Gun)
                        {
                           case Character.GunLevel.Default:
                              _character.SetGun(Character.GunLevel.Medium);
                              break;
                           case Character.GunLevel.Medium:
                              _character.SetGun(Character.GunLevel.High);
                              break;
                           case Character.GunLevel.High:
                              continue;
                        }
                        break;
                     case 2:
                        switch (_character.Boots)
                        {
                           case Character.BootsLevel.Default:
                              _character.SetBoots(Character.BootsLevel.Medium);
                              break;
                           case Character.BootsLevel.Medium:
                              continue;
                        }

                        break;
                  }

                  _character.SpendCoins(_prices[i]);
                  _shopList[i].Dispose();
                  _available[i] = false;

                  break;

               }
            }
         }
      }

      public override void Render(FrameEventArgs args, GameState gameState)
      {
         Texture.DrawTexturedRectangle(_textureShader, _textures[_mapTextureName], MapPosition, _mapVao);

         if (_enemies.Count == 0 && _enemySpawnAllowed is false)
         {
            _Interfaces["arrow"].Render(_textureShader);
         }

         foreach (var itemHolder in _itemHolders)
         {
            itemHolder.Render(_textureShader, _textures["bonusholder"]);
         }

         for (int i = 0; i < 3; i++)
         {
            if (_available[i] is true)
            {
               switch (i)
               {
                  case 0:
                     switch (_character.Ammo)
                     {
                        case Character.ProjectileLevel.Stone:
                           _shopList[i].Render(_textureShader, _textures["ammo2"]);
                           break;
                        case Character.ProjectileLevel.Metal:
                           _shopList[i].Render(_textureShader, _textures["ammo3"]);
                           break;
                        case Character.ProjectileLevel.Burning:
                           continue;
                     }
                     break;
                  case 1:
                     switch (_character.Gun)
                     {
                        case Character.GunLevel.Default:
                           _shopList[i].Render(_textureShader, _textures["gun2"]);
                           break;
                        case Character.GunLevel.Medium:
                           _shopList[i].Render(_textureShader, _textures["gun3"]);
                           break;
                        case Character.GunLevel.High:
                           continue;
                     }
                     break;
                  case 2:
                     switch (_character.Boots)
                     {
                        case Character.BootsLevel.Default:
                           _shopList[i].Render(_textureShader, _textures["shoes"]);
                           break;
                        case Character.BootsLevel.Medium:
                           continue;
                     }

                     break;

               }
            }
         }

         for (int i = 0; i < 3; i++)
         {
            string interfaceName = "item" + $"{i}" + "_coin";
            if (_available[i] is true && _pricesInterface.ContainsKey(interfaceName) is false)
            {
               _pricesInterface[interfaceName].Render(_textureShader);
            }
         }

         for (int i = 0; i < 3; i++)
         {
            string interfaceName = "item" + $"{i}" + "_counter";
            if (_available[i] is true && _pricesInterface.ContainsKey(interfaceName) is false)
            {
               _pricesInterface[interfaceName].Render(_textureShader, _prices[i]);
            }
         }

         DrawProjectiles();

         _character.Render(_textureShader, _textures["char1"]);
         DrawInterface(gameState);
      }
   }

   public sealed class Level2 : Level
   {
      #region Constants

      private const long _enemySpawnCooldown = 2_000;
      private const long _levelPlayTime = 120_000;
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

         if (_levelTimer.IsTimeEnded is true)
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
      private long _lastSummonTime;

      private Vector2 _bossProjectileSize;
      private Vector2 _summonedEnemiesSize;
      private Vector2 _bossStartPosition;
      private Vector2 _charStartPosition;

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
         _charStartPosition = Coordinates.PosInNDC(new Vector2(_winSize.X / 2 - 31, 800), _winSize);
         _bossStartPosition = Coordinates.PosInNDC(new Vector2(_winSize.X / 2 - 2 * _tileSize.X, 200), _winSize) ;
         _boss = new Boss(new Vector2(MapSize.X / 8.0f, MapSize.Y / 8.0f), _bossStartPosition);
         _levelTimer = new Timer(_boss.HitPoints, Coordinates.PosInNDC(new Vector2(460, 1040), _winSize), new Vector2(MapSize.X, MapSize.Y / 100));
         _bossProjectiles = new HashSet<Projectile>();
         _bossProjectileSize = Coordinates.SizeInNDC(new Vector2(24, 24), _winSize);
         _summonedEnemiesSize = Coordinates.SizeInNDC(new Vector2(36, 62), _winSize);
         _lastSummonTime = 0;
         BossDecision();
      }

      public override void OnCharacterSet(Character character)
      {
         character.ChangePosition(_charStartPosition);
      }

      public override void Update(FrameEventArgs args, Vector2 moveDir, Vector2 projectileDir, ref Stopwatch gameRunTime, ref GameState gameState)
      {
         base.Update(args, moveDir, projectileDir, ref gameRunTime, ref gameState);

         long MillisecLastUpdate = (long)(args.Time * 1000);

         if (_activeBonus?.IsDurationEnded(MillisecLastUpdate) is true)
         {
            _activeBonus = null;
         }

         float deltaTime = (float)args.Time;

         //Console.WriteLine(deltaTime);
         deltaTime = (float)Math.Min(deltaTime, FrameRate.MaxDeltaTime);
         deltaTime = (float)Math.Max(deltaTime, FrameRate.MinDeltaTime);

         if (_levelTimer.IsTimeEnded && _boss.IsDead is false)
         {
            _boss.IsDead = true;
            _enemySpawnAllowed = false;
            RemoveBossSummons();
         }

         if (_boss.IsDead is true)
         {
            return;
         }

         //Console.WriteLine(_levelTimer.Ratio);
         if (_levelTimer.Ratio < 0.67f && _boss.Phase == Boss.BossPhase.First)
         {
            _boss.Phase = Boss.BossPhase.Second;
         }
         if (_levelTimer.Ratio < 0.33f && _boss.Phase == Boss.BossPhase.Second)
         {
            _boss.Phase = Boss.BossPhase.Third;
            _boss.Velocity *= 0.8f;
         }

         switch (_boss.Phase)
         {
            case Boss.BossPhase.First:
            {
               if (_boss.OnHisDestination() is true)
               {
                  _boss.StopMoving();
                  if (gameRunTime.ElapsedMilliseconds - _boss.LastShotTime > _boss.ReloadTime)
                  {
                     _boss.LastShotTime = gameRunTime.ElapsedMilliseconds;
                     SpawnBossProjectiles(_boss.Phase, gameRunTime.ElapsedMilliseconds);
                  }
               }

               _boss.ChangePosition(_boss.Position + _boss.Direction * deltaTime * _winSizeToSquare * _boss.Velocity);

               foreach (var projectile in _projectiles)
               {
                  if (Entity.CheckCollision(_boss, projectile))
                  {
                     BossDecision();
                     projectile.Dispose();
                     _projectiles.Remove(projectile);
                     _levelTimer.Update(projectile.Damage);
                  }
               }
               break;
            }
            case Boss.BossPhase.Second:
            {
               if (_boss.OnHisDestination() is true)
               {
                  _boss.StopMoving();
                  if (gameRunTime.ElapsedMilliseconds - _boss.LastShotTime > _boss.ReloadTime * 1.3)
                  {
                     _boss.LastShotTime = gameRunTime.ElapsedMilliseconds;
                     SpawnBossProjectiles(_boss.Phase, gameRunTime.ElapsedMilliseconds);
                  }

                  if (gameRunTime.ElapsedMilliseconds % 4000 == 1)
                  {
                     BossDecision();
                  }
               }

               _boss.ChangePosition(_boss.Position + _boss.Direction * deltaTime * _winSizeToSquare * _boss.Velocity);

               foreach (var projectile in _projectiles)
               {
                  if (Entity.CheckCollision(_boss, projectile))
                  {
                     projectile.Dispose();
                     _projectiles.Remove(projectile);
                     _levelTimer.Update(projectile.Damage);
                  }
               }
               break;
            }
            case Boss.BossPhase.Third:
               if (_boss.OnHisDestination() is true)
               {
                  _boss.StopMoving();
                  if (gameRunTime.ElapsedMilliseconds - _boss.LastShotTime > _boss.ReloadTime * 1.7f)
                  {
                     _boss.LastShotTime = gameRunTime.ElapsedMilliseconds;
                     SpawnBossProjectiles(_boss.Phase, gameRunTime.ElapsedMilliseconds);
                  }

                  if (gameRunTime.ElapsedMilliseconds % 10000 < 10)
                  {
                     BossDecision();
                  }

                  if (gameRunTime.ElapsedMilliseconds - _lastSummonTime > 5000)
                  {
                     _lastSummonTime = gameRunTime.ElapsedMilliseconds;
                     var pos = _boss.Position + _boss.Size / 2;
                     List<float> spawnPosX = new List<float> { pos.X + _boss.Size.X / 2, pos.X - _boss.Size.X / 2 };
                     List<float> spawnPosY = new List<float> { pos.Y + _boss.Size.Y / 2, pos.Y - _boss.Size.Y / 2 };

                     var rand = new Random();

                     for (int i = 0; i < 4; i++)
                     {
                        var iX = rand.Next(spawnPosX.Count);
                        var iY = rand.Next(spawnPosY.Count);

                        _enemies.Add(new Enemy(_summonedEnemiesSize, new Vector2(spawnPosX[iX], spawnPosY[iY]), Enemy.EnemyType.Ghost));
                     }
                  }
               }

               _boss.ChangePosition(_boss.Position + _boss.Direction * deltaTime * _winSizeToSquare * _boss.Velocity);

               foreach (var projectile in _projectiles)
               {
                  if (Entity.CheckCollision(_boss, projectile))
                  {
                     _projectiles.Remove(projectile);
                     projectile.Dispose();

                     if (_enemies.Count == 0)
                     {
                        _levelTimer.Update(projectile.Damage);
                     }
                  }
               }
               break;
         }


         foreach (var projectile in _bossProjectiles)
         {
            projectile.ChangePosition(projectile.Position + projectile.Direction * deltaTime * _winSizeToSquare * projectile.Velocity);

            if (Entity.CheckCollision(_character, projectile))
            {
               _bossProjectiles.Remove(projectile);
               projectile.Dispose();

               _character.GotDamage(1, gameRunTime.ElapsedMilliseconds);
               if (_character.HitPoints < 0)
               {
                  gameState = GameState.GameOver;
               }
            }

            foreach (var obstacle in _boundaryObstacles)
            {
               if (Entity.CheckCollision(obstacle, projectile))
               {
                  _bossProjectiles.Remove(projectile);
                  projectile.Dispose();
               }
            }
         }
      }


      public override void Render(FrameEventArgs args, GameState gameState)
      {
         Texture.DrawTexturedRectangle(_textureShader, _textures[_mapTextureName], MapPosition, _mapVao);
         _levelTimer.Render(_textureShader, _textures["bossbar"]);

         if (_boss.IsDead)
         {
            _Interfaces["arrow"].Render(_textureShader);
         }

         DrawProjectiles();

         DrawBonuses();

         _character.Render(_textureShader, _textures["char1"]);

         foreach (var enemy in _enemies)
         {
            enemy.Render(_textureShader, _textures["icefire"]);
         }

         if (_boss.IsDead is false)
         {
            _boss.Render(_textureShader, _textures["boss"]);
         }

         foreach (var projectile in _bossProjectiles)
         {
            projectile.Render(_textureShader, _textures["fireball"]);
         }


         DrawInterface(gameState);
      }

      private void BossDecision()
      {
         var rand = new Random();
         int randTileX = rand.Next(1, 14);
         int randTileY = rand.Next(1, 14);


         Vector2 randPoint = new Vector2(MapPosition.X + _tileSize.X * randTileX, MapPosition.Y + _tileSize.Y * randTileY);
         var direction = randPoint - _boss.Position;
         direction.Normalize();

         _boss.ChangeDestination(randPoint);
         _boss.ChangeDirection(direction);
      }

      private void SpawnBossProjectiles(Boss.BossPhase phase, long ellapsedMilliseconds)
      {
         var projectilePos = _boss.Position + _boss.Size / 2;

         if (phase is Boss.BossPhase.Second)
         {
            for (int i = -1; i < 2; i++)
            {
               for (int j = -1; j < 2; j++)
               {
                  var direction = new Vector2(i, j);
                  direction.Normalize();

                  var angle = (float)(ellapsedMilliseconds % 10 * Math.PI / 10);
                  direction = Matrix2.CreateRotation(angle) * direction;

                  _bossProjectiles.Add(new Projectile(_bossProjectileSize, projectilePos, direction, 1, 0.15f));
               }
            }
         }
         else
         {
            var vecToChar = ((_character.Position + _character.Size / 2) - (_boss.Position + _boss.Size / 2)) / _winSizeToSquare;
            vecToChar.Normalize();
            _bossProjectiles.Add(new Projectile(_bossProjectileSize, projectilePos, vecToChar, 1, 0.15f));
         }
      }

      public override void Restart()
      {
         base.Restart();

         _bossProjectiles.Clear();
         _character.ChangePosition(_charStartPosition);
         _boss.ChangePosition(_bossStartPosition);
         _boss.Phase = Boss.BossPhase.First;
         _levelTimer.Reset();
         BossDecision();
         _lastSummonTime = 0;
      }

      private void RemoveBossSummons()
      {
         foreach (var projectile in _bossProjectiles)
         {
            _bossProjectiles.Remove(projectile);
            projectile.Dispose();
         }

         foreach (var enemy in _enemies)
         {
            _enemies.Remove(enemy);
            enemy.Dispose();
         }

      }
   }
}