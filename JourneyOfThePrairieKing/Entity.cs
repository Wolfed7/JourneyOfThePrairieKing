using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System.Drawing;

namespace JourneyOfThePrairieKing
{
   public abstract class Entity
   {
      public abstract Vector2 Size { get; init; }

      public abstract Vector2 Position { get; set; }

      public abstract float Velocity { get; set; }

      public static bool CheckCollision(Entity one, Entity two)
      {
         bool collisionX = one.Position.X + one.Size.X >= two.Position.X &&
             two.Position.X + two.Size.X >= one.Position.X;

         bool collisionY = one.Position.Y + one.Size.Y >= two.Position.Y &&
             two.Position.Y + two.Size.Y >= one.Position.Y;

         return collisionX && collisionY;
      }
   }

   public sealed class Character : Entity
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public static readonly int DefaultHitPoints = 0;
      public static readonly int DefaultDamage = 1;
      public static readonly long DefaultReloadTime = 350;
      public static readonly float DefaultVelocity = 0.12f;
      public static readonly long HitCooldown = 2000; // Milliseconds

      public override Vector2 Size { get; init; }
      public override Vector2 Position { get; set; }
      public override float Velocity { get; set; }
      public long ReloadTime { get; set; } // Milliseconds
      public int Damage { get; set; }
      public int HitPoints { get; set; }
      public long LastHitTime {  get; private set; } // Milliseconds
      public long LastShotTime { get; set; } // Milliseconds

      public Character()
      {
         Size = new Vector2(128.0f / 1920, 128.0f / 1080);
         Position = new Vector2(0, 0);
         Velocity = 0.12f;
         ReloadTime = 350;
         LastHitTime = 0;
         LastShotTime = 0;
         HitPoints = 2;

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

      public void Draw()
      {
         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertices.Length);
      }

      public void GotDamage(int damage, long time)
      {
         if (time - LastHitTime < HitCooldown)
            return;

         HitPoints -= damage;
         LastHitTime = time;
      }

      public void Die()
      {
         _vao.Dispose();
         _vbo.Dispose();
      }

      public void Reset()
      {
         Position = new Vector2(0, 0);
         HitPoints = DefaultHitPoints;
         LastHitTime = 0;
         LastShotTime = 0;
      }
   }

   public sealed class Enemy : Entity
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

      public static readonly int DefaultHitPoints = 1;
      public static readonly int DefaultDamage = 1;
      public static readonly float DefaultVelocity = 1.0f;

      public override Vector2 Size { get; init; }
      public override Vector2 Position { get; set; }
      public override float Velocity { get; set; }
      public int Damage { get; set; }
      public int HitPoints { get; set; }
      public EnemyType Type { get; init; }

      public Enemy(EnemyType type)
      {
         Size = new Vector2(110.0f / 1920, 110.0f / 1080);
         Position = new Vector2(0, 0);

         switch (type)
         {
            case EnemyType.Log:
               Velocity = 0.05f;
               Type = type;
               HitPoints = 2;
               break;
            case EnemyType.Knight:
               Velocity = 0.03f;
               Type = type;
               HitPoints = 3;
               break;
            case EnemyType.Ghost:
               Velocity = 0.07f;
               Type = type;
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

      public void Draw()
      {
         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertices.Length);
      }

      public void GotDamage(int damage)
      {
         HitPoints -= damage;
      }


      public void Die()
      {
         _vao.Dispose();
         _vbo.Dispose();
      }
   }

   public sealed class Projectile : Entity
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      //public long LivingTime = 3000;
      public int Damage { get; }
      public Vector2 Direction { get; }

      public override Vector2 Size { get; init; }
      public override Vector2 Position { get; set; }
      public override float Velocity { get; set; }

      public Projectile(Vector2 position, Vector2 direction, int damage)
      {
         Size = new Vector2(32.0f / 1920, 32.0f / 1080);

         Damage = damage;
         Position = position;
         Direction = direction;
         Velocity = 0.00125f;


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

      public void Draw()
      {
         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertices.Length);
      }

      public void Die()
      {
         _vao.Dispose();
         _vbo.Dispose();
      }
   }

   public sealed class Obstacle : Entity
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public override Vector2 Size { get; init; }
      public override Vector2 Position { get; set; }
      public override float Velocity { get; set; }

      public Obstacle(Vector2 position, Vector2 size)
      {
         Position = position;
         Size = size;
         Velocity = 0.0f;

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

      public void Draw()
      {
         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertices.Length);
      }
   }

   public sealed class Bonus : Entity
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public override Vector2 Size { get; init; }
      public override Vector2 Position { get; set; }
      public override float Velocity { get; set; }

      public Bonus(Vector2 position, Vector2 size)
      {
         Position = position;
         Size = size;
         Velocity = 0.0f;

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

      public void Draw()
      {
         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertices.Length);
      }
   }


}