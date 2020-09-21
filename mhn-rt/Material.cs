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

    class PhongMaterial : IMaterial
    {
        public double KTransparency { get; set; } = 0.0;
        public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);// will be ignored if Texture != null
        public ITexture Texture { get; set; }
        public ITexture NormalMap { get; set; }

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
