using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace mhn_rt
{
    interface ITexture
    {
        Vector3d GetColor(Vector2 uv, Vector3d point, out double alpha);
    }

    class CheckerTexture3D : ITexture
    {
        public Vector3d Color1 { get; set; } = new Vector3d(1.0, 0.0, 0.0);
        public Vector3d Color2 { get; set; } = new Vector3d(0.0, 1.0, 0.0);
        public double Frequency { get => mult*MathHelper.TwoPi; set => mult = value/MathHelper.TwoPi; }
        public double Period { get => 1 / Frequency; set => Frequency = (1 / value); }
        public double Alpha { get; set; } = 1.0;

        double mult = 25;
        //double period = 0.01;

        public Vector3d GetColor(Vector2 uv, Vector3d point, out double alpha)
        {
            alpha = Alpha;
            var s = Math.Sin(mult * point.X) * Math.Sin(mult * point.Y) * Math.Sin(mult * point.Z);

            if (s < 0)
                return Color1;
            else
                return Color2;
        }
    }

    class CheckerTexture : ITexture
    {
        public Vector3d Color1 { get; set; } = new Vector3d(1.0, 0.0, 0.0);
        public Vector3d Color2 { get; set; } = new Vector3d(0.0, 1.0, 0.0);
        public double Frequency { get => mult * MathHelper.TwoPi; set => mult = value / MathHelper.TwoPi; }
        public double Period { get => 1 / Frequency; set => Frequency = (1 / value); }
        public double Alpha { get; set; } = 1.0;

        double mult = 25;
        //double period = 0.01;

        public Vector3d GetColor(Vector2 uv, Vector3d point, out double alpha)
        {
            alpha = Alpha;
            var s = Math.Sin(mult * uv.X) * Math.Sin(mult * uv.Y);

            if (s < 0)
                return Color1;
            else
                return Color2;
        }
    }

    class BitmapTexture : ITexture
    {
        byte[] img2;

        int Width;
        int Height;

        public BitmapTexture(string filename)
        {
            // Convert original texture to ARGB
            Bitmap img = new Bitmap(filename);
            Bitmap temp = new Bitmap(img.Width, img.Height, PixelFormat.Format32bppArgb);
            
            using (Graphics g = Graphics.FromImage(temp))
            {
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
            }

            // copy image data into an array (Bitmap.GetPixel doesn't support simultaneous access from multiple threads)
            var data = temp.LockBits(new Rectangle(0, 0, temp.Width, temp.Height), ImageLockMode.ReadOnly, temp.PixelFormat);
            img2 = new byte[temp.Width * temp.Height * 4];
            Marshal.Copy(data.Scan0, img2, 0, img2.Length);
            temp.UnlockBits(data);
            Width = temp.Width;
            Height = temp.Height;
        }
        public Vector3d GetColor(Vector2 uv, Vector3d point, out double alpha)
        {
            return GetColorInterp(uv, out alpha);
        }

        public Color GetPixel(int x, int y)
        {
            int offset = (y * Width + x) * 4;
            byte r = img2[offset + 2];
            byte g = img2[offset + 1];
            byte b = img2[offset + 0];
            byte a = img2[offset + 3];

            return Color.FromArgb(a, r, g, b);
        }

        public Vector3d GetColorInterp(Vector2 uv, out double alpha)
        {
            bool bilinear = true;
            double R, G, B, A;

            double x, y;
            x = uv.X;
            y = (1 - uv.Y);
            //y = img.Height * uv.Y;
            /*
            if (x < 0 && x > -1.01) // mirror
                x *= -1;
                //x += 1;
            if (y < 0 && y > -1.01)
                y *= -1;
            */
            while (x < 0)
                x += 1.0;
            while (y < 0)
                y += 1.0;
            while (x > 1.0)
                x -= 1.0;
            while (y > 1.0)
                y -= 1.0;

            x = (Width - 1) * x;
            y = (Height - 1) * y;

            int x1, x2, y1, y2;
            x1 = (int)Math.Floor(x);
            x2 = (int)Math.Ceiling(x);
            y1 = (int)Math.Floor(y);
            y2 = (int)Math.Ceiling(y);

            double dx = x - x1;
            double dy = y - y1;


            if (bilinear && x1 >= 0 && x2 < Width && y1 >= 0 && y2 < Height) // enough pixels for bilinear interp.
            {
                var c11 = GetPixel(x1, y1);
                var c12 = GetPixel(x1, y2);
                var c21 = GetPixel(x2, y1);
                var c22 = GetPixel(x2, y2);


                //if (c11.A != 255)
                //    Co

                R = ((1 - dx) * (1 - dy) * c11.R + (1 - dx) * dy * c12.R + dx * (1 - dy) * c21.R + dx * dy * c22.R) / 255.0;
                G = ((1 - dx) * (1 - dy) * c11.G + (1 - dx) * dy * c12.G + dx * (1 - dy) * c21.G + dx * dy * c22.G) / 255.0;
                B = ((1 - dx) * (1 - dy) * c11.B + (1 - dx) * dy * c12.B + dx * (1 - dy) * c21.B + dx * dy * c22.B) / 255.0;
                A = ((1 - dx) * (1 - dy) * c11.A + (1 - dx) * dy * c12.A + dx * (1 - dy) * c21.A + dx * dy * c22.A) / 255.0;
            }
            else // otherwise we just select nearest neighbour
            {
                var t = GetPixel((int)Math.Round(x), (int)Math.Round(y));

                R = t.R / 255.0;
                G = t.G / 255.0;
                B = t.B / 255.0;
                A = t.A / 255.0;
            }

            alpha = A;

            return new Vector3d(R, G, B);
        }
    }
}