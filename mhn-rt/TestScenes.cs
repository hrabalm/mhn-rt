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
                Background = GradientBackground.BasicSky,
            };

            SceneNode rootNode = scene.RootIntersectable;

            var t = ObjLoader.LoadObjFile("cat.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(4.0) * Matrix4d.RotateX(MathHelper.PiOver2) * Matrix4d.RotateZ(MathHelper.Pi) * Matrix4d.RotateY(1.75 * MathHelper.Pi) * Matrix4d.CreateTranslation(-0.4, -1.51, -2);
            rootNode.AddChild(t);

            t = ObjLoader.LoadObjFile("Sylvanas.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(2.0) * Matrix4d.RotateY(0.0) * Matrix4d.CreateTranslation(2.5, -1.525, -2);
            rootNode.AddChild(t);

            PhongMaterial npm = new PhongMaterial() { Kd = 0.8, Ks = 0.1, Ka = 0.1 };
            npm.Texture = new CheckerTexture3D();
            (npm.Texture as CheckerTexture3D).Color1 = new Vector3d(0.0, 1.0, 1.0);
            (npm.Texture as CheckerTexture3D).Color2 = new Vector3d(0.0, 0.5, 1.0);
            rootNode.AddChild(new Sphere(new Vector3d(0, -1001.5, -1), 1000, npm));

            scene.LightSources.Add(new DirectionalLight { Direction = new Vector3d(0.0, -0.5, -1.0).Normalized(), Intensity = 1.5 });
            scene.Camera = new Camera(new Vector3d(0.0, 0.0, 0.5), new Vector3d(0.0, 0.0, -1.0), new Vector3d(0.0, 1.0, 0.0));

            return scene;
        }

        public static Scene TestScene2()
        {
            Scene scene = new Scene()
            {
                Background = GradientBackground.BasicSky,
            };
            SceneNode rootNode = scene.RootIntersectable;

            PhongMaterial npm = new PhongMaterial() { Kd = 0.5, Ks = 0.3, Ka = 0.2 };
            PhongMaterial glass = new PhongMaterial() { Kd = 0.1, Ks = 0.8, Ka = 0.1, KTransparency = 0.95, N=1.1, Color = new Vector3(0.93f, 1.0f, 0.93f)};
            npm.Texture = new CheckerTexture3D();
            (npm.Texture as CheckerTexture3D).Color1 = new Vector3d(0.1, 0.3, 0.1);
            (npm.Texture as CheckerTexture3D).Color2 = new Vector3d(0.3, 0.5, 0.3);
            rootNode.AddChild(new Sphere(new Vector3d(0, -1000.5, 0), 1000, npm));

            rootNode.AddChild(new Sphere(new Vector3d(0.25, 0.0, 0), 0.5, glass));
            rootNode.AddChild(new Sphere(new Vector3d(1.55, -0.10, 0), 0.40, npm));

            var t = ObjLoader.LoadObjFile("teapot.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(0.35) * Matrix4d.RotateY(0.1) * Matrix4d.CreateTranslation(-1.5, 0.0, -0.75);
            rootNode.AddChild(t);

            PhongMaterial tpt = new PhongMaterial() { Kd = 0.1, Ks = 0.8, Ka = 0.1, Color = new Vector3(0.0f, 0.5f, 1.0f) };
            t.Material = tpt;

            scene.LightSources.Add(new PointLight { Position = new Vector3d(0.5, 0.5, 1.0), Intensity = 3.0 });
            scene.LightSources.Add(new DirectionalLight { Direction = new Vector3d(0.1, -0.5, -0.5), Intensity = 0.1 });
            scene.Camera = new Camera(new Vector3d(-0.1, 0.125, 1.2), new Vector3d(0.0, 0.0, 0.0), new Vector3d(0.0, 1.0, 0.0));

            return scene;
        }

        public static Scene TestScene3()
        {
            Scene scene = new Scene()
            {
                Background = GradientBackground.BasicSky,
            };
            SceneNode rootNode = scene.RootIntersectable;

            PhongMaterial npm = new PhongMaterial() { Kd = 0.81, Ks = 0.09, Ka = 0.1 };
            npm.Texture = new CheckerTexture3D();
            (npm.Texture as CheckerTexture3D).Color1 = new Vector3d(0.1, 0.3, 0.1);
            (npm.Texture as CheckerTexture3D).Color2 = new Vector3d(0.3, 0.5, 0.3);
            rootNode.AddChild(new Sphere(new Vector3d(0, -1001, -1), 1000, npm));

            var t = ObjLoader.LoadObjFile("bunny.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.0) * Matrix4d.RotateY(0) * Matrix4d.CreateTranslation(-1.75, -1.1, -0.85);
            rootNode.AddChild(t);

            t = ObjLoader.LoadObjFile("bunny.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.0) * Matrix4d.RotateY(0) * Matrix4d.CreateTranslation(0, -1.1, -0.85);
            t.Material = new PhongMaterial();
            (t.Material as PhongMaterial).Texture = new CheckerTexture3D();
            (t.objects[0] as TriangleManager).GetTrianglesMesh(0).Material = t.Material;
            rootNode.AddChild(t);

            t = new SceneNode();
            var pm = new PhongMaterial() { Kd = 0.10, Ks = 0.0, Ka = 0.0, H = 500};
            pm.NormalMap = new CheckerTexture();
            t.AddChild(new Sphere(new Vector3d(2.0, -0.5, -1.0), 0.5, pm));
            rootNode.AddChild(t);

            pm = new PhongMaterial() { Kd = 0.10, Ks = 0.0, Ka = 0.0, H = 500 };
            pm.Texture = new BitmapTexture("lorem.png");
            t.AddChild(new Sphere(new Vector3d(2.0, 1.5, -1.0), 0.5, pm));
            rootNode.AddChild(t);

            scene.LightSources.Add(new DirectionalLight { Direction = new Vector3d(0.1, -0.5, -0.75), Intensity = 1.0 });
            scene.Camera = new Camera(new Vector3d(0.0, 0.0, 1.0), new Vector3d(0.0, 0.0, -0.8), new Vector3d(0.0, 1.0, 0.0));

            return scene;
        }

        public static Scene CornellBox()
        {
            Scene scene = new Scene()
            {
                Background = GradientBackground.BasicSky,
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
            SceneRegistry.Scenes.Add("Test Scene 3", TestScenes.TestScene3);
            SceneRegistry.Scenes.Add("zCornell Box", TestScenes.CornellBox);
        }
    }
}