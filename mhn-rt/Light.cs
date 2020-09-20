using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mhn_rt
{
    interface ILight
    {
        Vector3 Color { get; set; }
        Vector3d Position { get; set; }
        double Intensity { get; set; }
        Vector3 GetIntensityAndDirection(Intersection i, Scene scene, out Ray direction);
        void GetShadingInfo(Intersection i, Scene scene, out bool isVisible, out Vector3d lightDirection, out Vector3 intensity);
        bool IsVisible(Intersection i, Scene scene);
    }

    class PointLight : ILight
    {
        public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public Vector3d Position { get; set; }

        /// <summary>
        /// Light "intensity" at distance 1
        /// </summary>
        public double Intensity { get; set; } = 1.0;

        public float ConstAttenuation { get; set; } = 0.3f;
        public float LinearAttenuation { get; set; } = 0.3f;
        public float SquareAttenuation { get; set; } = 0.4f;

        public Vector3 GetIntensityAndDirection(Intersection i, Scene scene, out Ray direction)
        {
            float distance = (float)(Position - i.position).Length;

            Vector3d origin = i.position + i.normal * scene.ShadowBias;

            direction = new Ray(origin, this.Position - origin);

            return Color * (float)Intensity / (ConstAttenuation + LinearAttenuation * distance + SquareAttenuation * distance * distance);
        }

        public void GetShadingInfo(Intersection i, Scene scene, out bool isVisible, out Vector3d lightDirection, out Vector3 intensity)
        {
            Vector3d normalOffset = i.normal * scene.ShadowBias;
            Vector3d origin = i.position + normalOffset;

            // check visibility
            var intersections = scene.RootIntersectable.Intersect(new Ray(origin, this.Position - origin));
            if (Intersection.HasIntersection((List<Intersection>)intersections))
                isVisible = false;
            else
                isVisible = true;

            float distance = (float)(Position - i.position).Length;
            intensity = Color * (float)Intensity / (distance * distance);

            lightDirection = origin-Position;
        }

        public bool IsVisible(Intersection i, Scene scene)
        {
            Vector3d normalOffset = i.normal * scene.ShadowBias;
            Vector3d origin = i.position + normalOffset;

            var intersections = scene.RootIntersectable.Intersect(new Ray(origin, this.Position - origin));

            if (Intersection.HasIntersection((List<Intersection>)intersections))
                return false;
            return true;
        }
    }

    class DirectionalLight : ILight
    {
        public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public Vector3d Position { get; set; }
        public double Intensity { get; set; } = 1.0;

        public Vector3d Direction { get; set; }

        public Vector3 GetIntensityAndDirection(Intersection i, Scene scene, out Ray direction)
        {
            direction = new Ray(i.position, -Direction);
            return Color * (float)Intensity;
        }

        public bool IsVisible(Intersection i, Scene scene)
        {
            Vector3d normalOffset = i.normal * scene.ShadowBias;
            Vector3d origin = i.position + normalOffset;

            var intersections = scene.RootIntersectable.Intersect(new Ray(origin, -Direction));

            if (intersections.Count > 0) // TODO
                return false;

            return true;
        }

        public void GetShadingInfo(Intersection i, Scene scene, out bool isVisible, out Vector3d lightDirection, out Vector3 intensity)
        {
            if (IsVisible(i, scene))
                isVisible = true;
            else
                isVisible = false;

            intensity = Color * (float)Intensity;

            lightDirection = Direction;
        }
    }
}
