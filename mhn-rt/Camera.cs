using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace mhn_rt
{
    class Camera
    {
        /// <summary>
        /// vertical FOV angle, has to be < 180.0 
        /// </summary>
        double verticalFOV = (90.0/360.0)*MathHelper.TwoPi;
        double aspectRatio = 16.0 / 9.0;
        double viewportHeight = 2.0;
        double viewportWidth;
        double focalLength = 1.0;

        Vector3d origin = Vector3d.Zero;
        Vector3d horizontal;
        Vector3d vertical;
        Vector3d bottomLeftCorner;

        //Vector3d lookFrom;
        //Vector3d lookAt;
        //Vector3d up;

        public Camera(Vector3d lookFrom, Vector3d lookAt, Vector3d up)
        {
            //viewportWidth = aspectRatio * viewportHeight;
            //horizontal = new Vector3d(viewportWidth, 0, 0);
            //vertical = new Vector3d(0, viewportHeight, 0);
            //bottomLeftCorner = origin - (horizontal / 2)
            //    - (vertical / 2) - new Vector3d(0, 0, focalLength);

            var h = Math.Tan(verticalFOV / 2);
            viewportHeight = 2.0 * h;
            viewportWidth = aspectRatio * viewportHeight;
            horizontal = new Vector3d(viewportWidth, 0, 0);
            vertical = new Vector3d(0, viewportHeight, 0);
            bottomLeftCorner = origin - (horizontal / 2)
                - (vertical / 2) - new Vector3d(0, 0, focalLength);

            var w = (lookFrom - lookAt).Normalized();
            var u = (Vector3d.Cross(up, w)).Normalized();
            var v = Vector3d.Cross(w, u);

            origin = lookFrom;
            horizontal = viewportWidth * u;
            vertical = viewportWidth * v;
            bottomLeftCorner = origin - horizontal / 2 - vertical / 2 - w;
        }

        /// <summary>
        /// Get ray coming from origin going through viewport coords u, v
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public Ray GetRay(double u, double v)
        {
            return new Ray(origin, bottomLeftCorner + (u * horizontal) + (v * vertical) - origin);
        }
    }
}
