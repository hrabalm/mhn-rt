using System;
using System.Collections.Generic;
using OpenTK;

namespace mhn_rt
{
    class Scene
    {
        public SceneNode RootIntersectable { get; } = new SceneNode();
        public IList<ILight> LightSources { get; set; } = new List<ILight>();
        public Camera Camera { get; set; }
        public IBackground Background { get; set; } = new SolidBackground();
        public double ShadowBias { get; set; } = 0.00000001;

        public Scene()
        {
            RootIntersectable.Material = new PhongMaterial(); // default material for the scene
        }
    }

    class SceneNode : IIntersectable
    {
        public List<IIntersectable> objects = new List<IIntersectable>();
        public IIntersectable Parent { get; set; }
        public IMaterial Material
        {
            get
            {
                if (_material != null)
                    return _material;
                else
                    return Parent.Material;
            }
            set => _material = value;
        }
        protected IMaterial _material;

        protected Matrix4d _toParent;
        protected Matrix4d _toObject;

        public Matrix4d ToParent
        {
            get => _toParent;
            set
            {
                _toParent = value;
                _toObject = _toParent.Inverted();
            }
        }
        public Matrix4d ToObject
        {
            get => _toObject;
            set
            {
                _toObject = value;
                _toParent = _toObject.Inverted();
            }
        }

        public SceneNode()
        {
            ToParent = Matrix4d.Identity;
        }

        public void AddChild(IIntersectable child)
        {
            child.Parent = this;
            objects.Add(child);
        }

        public IList<Intersection> Intersect(Ray ray)
        {
            var res = new List<Intersection>();

            var tRay = new Ray(Vector3d.Transform(ray.origin, ToObject), Vector3d.TransformVector(ray.direction, ToObject));

            foreach (var o in objects)
            {
                // maybe not all intersections have to be transformed
                var intersections = o.Intersect(tRay);
                for (int i = 0; i < intersections.Count; i++)
                {
                    intersections[i] = intersections[i].Transform(ToParent);
                }
                res.AddRange(intersections);
            }

            res.Sort((p1, p2) => p1.t.CompareTo(p2.t));

            if (res.Count > 0)
                res[0].normal.Normalize();

            return res;
        }
    }

    static class SceneRegistry
    {
        public static SortedDictionary<string, Func<Scene>> Scenes = new SortedDictionary<string, Func<Scene>>();
    }
}
