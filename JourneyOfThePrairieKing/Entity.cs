using CG_PR3;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System.Drawing;
using static JourneyOfThePrairieKing.Projectile;
using static System.Reflection.Metadata.BlobBuilder;

namespace JourneyOfThePrairieKing
{
   interface IMovable
   {
      public float Velocity { get; set; }

      public void ChangePosition(Vector2 position);
   }

   public abstract class Entity
   {
      public abstract Vector2 Position { get; protected set; } // Left bottom corner (in NDC).
      public abstract Vector2 Size { get; protected set; } 

      public static bool CheckCollision(Entity one, Entity two)
      {
         bool collisionX = one.Position.X + one.Size.X >= two.Position.X &&
             two.Position.X + two.Size.X >= one.Position.X;

         bool collisionY = one.Position.Y + one.Size.Y >= two.Position.Y &&
             two.Position.Y + two.Size.Y >= one.Position.Y;

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
      }

      public enum GunLevel
      {
         Default = 100,
         Medium = 80,
      }

      #region buffers

      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public override Vector2 Size { get; protected set; }
      public override Vector2 Position { get; protected set; }

      #endregion

      private float _defaultVelocity = 0.12f;
      private long _defaultReloadTime = 350;
      private int _defaultHitPoints = 0;

      public static readonly long HitCooldown = 2000; // Milliseconds

      public int CoinsCount { get; private set; }
      public ProjectileLevel Ammo { get; set; }
      public BootsLevel Boots { get; set; }
      public GunLevel Gun { get; set; }

      public long ReloadTime { get; set; } // Milliseconds
      public int HitPoints { get; set; }
      public long LastHitTime {  get; private set; } // Milliseconds
      public long LastShotTime { get; set; } // Milliseconds
      public float Velocity { get; set; }

      public Character(Vector2 size, Vector2 position)
      {
         Ammo = ProjectileLevel.Stone;
         Boots = BootsLevel.Default;
         Gun = GunLevel.Default;

         Size = size;
         Position = position;
         Velocity = _defaultVelocity * (float)((int)Boots / 100.0f);
         ReloadTime = _defaultReloadTime * (long)((int)Gun / 100.0f);
         LastHitTime = 0;
         LastShotTime = 0;
         CoinsCount = 0;
         HitPoints = _defaultHitPoints;


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

      public void GotDamage(int damage, long time)
      {
         if (time - LastHitTime < HitCooldown)
            return;

         HitPoints -= damage;
         LastHitTime = time;
      }

      public void Dispose()
      {
         _vao.Dispose();
         _vbo.Dispose();
      }

      public void Reset()
      {
         Position = new Vector2(0, 0);
         Velocity = _defaultVelocity;
         ReloadTime = _defaultReloadTime;
         LastHitTime = 0;
         LastShotTime = 0;
         HitPoints = _defaultHitPoints;
      }

      public void GotCoin(int count)
      {
         if (CoinsCount > 0)
         {
            CoinsCount += count;

            if (CoinsCount > 99)
            {
               CoinsCount = 99;
            }
         }
      }

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

      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public override Vector2 Size { get; protected set; }
      public override Vector2 Position { get; protected set; }

      public EnemyType Type { get; init; }
      public int HitPoints { get; private set; }
      public float Velocity { get; set; }

      public Enemy(Vector2 size, Vector2 position, EnemyType type)
      {
         Type = type;
         Size = size;
         Position = position;

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

      public void GotDamage(int damage)
      {
         HitPoints -= damage;
      }


      public void Dispose()
      {
         _vao.Dispose();
         _vbo.Dispose();
      }

      public void ChangePosition(Vector2 position)
      {
         Position = position;
      }
   }

   public sealed class Projectile : Entity, IMovable
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public override Vector2 Size { get; protected set; }
      public override Vector2 Position { get; protected set; }

      private float _defaultVelocity = 0.00125f;

      public int Damage { get; }
      public Vector2 Direction { get; }
      public  float Velocity { get; set; }

      public Projectile(Vector2 size, Vector2 position, Vector2 direction, int damage)
      {
         Size = size;
         Position = position;
         Direction = direction;
         Velocity = _defaultVelocity;

         Damage = damage;

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
         _vao.Dispose();
         _vbo.Dispose();
      }

      public void ChangePosition(Vector2 position)
      {
         Position = position;
      }
   }

   public sealed class Obstacle : Entity
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public override Vector2 Size { get; protected set; }
      public override Vector2 Position { get; protected set; }

      public Obstacle(Vector2 position, Vector2 size)
      {
         Position = position;
         Size = size;

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
   }

   public sealed class Bonus : Entity
   {
      public enum BonusType
      {
         Wheel,
         ShotGun,
         Nuke,
         HitPoint,
      }

      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public override Vector2 Size { get; protected set; }
      public override Vector2 Position { get; protected set; }

      public Bonus(Vector2 position, Vector2 size)
      {
         Position = position;
         Size = size;

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
   }
}