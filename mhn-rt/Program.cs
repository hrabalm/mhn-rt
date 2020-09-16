using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Security;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace mhn_rt
{
    class Scene
    {
        public IIntersectable RootIntersectable { get; set; }
        public IList<ILight> LightSources { get; set; } = new List<ILight>();
        public Camera Camera { get; set; }
        public Vector3 BackgroundColor { get; set; }
        public double ShadowBias { get; set; } = 0.00000001;
    }

    class SceneNode : IIntersectable
    {
        public List<IIntersectable> objects = new List<IIntersectable>();
        public IIntersectable Parent { get; set; }
        public IMaterial Material {
            get
            {
                if (_material != null)
                    return _material;
                else
                    return Parent.Material;
            }
            set => _material = value; }
        protected IMaterial _material;

        protected Matrix4d _toParent;
        protected Matrix4d _toObject;

        public Matrix4d ToParent {
            get => _toParent;
            set
            {
                _toParent = value;
                _toObject = _toParent.Inverted();
            }
        }
        public Matrix4d ToObject {
            get => _toObject;
            set
            {
                _toObject = value;
                _toParent = _toObject.Inverted();
            }
        }

        public SceneNode()
        {
            ToParent = Matrix4d.Identity;
        }

        public void AddChild(IIntersectable child)
        {
            child.Parent = this;
            objects.Add(child);
        }

        public IList<Intersection> Intersect(Ray ray)
        {
            var res = new List<Intersection>();

            var tRay = new Ray(Vector3d.Transform(ray.origin, ToObject), Vector3d.TransformVector(ray.direction, ToObject));

            foreach (var o in objects)
            {
                // maybe not all intersections have to be transformed
                var intersections = o.Intersect(tRay);
                for (int i = 0; i < intersections.Count; i++)
                {
                    intersections[i] = intersections[i].Transform(ToParent);
                }
                res.AddRange(intersections);
            }

            res.Sort((p1, p2) => p1.t.CompareTo(p2.t));

            if (res.Count > 0)
                res[0].normal.Normalize();

            return res;
        }
    }

    class Program
    {
        static Random random = new Random(42);

        static void Main(string[] args)
        {
            SceneNode root = new SceneNode();
            Scene scene = new Scene()
            {
                RootIntersectable = root,
                BackgroundColor = new Vector3(0xA9 / 255.0f, 0xA9 / 255.0f, 0xA9 / 255.0f),
            };

            scene = TestScenes.TestScene1();

            int width = 1280;
            int height = 720;
            int sqrtSpp = 1;
            string filename = "test2.png";

            IRayTracer raytracer = new SimpleRayTracer();
            //IRayTracer raytracer = new NormalRayTracer();
            var renderer = new Renderer(raytracer);
            var output = renderer.Render(scene, width, height, sqrtSpp);

            output.Save(filename);
            Process.Start(filename);

            Console.Write(Statistics.Print()); 
        }
    }
}
