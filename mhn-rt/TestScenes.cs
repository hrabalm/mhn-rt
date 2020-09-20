using System;
using System.Security.Principal;
using OpenTK;

namespace mhn_rt
{
    static class TestScenes
    {
        public static Scene TestScene1()
        {
            
            Scene scene = new Scene()
            {
                BackgroundColor = new Vector3(0xA9 / 255.0f, 0xA9 / 255.0f, 0xA9 / 255.0f),
            };

            SceneNode rootNode = scene.RootIntersectable;

            var t = ObjLoader.LoadObjFile("cat.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(3.0) * Matrix4d.RotateX(MathHelper.PiOver2) * Matrix4d.RotateZ(MathHelper.Pi) * Matrix4d.RotateY(1.75 * MathHelper.Pi) * Matrix4d.CreateTranslation(0.4, -1.51, -2.5);
            rootNode.AddChild(t);

            var s = new SceneNode();
            s.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.0) * Matrix4d.CreateTranslation(-3.55, 0.75, -6.0); ;
            var o = new Sphere(Vector3d.Zero, 3.0, new PhongMaterial());
            (o.Material as PhongMaterial).KTransparency = 0.0;
            (o.Material as PhongMaterial).Texture = new CheckerTexture3D();
            ((o.Material as PhongMaterial).Texture as CheckerTexture3D).Period = 0.01;
            ((o.Material as PhongMaterial).Texture as CheckerTexture3D).Color1 = new Vector3d(0.6, 0.2, 0.2);
            ((o.Material as PhongMaterial).Texture as CheckerTexture3D).Color2 = new Vector3d(0.2, 0.6, 0.2);

            s.AddChild(o);
            rootNode.AddChild(s);

            s = new SceneNode();
            s.ToParent = Matrix4d.Identity * Matrix4d.Scale(0.25) * Matrix4d.CreateTranslation(-0.45, 0.0, -1.0);
            var pm = new PhongMaterial();
            pm.KTransparency = 1.0;
            pm.N = 1.5;
            s.AddChild(new Sphere(Vector3d.Zero, 1.0, pm));
            rootNode.AddChild(s);

            t = ObjLoader.LoadObjFile("Sylvanas.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.50) * Matrix4d.RotateY(0.0) * Matrix4d.CreateTranslation(2.5, -1.525, -2);
            rootNode.AddChild(t);

            PhongMaterial npm = new PhongMaterial() { Kd = 0.81, Ks = 0.01, Ka = 0.1 };
            npm.Texture = new CheckerTexture3D();
            (npm.Texture as CheckerTexture3D).Color1 = new Vector3d(0.1, 0.3, 0.1);
            (npm.Texture as CheckerTexture3D).Color2 = new Vector3d(0.3, 0.5, 0.3);
            rootNode.AddChild(new Sphere(new Vector3d(0, -1001.5, -1), 1000, npm));

            var m2 = new PhongMaterial();
            m2.Color = new Vector3(0.9f, 0.0f, 0.0f);
            m2.Ks = 0.3;
            m2.Ka = 0.1;
            m2.Kd = 0.6;
            m2.KTransparency = 0.0;
            rootNode.AddChild(new Sphere(new Vector3d(-1.5, 0, -1.0), 0.25, m2));

            scene.LightSources.Add(new DirectionalLight { Direction = new Vector3d(0.0, -0.5, -1.0).Normalized(), Intensity = 1.0 });
            scene.Camera = new Camera(new Vector3d(0.0, 0.0, 0.5), new Vector3d(0.0, 0.0, -1.0), new Vector3d(0.0, 1.0, 0.0));

            return scene;
        }

        public static Scene TestScene2()
        {
            Scene scene = new Scene()
            {
                BackgroundColor = new Vector3(0xA9 / 255.0f, 0xA9 / 255.0f, 0xA9 / 255.0f),
            };
            SceneNode rootNode = scene.RootIntersectable;

            PhongMaterial npm = new PhongMaterial() { Kd = 0.81, Ks = 0.01, Ka = 0.1 };
            PhongMaterial glass = new PhongMaterial() { Kd = 0.81, Ks = 0.01, Ka = 0.1, KTransparency = 0.95, N=1.1, Color = new Vector3(0.0f, 0.75f, 0.25f)};
            npm.Texture = new CheckerTexture3D();
            (npm.Texture as CheckerTexture3D).Color1 = new Vector3d(0.1, 0.3, 0.1);
            (npm.Texture as CheckerTexture3D).Color2 = new Vector3d(0.3, 0.5, 0.3);
            rootNode.AddChild(new Sphere(new Vector3d(0, -1001, -1), 1000, npm));

            rootNode.AddChild(new Sphere(new Vector3d(0.25, 0.0, 0), 0.35, glass));
            rootNode.AddChild(new Sphere(new Vector3d(1.1, 0, 0), 0.35, npm));

            var t = ObjLoader.LoadObjFile("teapot.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(0.35) * Matrix4d.RotateY(0) * Matrix4d.CreateTranslation(-1.25, 0.0, -0.75);
            rootNode.AddChild(t);

            scene.LightSources.Add(new PointLight { Position = new Vector3d(-0.5, 0.5, 1.0), Intensity = 3.0 });
            scene.LightSources.Add(new DirectionalLight { Direction = new Vector3d(0.1, -0.5, -0.5), Intensity = 0.1 });
            scene.Camera = new Camera(new Vector3d(0.0, 0.5, 1.0), new Vector3d(0.0, 0.0, 0.0), new Vector3d(0.0, 1.0, 0.0));

            return scene;
        }

        public static Scene CornellBox()
        {
            Scene scene = new Scene()
            {
                BackgroundColor = new Vector3(0xA9 / 255.0f, 0xA9 / 255.0f, 0xA9 / 255.0f),
            };
            SceneNode rootNode = scene.RootIntersectable;

            var t = ObjLoader.LoadObjFile("CornellBox-Original.obj");
            t.ToParent = Matrix4d.Scale(0.2);
            rootNode.AddChild(t);

            var la = new PointLight() { Intensity = 0.25, Position = new Vector3d(0.0 * 0.2, 1.9 * 0.2, -0.03 * 0.2) };
            var l1 = new PointLight() { Intensity = 0.125, Position = new Vector3d(-0.24 * 0.2, 1.98 * 0.2, 0.16 * 0.2) };
            var l2 = new PointLight() { Intensity = 0.125, Position = new Vector3d(-0.24 * 0.2, 1.98 * 0.2, -0.22 * 0.2) };
            var l3 = new PointLight() { Intensity = 0.125, Position = new Vector3d(0.23 * 0.2, 1.98 * 0.2, -0.22 * 0.2) };
            var l4 = new PointLight() { Intensity = 0.125, Position = new Vector3d(0.23 * 0.2, 1.98 * 0.2, 0.16 * 0.2) };
            //la = new PointLight() { Intensity = 0.5, Position = new Vector3d(0.0, 0.25, 0.5) };

            scene.LightSources.Add(la);
            //scene.LightSources.Add(l1);
            //scene.LightSources.Add(l2);
            //scene.LightSources.Add(l3);
            //scene.LightSources.Add(l4);
            scene.Camera = new Camera(new Vector3d(0.0, 0.2, 0.4), new Vector3d(0.0, 0.0, -1.0), new Vector3d(0.0, 1.0, 0.0));

            return scene;
        }

        public static void RegisterScenes()
        {
            SceneRegistry.Scenes.Add("Test Scene 1", TestScenes.TestScene1);
            SceneRegistry.Scenes.Add("Test Scene 2", TestScenes.TestScene2);
            SceneRegistry.Scenes.Add("zCornell Box", TestScenes.CornellBox);
        }
    }
}