using JourneyOfThePrairieKing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;


namespace JourneyOfThePrairieKing
{
   interface IMovable
   {
      public float Velocity { get; set; }

      public void ChangePosition(Vector2 position);
   }

   public class Entity : IDisposable
   {

      #region buffers

      protected VertexPositionTexture[] _vertices;
      protected VertexBufferObject _vbo;
      protected VertexArrayObject _vao;

      public Vector2 Size { get; protected set; } // Left bottom corner (in NDC).
      public Vector2 Position { get; protected set; }

      #endregion

      protected Entity(Vector2 size, Vector2 position)
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

         _vbo = new VertexBufferObject(_vertices, BufferUsageHint.StreamDraw);
         _vbo.Bind();

         _vao = new VertexArrayObject();
         _vao.Bind();
      }

      public void Render(Shader shader, Texture texture)
      {
         Texture.DrawTexturedRectangle(shader, texture, Position, _vao);
      }

      public void Dispose()
      {
         _vbo.Dispose();
         _vao.Dispose();
         GC.SuppressFinalize(this);
      }

      public static bool CheckCollision(Entity one, Entity two)
      {
         var colSizeOne = one.Size;
         var colSizeTwo = two.Size;

         //var colSizeOne = one.Size * 0.8f;
         //var colSizeTwo = two.Size * 0.8f;


         bool collisionX = one.Position.X + colSizeOne.X >= two.Position.X &&
             two.Position.X + colSizeTwo.X >= one.Position.X;

         bool collisionY = one.Position.Y + colSizeOne.Y >= two.Position.Y &&
             two.Position.Y + colSizeTwo.Y >= one.Position.Y;

         return collisionX && collisionY;
      }
   }

   public sealed class Character : Entity, IMovable
   {
      public enum ProjectileLevel
      {
         Stone = 1,
         Metal = 2,
         Burning = 3,
      }

      public enum BootsLevel
      {
         Default = 100,
         Medium = 120,
         High = 140,
      }

      public enum GunLevel
      {
         Default = 100,
         Medium = 80,
         High = 60,
      }

      private float _defaultVelocity = 0.12f;
      private long _defaultReloadTime = 350;
      private int _defaultHitPoints = 0;

      public static readonly long HitCooldown = 2000; // Milliseconds

      public int CoinsCount { get; private set; }
      public ProjectileLevel Ammo { get; private set; }
      public BootsLevel Boots { get; private set; }
      public GunLevel Gun { get; private set; }

      public long ReloadTime { get; set; } // Milliseconds
      public int HitPoints { get; set; }
      public long LastHitTime {  get; private set; } // Milliseconds
      public long LastShotTime { get; set; } // Milliseconds
      public float Velocity { get; set; }

      public Character(Vector2 size, Vector2 position) : base(size, position)
      {
         Ammo = ProjectileLevel.Stone;
         Boots = BootsLevel.Default;
         Gun = GunLevel.Default;

         Velocity = _defaultVelocity * (float)((int)Boots / 100.0f);
         ReloadTime = (long)(_defaultReloadTime * ((int)Gun / 100.0f));
         LastHitTime = 0;
         LastShotTime = 0;
         CoinsCount = 0;
         HitPoints = _defaultHitPoints;
      }

      public void GotDamage(int damage, long time)
      {
         if (time - LastHitTime < HitCooldown)
            return;
         System.Media.SoundPlayer player = new System.Media.SoundPlayer(@"classic_hurt.wav");
         player.Play();
         HitPoints -= damage;
         LastHitTime = time;
      }

      public void Reset()
      {
         Position = new Vector2(0, 0);
         Velocity = _defaultVelocity;
         ReloadTime = _defaultReloadTime;
         LastHitTime = 0;
         LastShotTime = 0;
         HitPoints = _defaultHitPoints;
         CoinsCount = 0;

         Ammo = ProjectileLevel.Stone;
         Boots = BootsLevel.Default;
         Gun = GunLevel.Default;
      }

      public void GotCoin(int count)
      {
         if (count > 0)
         {
            CoinsCount += count;

            if (CoinsCount > 99)
            {
               CoinsCount = 99;
            }
         }
      }

      public void SpendCoins(int count)
      {
         if (count > 0)
         {
            if (CoinsCount < count)
            {
               return;
            }
            CoinsCount -= count;
         }
      }

      public void SetGun(GunLevel level)
      {
         Gun = level;
         ReloadTime = (long)(_defaultReloadTime * ((int)level / 100.0f));
         //Console.WriteLine(ReloadTime);
      }

      public void SetBoots(BootsLevel level)
      {
         Boots = level;
         Velocity = _defaultVelocity * (float)((int)level / 100.0f);
      }

      public void SetAmmo(ProjectileLevel level)
         => Ammo = level;

      public void ChangePosition(Vector2 position)
      {
         Position = position;
      }
   }

   public sealed class Enemy : Entity, IMovable
   {
      public enum EnemyType
      {
         Log,
         Ghost,
         Knight,
      }

      public EnemyType Type { get; init; }
      public int HitPoints { get; private set; }
      public float Velocity { get; set; }

      public Enemy(Vector2 size, Vector2 position, EnemyType type) : base(size, position)
      {
         Type = type;
         switch (type)
         {
            case EnemyType.Log:
               Velocity = 0.05f;
               HitPoints = 2;
               break;

            case EnemyType.Knight:
               Velocity = 0.03f;
               HitPoints = 3;
               break;

            case EnemyType.Ghost:
               Velocity = 0.07f;
               HitPoints = 1;
               break;
         }
      }
      public void GotDamage(int damage)
      {
         HitPoints -= damage;
      }

      public void ChangePosition(Vector2 position)
      {
         Position = position;
      }
   }

   public sealed class Boss : Entity, IMovable
   {
      public enum BossPhase
      {
         First,
         Second,
         Third,
      }

      private float prevDistanceLength;
      private const float distanceEps = 0.001f;
      private long _defaultReloadTime = 400;


      public static readonly int DefaultHP = 120;

      public bool IsDead { get; set; }
      public int HitPoints { get; private set; }
      public float Velocity { get; set; }
      public BossPhase Phase { get; set; }
      public Vector2 Direction { get; private set; }
      public Vector2 Destination { get; private set; }

      public long ReloadTime { get; set; } // Milliseconds
      public long LastShotTime { get; set; } // Milliseconds

      public Boss(Vector2 size, Vector2 position) : base(size, position)
      {
         prevDistanceLength = 2;
         Velocity = 0.2f;
         HitPoints = DefaultHP;
         Phase = BossPhase.First;
         IsDead = false;

         ReloadTime = _defaultReloadTime;
         LastShotTime = 0;
      }

      public void GotDamage(int damage)
      {
         HitPoints -= damage;
      }

      public void ChangePosition(Vector2 position)
      {
         Position = position;
      }

      public void ChangeDirection(Vector2 direction)
      {
        Direction = direction;
      }

      public void ChangeDestination(Vector2 destination)
      {
         Destination = destination;
      }

      public bool OnHisDestination()
      {
         bool onPos = false;
         var distance = Destination - Position;

         if (distance.Length < distanceEps
            || distance.Length - prevDistanceLength > 0)
         {
            onPos = true;
         }
         prevDistanceLength =  distance.Length;
         return onPos;
      }

      public void StopMoving()
      {
         Destination = Position;
         Direction = Vector2.Zero;
         prevDistanceLength = 2;
      }
   }

   public sealed class Projectile : Entity, IMovable
   {
      private const float _defaultVelocity = 0.2f;

      public int Damage { get; }
      public Vector2 Direction { get; }
      public  float Velocity { get; set; }

      public Projectile(Vector2 size, Vector2 position, Vector2 direction, int damage, float velocity = _defaultVelocity) : base(size, position)
      {
         Direction = direction;
         Velocity = velocity;

         Damage = damage;
      }

      public void ChangePosition(Vector2 position)
      {
         Position = position;
      }
   }

   public sealed class Obstacle : Entity
   {
      public Obstacle(Vector2 position, Vector2 size) : base(size, position) { }
   }

   public sealed class Bonus : Entity
   {
      public enum BonusType
      {
         Wheel,
         ShotGun,
         Nuke,
         HitPoint,
         Coin,
      }
      private long _remainDuration;
      private long _remainTTL;

      public long Duration { get; init; }
      public long TTL { get; init; }
      public BonusType Type { get; init; }

      public Bonus(Vector2 position, Vector2 size, BonusType bonusType, long bonusDuration, long TTL) : base(size, position)
      {
         Type = bonusType;
         Duration = bonusDuration;
         _remainDuration = bonusDuration;
         _remainTTL = TTL;
      }

      public bool IsDurationEnded (long EllapsedMilliSec)
      {
         bool isEnded = false;
         _remainDuration -= EllapsedMilliSec;
         if (_remainDuration <= 0)
            isEnded = true;
         return isEnded;
      }

      public bool IsTTLEnded(long EllapsedMilliSec)
      {
         bool isEnded = false;
         _remainTTL -= EllapsedMilliSec;
         if (_remainTTL <= 0)
            isEnded = true;
         return isEnded;
      }
   }
}