﻿using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace mhn_rt
{
    interface IRayTracer
    {
        Vector3d GetRayColor(Ray ray, Scene scene, int depth, float weight);
    }
    class SimpleRayTracer : IRayTracer
    {
        bool shadows = true;
        bool reflections = true;
        bool refraction = true;

        public Vector3d GetRayColor(Ray ray, Scene scene, int depth, float weight)
        {
            // TODO: Weights and Reflectivity can be 1+ in bad scenes, probably gotta normalize them
            var intersections = scene.RootIntersectable.Intersect(ray);

            if (depth == 0 || weight < 0.05f)
                return new Vector3d(0.0, 0.0, 0.0);

            if (intersections.Count > 0)
            {
                Intersection i1 = intersections[0];
                i1.ApplyTexture();

                PhongMaterial pm = (PhongMaterial)i1.material;

                Vector3d diffuse = new Vector3d(0, 0, 0);
                Vector3d specular = new Vector3d(0, 0, 0); // specular reflection of either light source or other object

                // normalize coefficients so that they add up to 1.0
                double Kd = pm.Kd / (pm.Kd + pm.Ks + pm.Ka);
                double Ks = pm.Ks / (pm.Kd + pm.Ks + pm.Ka);
                double Ka = pm.Ka / (pm.Kd + pm.Ks + pm.Ka);

                double weightSum = Kd+Ks+Ka;

                double localAlpha = i1.localAlpha;
                double globalAlpha = 1.0 - (i1.material as PhongMaterial).KTransparency;
                if (globalAlpha < 0.0)
                    globalAlpha = 0.0;

                // direct illumination
                foreach (var light in scene.LightSources)
                {
                    Ray dlRay;
                    Vector3d intensity = (Vector3d)light.GetIntensityAndDirection(i1, scene, out dlRay);
                    
                    if (shadows) // TODO: Rework
                    {
                        Interlocked.Increment(ref Statistics.ShadowingRays);
                        if (!light.IsVisible(i1, scene))
                            continue;
                    }

                    // light reaches the intersection point
                    dlRay.direction.Normalize();
                    double d = Vector3d.Dot(dlRay.direction, i1.normal);

                    if (d > 0)
                        diffuse += intensity * (Vector3d)i1.color * d;

                    Vector3d r = Help.Reflect(dlRay.direction, i1.normal);
                    Vector3d v = ray.direction;

                    double h = pm.H; // "glosinesss ~ for cos^h beta"
                    d = Vector3d.Dot(r.Normalized(), v.Normalized());

                    if (d > 0)
                        specular += intensity * (Vector3d)i1.color * Math.Pow(d, h);
                }

                double reflectivity;
                Vector3d reflective = new Vector3d();
                if (reflections && Ks*weight/(weightSum+Ks) >= 0.05f || (refraction && (globalAlpha < 0.95 || localAlpha < 0.95)))
                {
                    weightSum += Ks;
                    reflectivity = Ks;
                    Vector3d rv = Help.Reflect(ray.direction, i1.normal);
                    Vector3d normalOffset = i1.normal * scene.ShadowBias;
                    reflective = GetRayColor(new Ray(i1.position + normalOffset, rv), scene, depth - 1, (float)(weight * reflectivity / weightSum));
                    Interlocked.Increment(ref Statistics.ReflectionRays);
                }
                else
                    reflectivity = 0.0f;

                float transparency = (float)Math.Max((1.0 - i1.localAlpha), pm.KTransparency);

                Vector3d refractive = new Vector3d();
                if (refraction && (globalAlpha < 0.95 || localAlpha < 0.95)) // TODO: Fix
                {
                    weightSum += transparency;
                    Vector3d refracted;
                    refracted = Help.Refract(ray.direction, i1.normal, (i1.material as PhongMaterial).N);

                    if (refracted != Vector3d.Zero)
                    {
                        refracted.Normalize();
                        var offset = refracted * scene.ShadowBias; // move slightly in the direction of the ray
                        refractive = GetRayColor(new Ray(i1.position + offset, refracted), scene, depth - 1, (float)(weight * transparency / weightSum));
                        Interlocked.Increment(ref Statistics.RefractionRays);
                    }
                    else
                    {
                        refractive = reflective;
                    }
                }
                else
                    transparency = 0.0f;

                Vector3d ambient = (Vector3d)i1.color;

                specular = new Vector3d(Math.Max(specular.X, reflective.X), Math.Max(specular.Y, reflective.Y), Math.Max(specular.Z, reflective.Z));

                Vector3d color = Vector3d.Zero;
                color += globalAlpha * (Kd * localAlpha * diffuse + Ka * ambient * localAlpha + Ks * specular * localAlpha + (Ks+Kd+Ka) * (1.0 - localAlpha) * refractive);

                double cos = Vector3d.Dot(ray.direction.Normalized(), i1.normal.Normalized());
                double n = cos >= 0.0 ? (i1.material as PhongMaterial).N : 1.0 / (i1.material as PhongMaterial).N;
                double schlick = Help.Schlick(Math.Abs(cos), n);
                color += (Vector3d)i1.color * (1-globalAlpha) * (schlick * reflective + (1.0-schlick) * refractive);

                return color;
            }

            return scene.Background.GetBackgroundColor(ray);
        }
    }

    class NormalRayTracer : IRayTracer
    {
        public Vector3d GetRayColor(Ray ray, Scene scene, int depth, float weight)
        {
            var intersections = scene.RootIntersectable.Intersect(ray);

            if (depth == 0 || weight < 0.05f)
                return new Vector3d(0.0f, 0.0f, 0.0f);

            if (intersections.Count > 0)
            {
                return new Vector3d(0.5f+(float)intersections[0].normal.X/2.0f, 0.5f + (float)intersections[0].normal.Y / 2.0f, 0.5f + (float)intersections[0].normal.Z / 2.0f);
            }

            return Vector3d.Zero;
        }
    }
}
