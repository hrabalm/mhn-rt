using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    class Program
    {
        static void Main(string[] args)
        {
            Scene scene;

            TestScenes.RegisterScenes();

            int width;
            int height;
            int sqrtSpp;
            string filename;

            Help.GetConfigFromUser(SceneRegistry.Scenes, out width, out height, out sqrtSpp, out scene);
            Help.GetFilenameFromUser("out.png", out filename);

            IRayTracer raytracer = new SimpleRayTracer();
            //IRayTracer raytracer = new NormalRayTracer(); // visualizes normals by mapping them as RGB colors
            var renderer = new Renderer(raytracer);
            var output = renderer.Render(scene, width, height, sqrtSpp);

            output.Save(filename);
            Process.Start(filename);

            Console.Write(Statistics.Print()); 
        }
    }
}
