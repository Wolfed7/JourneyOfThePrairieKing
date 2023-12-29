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

      public abstract float MoveSpeed { get; set; }

      public abstract int HitPoints { get; }

      public abstract void GotDamage(int damage);
      protected abstract void Die();

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

      private int _hitPoints;
      private int _damage;
      private int _damageSpeed;
      public override Vector2 Size { get; init; }
      public override Vector2 Position { get; set; }
      public override float MoveSpeed { get; set; }
      public long ReloadTime { get; set; }

      public override int HitPoints => _hitPoints;

      public Character()
      {
         Size = new Vector2(64.0f / 1920, 64.0f / 1080);
         Position = new Vector2(0, 0);
         MoveSpeed = 0.15f;
         ReloadTime = 200;

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

         _hitPoints = 1;
         _damage = 1;
         _damageSpeed = 1;
      }

      public void Draw()
      {
         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertices.Length);
      }

      public override void GotDamage(int damage)
      {
         _hitPoints -= damage;

         if (_hitPoints < 0)
         {

         }
      }


      protected override void Die()
      {

      }
   }

   public sealed class Enemy : Entity
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      private int _hitPoints;
      private int _damage;
      private int _damageSpeed;
      public override Vector2 Size { get; init; }
      public override Vector2 Position { get; set; }
      public override float MoveSpeed { get; set; }

      public override int HitPoints => _hitPoints;

      public Enemy()
      {
         Size = new Vector2(64.0f / 1920, 64.0f / 1080);
         Position = new Vector2(0, 0);
         MoveSpeed = 0.03f;


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

         _hitPoints = 1;
         _damage = 1;
         _damageSpeed = 1;
      }

      public void Draw()
      {
         _vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, _vertices.Length);
      }

      public override void GotDamage(int damage)
      {
         _hitPoints -= damage;

         if (_hitPoints < 0)
         {

         }
      }


      protected override void Die()
      {

      }
   }

   public sealed class Projectile : Entity
   {
      private VertexPositionTexture[] _vertices;
      private VertexBufferObject _vbo;
      private VertexArrayObject _vao;

      public int Damage { get; }
      public Vector2 Direction { get; }

      public override Vector2 Size { get; init; }
      public override Vector2 Position { get; set; }
      public override float MoveSpeed { get; set; }

      public override int HitPoints => 0;

      public Projectile(Vector2 position, Vector2 direction, int damage)
      {
         Size = new Vector2(16.0f / 1920, 16.0f / 1080);

         Damage = damage;
         Position = position;
         Direction = direction;
         MoveSpeed = 0.005f;


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

      public override void GotDamage(int damage)
      {

      }

      protected override void Die()
      {

      }
   }



   public static class Collision
   {
      public static bool Compute(Entity entity1, Entity entity2)
      {
         bool isCollisionHappened = false;

         var Ent1UpLeft = new Vector2(entity1.Position.X - entity1.Size.X / 2.0f, entity1.Position.Y + entity1.Size.Y / 2.0f);
         var Ent2UpLeft = new Vector2(entity2.Position.X - entity2.Size.X / 2.0f, entity2.Position.Y + entity2.Size.Y / 2.0f);

         double x1 = Math.Max(Ent1UpLeft.X, Ent2UpLeft.X);
         double x2 = Math.Min(Ent1UpLeft.X + entity1.Size.X, Ent2UpLeft.X + entity2.Size.X);
         double y1 = Math.Max(Ent1UpLeft.Y, Ent2UpLeft.Y);
         double y2 = Math.Min(Ent1UpLeft.Y + entity1.Size.Y, Ent2UpLeft.Y + entity2.Size.Y);

         if (x2 >= x1 && y2 >= y1)
            isCollisionHappened = true;

         return isCollisionHappened;
      }
   }

}