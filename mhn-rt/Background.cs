using OpenTK;
using System;
using System.Collections.Generic;

namespace mhn_rt
{
    interface IBackground
    {
        Vector3d GetBackgroundColor(Ray ray);
    }

    class SolidBackground : IBackground
    {
        public Vector3d Color { get; set; }
        public Vector3d GetBackgroundColor(Ray ray)
        {
            return Color;
        }
    }

    class GradientBackground : IBackground
    {
        public Vector3d TopColor { get; }
        public Vector3d BottomColor { get; }

        public GradientBackground(Vector3d topColor, Vector3d bottomColor)
        {
            TopColor = topColor;
            BottomColor = bottomColor;
        }

        public Vector3d GetBackgroundColor(Ray ray)
        {
            Vector3d d = ray.direction.Normalized();
            double m = d.Y;

            return m * TopColor + (1 - m) * BottomColor;
        }

        public static GradientBackground BasicSky { get => new GradientBackground(new Vector3d(0.5, 0.7, 1.0), new Vector3d(1.0, 1.0, 1.0)); }
    }
}
