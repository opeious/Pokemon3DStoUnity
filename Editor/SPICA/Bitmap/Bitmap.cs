using System;

namespace P3DS2U.Editor.SPICA.Bitmap
{
    public class Bitmap : IDisposable
    {
        public PixelFormat PixelFormat;
        public int Width, Height;

        public Bitmap (string fileName)
        {
        }

        public Bitmap (Bitmap bmp)
        {
        }

        public Bitmap (Bitmap bmp, int width, int height)
        {
        }

        public Bitmap (int width, int height, PixelFormat format32BppArgb)
        {
        }

        public void Dispose ()
        {
        }
    }
}