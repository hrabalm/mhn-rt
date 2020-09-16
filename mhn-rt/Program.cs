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

    static class TestScenes
    {
        public static SceneNode Ciri()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("ciri.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(0.02) * Matrix4d.RotateY(Math.PI) * Matrix4d.CreateTranslation(0.0, -8.8, -9.25);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Sylvanas()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("Sylvanas.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.70) * Matrix4d.RotateY(0.0) * Matrix4d.CreateTranslation(0.0, -1.45, -2);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Square()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("square.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(2.0) * Matrix4d.RotateY(0.0) * Matrix4d.CreateTranslation(0.0, -1.5, -2);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Teapot()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("teapot.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.0) * Matrix4d.RotateY(0) * Matrix4d.CreateTranslation(0.0, 0.0, -4.0);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Bunny()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("bunny.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.60) * Matrix4d.RotateY(0) * Matrix4d.CreateTranslation(0.30,-1.0, -2.0);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Buddha()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("buddha.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(0.75) * Matrix4d.RotateY(0) * Matrix4d.CreateTranslation(0.0, -3.0, -5.0);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Dragon()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("dragon.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.5) * Matrix4d.RotateY(MathHelper.PiOver2+0.25) * Matrix4d.CreateTranslation(0.0, 0.0, -1.0);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Oak()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("white_oak.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.5) * Matrix4d.RotateY(MathHelper.PiOver2 + 0.25) * Matrix4d.CreateTranslation(0.0, 0.0, -1.0);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Cat()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("cat.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(2.35) * Matrix4d.RotateX(MathHelper.PiOver2) * Matrix4d.RotateZ(MathHelper.Pi) * Matrix4d.RotateY(1.75*MathHelper.Pi) * Matrix4d.CreateTranslation(0.4, -0.75, -1.25);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode Erato()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("erato-1.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(100.0) * Matrix4d.CreateTranslation(0.0, 13000.0, -5);
            scene.AddChild(t);

            return scene;
        }

        public static SceneNode TestScene1()
        {
            SceneNode scene = new SceneNode();

            var t = ObjLoader.LoadObjFile("cat.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(3.0) * Matrix4d.RotateX(MathHelper.PiOver2) * Matrix4d.RotateZ(MathHelper.Pi) * Matrix4d.RotateY(1.75 * MathHelper.Pi) * Matrix4d.CreateTranslation(0.4, -1.51, -2.5);
            scene.AddChild(t);

            var s = new SceneNode();
            s.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.0) * Matrix4d.CreateTranslation(-3.55, 0.75, -6.0); ;
            var o = new Sphere(Vector3d.Zero, 3.0, new PhongMaterial());
            (o.Material as PhongMaterial).KTransparency = 0.0;
            (o.Material as PhongMaterial).Texture = new CheckerTexture3D();

            s.AddChild(o);
            scene.AddChild(s);

            s = new SceneNode();
            s.ToParent = Matrix4d.Identity * Matrix4d.Scale(0.25) * Matrix4d.CreateTranslation(-0.45, 0.0, -1.0);
            var pm = new PhongMaterial();
            pm.KTransparency = 1.0;
            pm.N = 1.5;
            s.AddChild(new Sphere(Vector3d.Zero, 1.0, pm));
            scene.AddChild(s);

            t = ObjLoader.LoadObjFile("Sylvanas.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.50) * Matrix4d.RotateY(0.0) * Matrix4d.CreateTranslation(2.5, -1.525, -2);
            scene.AddChild(t);

            scene.AddChild(new Sphere(new Vector3d(0, -1001.5, -1), 1000, o.Material));

            var m2 = new PhongMaterial();
            m2.Color = new Vector3(0.9f, 0.0f, 0.0f);
            m2.Ks = 0.3;
            m2.Ka = 0.1;
            m2.Kd = 0.6;
            m2.KTransparency = 0.0;
            scene.AddChild(new Sphere(new Vector3d(-1.5, 0, -1.0), 0.25, m2));

            return scene;
        }
    }


    class Program
    {
        static Random random = new Random(42);

        static void Main(string[] args)
        {
            // test
            //var m = Matrix4d.CreateTranslation(0, 0, 5);
            //var v = Vector3d.Transform(new Vector3d(0, 0, 0), m);
            SceneNode root = new SceneNode();
            Scene scene = new Scene()
            {
                RootIntersectable = root,
                //Camera = new Camera(),
                BackgroundColor = new Vector3(0xA9 / 255.0f, 0xA9 / 255.0f, 0xA9 / 255.0f),
                //BackgroundColor = new Vector3(230 / 255.0f, 0 / 255.0f, 0 / 255.0f),
            };
            

            //IMaterial m1 = new LambertianMaterial(new Vector3(0.8f, 0.8f, 0.0f));
            //IMaterial m2 = new MetalMaterial(new Vector3(0.8f, 0.8f, 0.8f));


            //root.objects.Add(new Sphere(new Vector3d(0, -2, 0), 0.5, m1));
            //root.objects.Add(new Sphere(new Vector3d(0, -101.5, -1), 100, m2));
            //root.objects.Add(new Sphere(new Vector3d(-1.2, 0, -1.5), 0.5, m2));


            //var TM = new TriangleManager();
            //TM.AddTriangle(new Vector3(0, 0, -1.0f), new Vector3(1.0f, 1.0f, -1.0f), new Vector3(0, 1.0f, -1.0f));
            //TM.BuildBVH();
            //root.objects.Add(TM);

            //root.AddChild(TestScenes.Oak());
            //root.AddChild(TestScenes.Bunny());
            //root.AddChild(TestScenes.Sylvanas());
            //root.AddChild(TestScenes.Square());
            root.AddChild(TestScenes.TestScene1());
            //root.AddChild(TestScenes.Ciri());
            //root.AddChild(TestScenes.Cat());
            //root.AddChild(TestScenes.Teapot());
            //root.AddChild(TestScenes.Dragon());
            //root.AddChild(TestScenes.Buddha());
            //root.AddChild(TestScenes.Erato());

            //root.objects.Add(new Sphere(new Vector3d(-2.5, 0, -9.75), 0.45, m1));
            //root.objects.Add(new Sphere(new Vector3d(-8, 0, -12.75), 3, m1));
            //root.objects.Add(new Sphere(new Vector3d(-0, 0, -75), 60, m1));

            //ITexture test_texture = new BitmapTexture("t_01__cat_cs_d01.png"); // TODO:Remove
            //ITexture test_texture = new BitmapTexture("ab.jpg"); // TODO:Remove
            ITexture test_texture = new BitmapTexture("Sylvanas_ladysylvanaswindrunner_01.png"); // TODO:Remove
            root.Material = new PhongMaterial();
            (root.Material as PhongMaterial).Texture = test_texture;
            (root.Material as PhongMaterial).Color = new Vector3(1, 1, 1);
            (root.Material as PhongMaterial).Kd = 0.75;
            (root.Material as PhongMaterial).Ka = 0.25;
            (root.Material as PhongMaterial).Ks = 0.00;

            Camera camera = new Camera(new Vector3d(0.0, 0.0, 0.5), new Vector3d(0.0, 0.0, -1.0), new Vector3d(0.0, 1.0, 0.0));
            scene.Camera = camera;
            //scene.LightSources.Add(new PointLight { Position = new Vector3d(-3.0, 0, -7), Intensity=20});
            scene.LightSources.Add(new DirectionalLight { Direction = new Vector3d(0.0, -0.5, -1.0).Normalized(), Intensity=1.0});

            //IRayTracer raytracer = new NormalRayTracer();
            IRayTracer raytracer = new SimpleRayTracer();
            var renderer = new Renderer(raytracer);
            var output = renderer.Render(scene, 1280, 720, 3);
            //var output = renderer.Render(scene, 1920, 1080, 2);
            //var output = renderer.Render(scene, 2*1920, 2*1080, 15);


            output.Save("test2.png");
            Process.Start("test2.png");

            Console.Write(Statistics.Print());

            //Console.WriteLine(stopwatch.Elapsed.TotalSeconds + "s");
            
        }
    }
}
