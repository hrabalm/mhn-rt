using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace mhn_rt
{
    struct Ray
    {
        public Vector3d origin;
        public Vector3d direction;
        public Ray(Vector3d origin, Vector3d direction)
        {
            this.origin = origin;
            this.direction = direction;
        }

        public Vector3d PointAt(double t)
        {
            return origin + t * direction;
        }
    }
}
