using OpenTK.Graphics.OpenGL4;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using StbImageSharp;
using OpenTK.Mathematics;

namespace JourneyOfThePrairieKing
{
   // A helper class, much like Shader, meant to simplify loading textures.
   public class Texture
   {
      public readonly int Handle;

      public static Texture LoadFromFile(string path)
      {
         // Generate handle
         int handle = GL.GenTexture();

         // Bind the handle
         GL.ActiveTexture(TextureUnit.Texture0);
         GL.BindTexture(TextureTarget.Texture2D, handle);

         StbImage.stbi_set_flip_vertically_on_load(1);

         // Here we open a stream to the file and pass it to StbImageSharp to load.
         using (Stream stream = File.OpenRead(path))
         {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
         }

         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

         GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

         return new Texture(handle);
      }

      public Texture(int glHandle)
      {
         Handle = glHandle;
      }

      public void Use(TextureUnit unit)
      {
         GL.ActiveTexture(unit);
         GL.BindTexture(TextureTarget.Texture2D, Handle);
      }

      public static void DrawTexturedRectangle(Shader shader, Texture texture, Vector2 position, VertexArrayObject vao)
      {
         var modelMatrix = Matrix4.CreateTranslation(new Vector3(position.X, position.Y, 0.0f));

         shader.SetMatrix4("model", modelMatrix);
         texture.Use(TextureUnit.Texture0);
         shader.SetInt("textureMap", 0);

         vao.Bind();
         GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
      }
   }
}
