using System;
using System.Security.Principal;
using OpenTK;

namespace mhn_rt
{
    static class TestScenes
    {
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

            var t = ObjLoader.LoadObjFile("testModels/teapot.obj");
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

            var t = ObjLoader.LoadObjFile("testModels/bunny.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.0) * Matrix4d.RotateY(0) * Matrix4d.CreateTranslation(-1.65, -1.1, -0.85);
            rootNode.AddChild(t);

            t = ObjLoader.LoadObjFile("testModels/bunny.obj");
            t.ToParent = Matrix4d.Identity * Matrix4d.Scale(1.0) * Matrix4d.RotateY(0) * Matrix4d.CreateTranslation(0.3, -1.1, -0.85);
            t.Material = new PhongMaterial();
            (t.Material as PhongMaterial).Texture = new CheckerTexture3D();
            (t.objects[0] as TriangleManager).GetTrianglesMesh(0).Material = t.Material;
            rootNode.AddChild(t);

            t = new SceneNode();
            var pm = new PhongMaterial() { Kd = 0.10, Ks = 0.0, Ka = 0.0, H = 500};
            pm.Texture = new CheckerTexture();
            t.AddChild(new Sphere(new Vector3d(2.3, -0.5, -1.0), 0.5, pm));
            rootNode.AddChild(t);

            pm = new PhongMaterial() { Kd = 0.10, Ks = 0.0, Ka = 0.0, H = 500 };
            pm.Texture = new BitmapTexture("testModels/lorem.png");
            t.AddChild(new Sphere(new Vector3d(2.3, 1.5, -1.0), 0.5, pm));
            rootNode.AddChild(t);

            scene.LightSources.Add(new DirectionalLight { Direction = new Vector3d(0.4, -0.5, -0.75), Intensity = 1.0 });
            scene.Camera = new Camera(new Vector3d(0.5, 0.0, 1.5), new Vector3d(0.0, 0.0, -0.8), new Vector3d(0.0, 1.0, 0.0));

            return scene;
        }

        public static void RegisterScenes()
        {
            SceneRegistry.Scenes.Add("Test Scene (reflections, refractions)", TestScenes.TestScene2);
            SceneRegistry.Scenes.Add("Test Scene (textures)", TestScenes.TestScene3);
        }
    }
}