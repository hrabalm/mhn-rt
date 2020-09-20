using OpenTK;
using OpenTK.Graphics.ES20;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace mhn_rt
{
    interface IIntersectable
    {
        IList<Intersection> Intersect(Ray ray);
        IMaterial Material { get; set; }
        IIntersectable Parent { get; set; }
    }

    class Intersection
    {
        public Vector3d position;
        public Vector3d normal;
        public double t; // position relative to the direction of the ray
        public Vector3 color;
        public IMaterial material;
        public Vector2 uv;
        public double localAlpha;// = 1.0;

        /// <summary>
        /// Does the ray enter (or exit) the solid object through this intersection
        /// (We expect that normal points outwards from the surface)
        /// </summary>
        public bool Enter { get; set; }

        public Vector3d OffsetPosition(double shadowBias)
        {
            return position + normal * shadowBias;
        }

        public Intersection Transform(Matrix4d m)
        {
            Intersection ret = new Intersection
            {
                position = Vector3d.Transform(position, m),
                normal = Vector3d.TransformNormal(normal, m), // TODO: TransformNormalInverse
                t = t,
                color = color,
                material = material,
                uv = uv,
                localAlpha = localAlpha,
            };

            return ret;
        }

        public bool IsValidIntersection()
        {
            if (t > 0.0001 && t <= 0.9999) // TODO: Check this
                return true;
            return false;
        }

        public bool IsValidIntersection(double minT, double maxT)
        {
            if (t > minT && t <= maxT) // TODO: Check this
                return true;
            return false;
        }

        public static bool HasIntersection(IEnumerable<Intersection> intersections)
        {
            foreach (var i in intersections)
            {
                if (i.IsValidIntersection())
                    return true;
            }

            return false;
        }

        public void ApplyTexture()
        {
            if (material is PhongMaterial)
            {
                PhongMaterial pm = (PhongMaterial)material;

                if (pm.Texture != null)
                {
                    color = (Vector3)pm.Texture.GetColor(uv, position, out localAlpha);
                }
                else
                {
                    color = pm.Color;
                    localAlpha = 1.0;
                }

                if (pm.BumpMap != null)
                {
                    double la;
                    var bc = pm.BumpMap.GetColor(uv, position, out la);
                    Vector3d dn = new Vector3d(bc.X - 0.5, bc.Y - 0.5, bc.Z - 0.5);
                    //normal.Normalize();
                    normal += dn;
                }
            }
        }
    }

    class Sphere : IIntersectable // TODO: Shell mode
    {
        Vector3d center;
        double radius;

        public IMaterial Material { get; set; }
        public IIntersectable Parent { get; set; }

        public Sphere(Vector3d center, double radius, IMaterial material)
        {
            this.center = center;
            this.radius = radius;
            this.Material = material;
        }

        public Vector2d GetUV(Vector3d position)
        {
            var p = (position - center) / radius;

            var phi = Math.Atan2(p.Z, p.X);
            var theta = Math.Asin(p.Y);
            var u = 1 - (phi + MathHelper.Pi) / (2 * MathHelper.Pi);
            var v = (theta + MathHelper.PiOver2) / MathHelper.Pi;

            return new Vector2d(u, v);
        }

        public IList<Intersection> Intersect(Ray ray)
        {
            Vector3d x = ray.origin - center;
            var a = Vector3d.Dot(ray.direction, ray.direction);
            var bh = Vector3d.Dot(x, ray.direction);
            var b = 2.0 * bh;
            var c = Vector3d.Dot(x, x) - radius*radius;
            var d = b * b - 4 * a * c;

            var res = new List<Intersection>();

            if (d > 0)
            {
                var d_root = Math.Sqrt(d);

                var t = (-b - d_root) / (2.0 * a);

                if (t > 0.0001 && !Double.IsInfinity(t)) // TODO: Make it a global constant
                {
                    var pos = ray.PointAt(t);
                    var n = ((pos - center)).Normalized();
                    var cn = 0.5 * (n + new Vector3d(1, 1, 1));
                    var i1 = new Intersection { t = t, normal = n, position = pos, material = Material };

                    i1.uv = (Vector2)GetUV(pos);

                    if (Vector3d.Dot(n, ray.direction.Normalized()) < 0.0)
                        i1.Enter = true;
                    else
                        i1.Enter = false;

                    res.Add(i1);
                }

                t = (-b + d_root) / (2.0 * a);

                if (t > 0.0001 && !Double.IsInfinity(t)) // TODO: Make it a global constant
                {
                    var pos = ray.PointAt(t);
                    var n = ((pos - center)).Normalized();
                    var cn = 0.5 * (n + new Vector3d(1, 1, 1));
                    var i2 = new Intersection { t = t, normal = n, position = pos, material = Material };

                    i2.uv = (Vector2)GetUV(pos);

                    if (Vector3d.Dot(n, ray.direction.Normalized()) < 0.0)
                        i2.Enter = true;
                    else
                        i2.Enter = false;

                    res.Add(i2);
                }
            }

            res.Sort((p1, p2) => p1.t.CompareTo(p2.t));
            return res;
        }
    }
}
