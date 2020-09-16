using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mhn_rt
{
    interface IMaterial
    {
        /// <summary>
        /// Surface Color
        /// </summary>
        Vector3 Color { get; set; }

        /// <summary>
        /// Coefficient of transparency, 0.0 for opaque
        /// </summary>
        double KTransparency { get; set; }
    }

    class LambertianMaterial : IMaterial
    {
        public Vector3 Color { get; set; }
        public double KTransparency { get; set; } = 0.0;
        static Random random = new Random();
        public LambertianMaterial(Vector3 color)
        {
            this.Color = color;
        }

        public bool Scatter(Ray inRay, Intersection intersection, out Vector3 attenuation, out Ray scatteredRay)
        {
            var r = Help.RandomUnitVector();

            scatteredRay = new Ray(intersection.position, intersection.normal + r);
            attenuation = Color;
            return true;
        }
    }

    class MetalMaterial : IMaterial
    {
        public Vector3 Color { get; set; }
        public double KTransparency { get; set; } = 0.0;
        public MetalMaterial(Vector3 color)
        {
            this.Color = color;
        }

        public bool Scatter(Ray inRay, Intersection intersection, out Vector3 attenuation, out Ray scatteredRay)
        {
            Vector3d reflected = Help.Reflect(inRay.direction.Normalized(), intersection.normal);
            scatteredRay = new Ray(intersection.position, reflected);
            attenuation = Color;

            return (Vector3d.Dot(scatteredRay.direction, intersection.normal) > 0);
        }
    }

    class PhongMaterial : IMaterial
    {
        public double KTransparency { get; set; } = 0.0;
        public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);// will be ignored if Texture != null
        public ITexture Texture { get; set; }
        public ITexture BumpMap { get; set; }

        /// <summary>
        /// Diffuse reflection
        /// </summary>
        public double Kd { get; set; } = 0.6;

        /// <summary>
        /// Specular reflection
        /// </summary>
        public double Ks { get; set; } = 0.2;

        /// <summary>
        /// Specular exponent - "glossiness of the material"
        /// </summary>
        public double H { get; set; } = 5;

        /// <summary>
        /// Ambient component
        /// </summary>
        public double Ka { get; set; } = 0.2;

        /// <summary>
        /// Index of refraction
        /// </summary>
        public double N { get; set; } = 1.0;
    }

}
