using System;
using OpenTK.Graphics.OpenGL4;

namespace JourneyOfThePrairieKing;

public sealed class VertexArrayObject : IDisposable
{
   private bool _disposed;

   public int Handle { get; private set; }

   public VertexArrayObject()
   {
      _disposed = false;

      int vertexSizeInBytes = VertexPositionTexture.VertexInfo.SizeInBytes;
      VertexAttribute[] attributes = VertexPositionTexture.VertexInfo.VertexAttributes;

      Handle = GL.GenVertexArray();
      Bind();

      for (int i = 0; i < attributes.Length; i++)
      {
         VertexAttribute attribute = attributes[i];
         GL.VertexAttribPointer(attribute.Index, attribute.ComponentCount, VertexAttribPointerType.Float, true, vertexSizeInBytes, attribute.Offset);
         GL.EnableVertexAttribArray(attribute.Index);
      }

      GL.BindVertexArray(0);
   }

   public void Bind()
   {
      GL.BindVertexArray(Handle);
   }

   public void Dispose()
   {
      if (_disposed is true)
         return;

      GL.BindVertexArray(0);
      GL.DeleteVertexArray(Handle);

      GC.SuppressFinalize(this);
      _disposed = true;
   }
}
