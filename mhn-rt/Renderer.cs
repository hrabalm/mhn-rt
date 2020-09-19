using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace mhn_rt
{
    class Renderer
    {
        public bool Multithreading { get; set; } = true;
        public bool Tracing { get; set; } = true;
        public int MaxDepth { get; set; } = 100;
        Stopwatch stopwatch = new Stopwatch();
        IRayTracer raytracer;

        public Renderer(IRayTracer raytracer)
        {
            this.raytracer = raytracer;
        }

        /// <summary>
        /// Only samples 2^x are accepted
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="spp"></param>
        /// <returns></returns>
        public Bitmap Render(Scene scene, int width, int height, int spp)
        {
            Bitmap bitmap = new Bitmap(width, height);
            Random random = new Random(42);

            stopwatch.Start();

            // we want to access bitmap in parallel and faster (Bitmap.{Get,Set}Pixel is slow)
            // so we copy the bitmap to array
            // we could start with initialized array here instead,
            // but this makes it slightly easier to change pixel format later
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bitmap.PixelFormat);
            var bitDepth = Bitmap.GetPixelFormatSize(bitmap.PixelFormat);
            var buffer = new byte[data.Width * data.Height * bitDepth / 8];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            int processedRown = 0;

            Action<int, ParallelLoopState> DrawRow = (y, state) =>
            {
                for (int x = 0; x < width; x++)
                {
                    // jitter 
                    // 'pixel corners'
                    //double bottomLeft;
                    //double topRight;
                    // getPixelColor
                    double dX = 1.0 / width;
                    double dY = 1.0 / height;
                    double minX = x * dX;
                    double minY = (height - y) * dY; // axis y has opposite index order in picture and in scene

                    Vector3d pixelColor = Vector3d.Zero;

                    List<Ray> rays = new List<Ray>(spp * spp);
                    List<Vector3d> colors = new List<Vector3d>();
                    for (int i = 0; i < spp; i++)
                    {
                        for (int j = 0; j < spp; j++)
                        {
                            double rX = minX + (i + random.NextDouble()) * (dX / (spp)); // TODO: Check constants
                            double rY = minY + (j + random.NextDouble()) * (dY / (spp));

                            rays.Add(scene.Camera.GetRay(rX, rY));
                            Interlocked.Increment(ref Statistics.PrimaryRays);
                        }
                    }

                    foreach (var ray in rays)
                        colors.Add(raytracer.GetRayColor(ray, scene, MaxDepth, 1.0f));

                    if (colors.Count > 0)
                    {
                        foreach (var c in colors)
                        {
                            pixelColor += c;
                        }

                        pixelColor /= colors.Count;
                        // gamma correction stub - gamma=2.0
                        //pixelColor = new Vector3((float)Math.Sqrt(pixelColor.X), (float)Math.Sqrt(pixelColor.Y), (float)Math.Sqrt(pixelColor.Z));
                    }

                    pixelColor = 255.0f * pixelColor;
                    pixelColor = new Vector3d(pixelColor.X > 255.0f ? 255.0f : pixelColor.X, pixelColor.Y > 255.0f ? 255.0f : pixelColor.Y, pixelColor.Z > 255.0f ? 255.0f : pixelColor.Z);
                    //bitmap.SetPixel(x, y, Color.FromArgb((int)pixelColor.X, (int)pixelColor.Y, (int)pixelColor.Z));
                    int offset = ((y * width) + x) * bitDepth / 8;
                    buffer[offset + 2] = (byte)pixelColor.X; // red
                    buffer[offset + 1] = (byte)pixelColor.Y; // green
                    buffer[offset + 0] = (byte)pixelColor.Z; // blue
                    buffer[offset + 3] = 255; // alpha

                }
                Interlocked.Increment(ref processedRown);

                if (Tracing && y % 10 == 0)
                    Console.WriteLine($"{((float)100 * processedRown / (height - 1)).ToString("F1")}% done.");
            };

            if (Multithreading)
                Parallel.For(0, height, DrawRow);
            else
            {
                for (int y = 0; y < height; y++)
                    DrawRow(y, null);
            }

            Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            bitmap.UnlockBits(data);

            Console.WriteLine(stopwatch.Elapsed.TotalSeconds + "s");

            return bitmap;
        }
    }
}
