using System;
using OpenTK.Graphics.OpenGL4;

namespace JourneyOfThePrairieKing
{
   public sealed class VertexBufferObject : IDisposable
   {
      private bool _disposed;

      public int Handle { get; private set; }
      public BufferUsageHint Hint { get; }

      public VertexBufferObject(VertexPositionTexture[] vertices, BufferUsageHint hint)
      {
         _disposed = false;
         Hint = hint;

         Handle = GL.GenBuffer();
         Bind();
         GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * VertexPositionTexture.VertexInfo.SizeInBytes, vertices, Hint);
         GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
      }

      public void Bind()
      {
         GL.BindBuffer(BufferTarget.ArrayBuffer, Handle);
      }

      public void Update(VertexPositionTexture[] vertices)
      {
         Bind();
         GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * VertexPositionTexture.VertexInfo.SizeInBytes, vertices, Hint);
         GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
      }

      public void Dispose()
      {
         if (_disposed)
            return;

         GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
         GL.DeleteBuffer(Handle);

         _disposed = true;
         GC.SuppressFinalize(this);
      }
   }
}