using OpenTK;
using OpenTK.Graphics.ES11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mhn_rt
{
    /// <summary>
    /// Static helper methods
    /// </summary>
    static class Help
    {
        static Random random = new Random(42); // TODO: Unify Random numbers
        public static Vector3d Reflect(Vector3d vector, Vector3d normal) // Change
        {
            normal = normal.Normalized();
            return vector - 2 * Vector3d.Dot(vector, normal) * normal;
        }

        public static Vector3d Refract(Vector3d vector, Vector3d normal, double n0, double n1)
        {
            if (-0.001 < n0 - n1 && n0 - n1 < 0.001)
                return vector;

            double cos_alpha = Vector3d.Dot(-vector.Normalized(), normal.Normalized());
            Vector3d r1 = (n0 / n1) * (vector * cos_alpha * normal);
            Vector3d r2 = -Math.Sqrt(Math.Abs(1.0 - r1.LengthSquared)) * normal;

            return r1 + r2;
        }

        /// <summary>
        /// Find Ray-Triangle intersection.
        /// 
        /// Implemented as described in this paper: http://webserver2.tecgraf.puc-rio.br/~mgattass/cg/trbRR/Fast%20MinimumStorage%20RayTriangle%20Intersection.pdf
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="uv"></param>
        /// <returns></returns>
        public static double RayTriangleIntersection(ref Ray ray, ref Vector3 a, ref Vector3 b, ref Vector3 c, out Vector2d uv)
        {// TODO: Needs more testing
            uv = Vector2d.Zero;

            Vector3d edge1 = (Vector3d)b - (Vector3d)a;
            Vector3d edge2 = (Vector3d)c - (Vector3d)a;
            Vector3d pvec = Vector3d.Cross(ray.direction, edge2);
            double det = Vector3d.Dot(edge1, pvec);

            if (det >= -double.Epsilon && det < double.Epsilon)
                return double.NegativeInfinity;
            double inv_det = 1.0 / det;

            Vector3d tvec = ray.origin - (Vector3d)a;
            uv.X = Vector3d.Dot(tvec, pvec) * inv_det;
            if (uv.X < 0.0 || uv.X > 1.0)
                return double.NegativeInfinity; // value much smaller than 0 means there is no intersection

            Vector3d qvec = Vector3d.Cross(tvec, edge1);

            uv.Y = Vector3d.Dot(ray.direction, qvec) * inv_det;
            if (uv.Y < 0.0 || uv.X + uv.Y > 1.0)
                return double.NegativeInfinity;

            double t = Vector3d.Dot(edge2, qvec) * inv_det;

            if (t <= 0.001)
                return double.NegativeInfinity;

            return t;
        }

        public static Vector3d RandomUnitVector()
        {
            double a = random.NextDouble() * MathHelper.TwoPi;
            double z = random.NextDouble() * 2.0 - 1.0;
            double r = Math.Sqrt(1 - z * z);

            return new Vector3d(r*Math.Cos(a), r*Math.Sin(a), z);
        }

        public static bool IsVisible(Vector3d origin, Vector3d target, Vector3d normal, Scene scene)
        {
            Vector3d normalOffset = normal * scene.ShadowBias;
            //Vector3d directionOffset = (target - origin) * scene.ShadowBias;
            Vector3d directionOffset = Vector3d.Zero;
            origin +=  directionOffset + normalOffset;
            var intersections = scene.RootIntersectable.Intersect(new Ray(origin, target - origin));

            if (Intersection.HasIntersection((List<Intersection>)intersections))
                return false;

            return true;
        }

        static void AskForInt(string name, int defaultValue, int minValue, int maxValue, out int result)
        {
            while (true)
            {
                Console.Write($"Specify {name} [{defaultValue}]: ");
                string i = Console.ReadLine();
                if (i.Length == 0)
                {
                    result = defaultValue;
                    break;
                }
                else if (i.Length > 0 && int.TryParse(i, out result) && result >= minValue && result <= maxValue)
                    break;
            }
        }

        public static void GetConfigFromUser(SortedDictionary<string, Func<Scene>> scenes, out int width, out int height, out int sqrtSpp, out Scene scene)
        {
            AskForInt("width", 1280, 1, int.MaxValue, out width);
            AskForInt("height", 720, 1, int.MaxValue, out height);
            AskForInt("square root of samples per pixel (natural number)", 2, 1, int.MaxValue, out sqrtSpp);

            while (true) // scene selection
            {
                Console.WriteLine("Scenes:");
                int i = 1;
                foreach (var x in scenes)
                {
                    Console.WriteLine($"{i++}. {x.Key}");
                }
                Console.Write("Selected scene [1]: ");
                string line = Console.ReadLine();
                int selected;
                if (line.Length == 0)
                {
                    scene = scenes.ElementAt(0).Value();
                    break;
                }
                else if (int.TryParse(line, out selected) && selected > 0 && selected < i)
                {
                    scene = scenes.ElementAt(selected-1).Value();
                    break;
                }
            }
        }

        public static void GetFilenameFromUser(string defaultFilename, out string filename)
        {
            filename = null;
            while (filename == null)
            {
                Console.Write($"Specify output filename [{defaultFilename}]: ");
                filename = Console.ReadLine();

                if (filename.Length == 0)
                    filename = defaultFilename;
            }
        }
    }
}
