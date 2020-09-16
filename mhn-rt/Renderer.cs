using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mhn_rt
{
    class Renderer
    {
        public bool Tracing { get; set; } = true;
        public int MaxDepth { get; set; } = 10;
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


            for (int x = 0; x < width; x++)
            {
                if (Tracing && x % 10 == 0)
                    Console.WriteLine($"{((float)100 * x / (width - 1)).ToString("F1")}% done.");
                for (int y = 0; y < height; y++)
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
                            Statistics.PrimaryRays++;
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
                    bitmap.SetPixel(x, y, Color.FromArgb((int)pixelColor.X, (int)pixelColor.Y, (int)pixelColor.Z));
                }
            }

            Console.WriteLine(stopwatch.Elapsed.TotalSeconds + "s");

            return bitmap;
        }
    }
}
