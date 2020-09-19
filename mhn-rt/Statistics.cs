using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mhn_rt
{
    static class Statistics
    {
        public static int PrimaryRays  = 0;
        public static int ShadowingRays  = 0;
        public static int ReflectionRays  = 0;
        public static int RefractionRays  = 0;

        public static string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Primary rays: {PrimaryRays}");
            sb.AppendLine($"Shadowing rays: {ShadowingRays}");
            sb.AppendLine($"Reflection rays: {ReflectionRays}");
            sb.AppendLine($"Refraction rays: {RefractionRays}");

            return sb.ToString();
        }
    }
}
