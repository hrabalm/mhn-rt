using System;
using System.Collections.Generic;
using OpenTK;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace mhn_rt
{
    /// <summary>
    /// Bounding Volume Hierarchy for triangles, implemented as described in Physically Based Rendering (http://www.pbr-book.org/3ed-2018/contents.html
    /// http://www.pbr-book.org/3ed-2018/Primitives_and_Intersection_Acceleration/Bounding_Volume_Hierarchies.html).
    ///
    /// Not all performance related concepts are implemented.
    /// </summary>
    class TriangleManager : IIntersectable
    {
        public IIntersectable Parent { get; set; }
        public IMaterial Material { get => DefaultMesh.Material; set => value = DefaultMesh.Material; }

        public bool Smooth { get => DefaultMesh.Smooth; set => DefaultMesh.Smooth = value; }
        /// <summary>
        /// If true, computes two intersections for every triangle (each triangle is then considered a very thin object). May be useful for transparent objects.
        /// </summary>
        public bool Shell { get => DefaultMesh.Shell; set => DefaultMesh.Shell = value; }

        protected List<TriangleVertice> triangles = new List<TriangleVertice>();
        protected List<Vector3> vertices = new List<Vector3>();
        protected List<TriangleNormals> normals = null;
        protected List<Vector3> colors = null; // Vertex colors
                                               //protected List<Vector2> txtCoords = null;
        protected List<TextureCoords> textureCoords = null;

        protected AbstractNode root; // BVH tree
        protected Mesh DefaultMesh { get; } // DefaultMesh is used for triangles which should share properties with their parent, so it isn's meant to be changed
        protected List<Mesh> meshes = new List<Mesh>(); // unique list of different triangle meshes
        protected List<int> triangleMesh = null;

        public int MeshesCount { get => meshes.Count; }

        public double Start { get; set; }
        public double End { get; set; }
        public double Time { get; set; }

        public TriangleManager()
        {
            DefaultMesh = new Mesh();
            meshes.Add(DefaultMesh);
            DefaultMesh.Parent = this;
        }

        #region Intersections
        public IList<Intersection> Intersect(Ray ray)
        {
            Vector3d p0 = ray.origin;
            Vector3d p1 = ray.direction;
            List<Intersection> result = new List<Intersection>();
            RecursiveIntersect(root, ray, ref result);
            return new List<Intersection>(result);
        }

        protected void RecursiveIntersect(AbstractNode node, Ray ray, ref List<Intersection> result)
        {
            if (node is LeafNode) // node is leaf
            {
                if (node.bounds.Intersect((Vector3)ray.origin, (Vector3)ray.direction))
                {
                    // call intersection test on a triangle
                    foreach (var index in ((LeafNode)node).triangleIndexes)
                    {
                        GetTrianglesMesh(index).IntersectTriangle(index, ray, ref result);
                    }
                }
            }
            else if (node is InnerNode)
            {
                // test if we intersect bounding box
                if (node.bounds.Intersect((Vector3)ray.origin, (Vector3)ray.direction))
                {
                    var inner = (InnerNode)node;
                    RecursiveIntersect(inner.children[0], ray, ref result);
                    RecursiveIntersect(inner.children[1], ray, ref result);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        protected Vector3 FindSmoothNormal(int triangleIndex, Vector2d uv)
        {
            Debug.Assert(normals.Count > 0);

            Vector3 n1, n2, n3;
            GetNormals(triangleIndex, out n1, out n2, out n3);

            Vector3 normal = n1 * (float)(1.0 - uv.X - uv.Y);
            normal += n2 * (float)uv.X;
            normal += n3 * (float)uv.Y;

            return normal;
        }

        #endregion

        #region BVH SAH Build
        public void BuildBVH()
        {
            List<TriangleInfo> aux = new List<TriangleInfo>();
            for (int i = 0; i < triangles.Count; i++)
                aux.Add(new TriangleInfo(i, this));

            int nodeCount = 0;
            root = RecursiveBuild(aux, 0, aux.Count, ref nodeCount);
        }

        AbstractNode RecursiveBuild(List<TriangleInfo> aux, int start, int end, ref int nodeCount)
        {
            AbstractNode result;

            nodeCount++;

            // bounding box of all elements
            BoundingBox bb = aux[start].bounds;
            for (int i = start + 1; i < end; i++)
                bb = BoundingBox.Union(bb, aux[i].bounds);

            int count = end - start; // number of elements

            if (count == 1) // there's nothing to partition so we create a leaf node
            {
                result = new LeafNode(bb, new int[] { aux[start].triangleIndex });
            }
            else
            {
                // Find BoundingBox which encompases all centroids and use it to select the axis we will be
                // splitting - the idea is described in http://www.pbr-book.org/3ed-2018/Primitives_and_Intersection_Acceleration/Bounding_Volume_Hierarchies.html
                var cb = BoundingBox.INIT;
                for (int i = start; i < end; i++)
                    cb = BoundingBox.Union(cb, new BoundingBox(aux[i].centroid, aux[i].centroid));

                int axis = cb.MaximumExtent(); // axis to partition

                int mid = (start + end) / 2;
                if (cb.pointMax.Get(axis) == cb.pointMin.Get(axis)) // we can't really partition this so we just create a leaf
                {
                    List<int> tgs = new List<int>();
                    for (int i = start; i < end; i++)
                        tgs.Add(aux[i].triangleIndex);
                    result = new LeafNode(bb, tgs);
                    return result;
                }

                // if there aren't many elements left, we split into equaly sized nodes - I will use sorting here but quick select would be faster for larger collections
                if (count <= 4) // maybe 2?
                {
                    bool sorted = false; // bubblesort, because I can't pass lambda to List.Sort (and the can't just implement Compare on AuxTriangle, because the axis changes
                    while (!sorted)
                    {
                        sorted = true;
                        for (int i = start; i < end - 1; i++)
                        {
                            if (aux[i].centroid.Get(axis) > aux[i + 1].centroid.Get(axis))
                            {
                                sorted = false;
                                var temp = aux[i];
                                aux[i] = aux[i + 1];
                                aux[i + 1] = temp;
                            }
                        }
                    }

                    var left = RecursiveBuild(aux, start, mid, ref nodeCount);
                    var right = RecursiveBuild(aux, mid, end, ref nodeCount);

                    result = new InnerNode(bb, left, right); // create inner node
                }
                else // otherwise we use SAH for splitting
                {
                    int buckets_count = 12; // how many buckets will we use?
                    BucketInfo[] buckets = new BucketInfo[buckets_count];

                    for (int i = 0; i < buckets.Length; i++)
                    {
                        buckets[i].count = 0;
                        buckets[i].bounds = BoundingBox.INIT;
                    }

                    // find how many triangles are in each bucket and bounding boxes of the buckets
                    for (int i = start; i < end; i++)
                    {
                        int b = (int)(buckets.Length * cb.Offset(aux[i].centroid).Get(axis));
                        if (b >= buckets.Length)
                            b = buckets.Length - 1;
                        buckets[b].count++;
                        buckets[b].bounds = BoundingBox.Union(buckets[b].bounds, aux[i].bounds);
                    }

                    // costs for splitting after each bucket (can't split after the last one)
                    float[] cost = new float[buckets.Length - 1];
                    for (int i = 0; i < cost.Length; i++)
                    {
                        BoundingBox bb0 = BoundingBox.INIT, bb1 = BoundingBox.INIT;
                        int count0 = 0, count1 = 0;
                        for (int j = 0; j <= i; j++)
                        {
                            bb0 = BoundingBox.Union(bb0, buckets[j].bounds);
                            count0 += buckets[j].count;
                        }

                        for (int j = i + 1; j < buckets.Length; j++)
                        {
                            bb1 = BoundingBox.Union(bb1, buckets[j].bounds);
                            count1 += buckets[j].count;
                        }

                        cost[i] = 1 + (count0 * bb0.SurfaceArea() + count1 * bb1.SurfaceArea()) / bb.SurfaceArea();
                    }

                    // find the best bucket for splitting (minimal cost)
                    float minCost = cost[0];
                    int minCostBucket = 0;

                    for (int i = 1; i < cost.Length; i++)
                    {
                        if (cost[i] < minCost)
                        {
                            minCost = cost[i];
                            minCostBucket = i;
                        }
                    }

                    // decide whether it's better to create a leaf or split at found bucket
                    float leafCost = count;
                    int maxInNode = 4;
                    if (count > maxInNode || minCost < leafCost) // split
                    {
                        // Partition
                        int index0 = start;
                        int index1 = end - 1;

                        Func<TriangleInfo, bool> exp = delegate (TriangleInfo t)
                        {
                            int b = (int)(buckets.Length * cb.Offset(t.centroid).Get(axis));
                            if (b == buckets.Length)
                                b = buckets.Length - 1;

                            return b <= minCostBucket;
                        };

                        while (index0 < index1)
                        {
                            while (exp(aux[index1]) == false && index1 > index0)
                                index1--;

                            while (exp(aux[index0]) == true && index0 < index1)
                                index0++;

                            if (exp(aux[index0]) == false && exp(aux[index1]) == true)
                            {
                                var temp = aux[index0];
                                aux[index0] = aux[index1];
                                aux[index1] = temp;

                                index0++;
                                index1--;
                            }
                        }

                        for (int i = start; i < end; i++) // find first element after the split
                        {
                            if (exp(aux[i]) == false)
                            {
                                mid = i;
                                break;
                            }
                        }

                        var left = RecursiveBuild(aux, start, mid, ref nodeCount);
                        var right = RecursiveBuild(aux, mid, end, ref nodeCount);

                        result = new InnerNode(bb, left, right); // create inner node

                    }
                    else // create leaf
                    {
                        List<int> indexes = new List<int>();
                        for (int i = start; i < end; i++)
                            indexes.Add(aux[i].triangleIndex);
                        result = new LeafNode(bb, indexes);
                    }
                }
            }
            return result;
        }

        #endregion

        public void GetTriangleVertices(int index, out int v1, out int v2, out int v3)
        {
            v1 = triangles[index].v1;
            v2 = triangles[index].v2;
            v3 = triangles[index].v3;
        }

        public void GetTriangleVertices(int index, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            v1 = GetVertex(triangles[index].v1);
            v2 = GetVertex(triangles[index].v2);
            v3 = GetVertex(triangles[index].v3);
        }

        #region Direct Modification/Setup

        public int TrianglesCount { get => triangles.Count; }
        public int VerticesCount { get => vertices.Count; }

        public int AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int p1 = AddVertex(v1);
            int p2 = AddVertex(v2);
            int p3 = AddVertex(v3);

            int t = AddTriangleFace(p1, p2, p3);
            return t;
        }

        public int AddTriangleFace(int v1, int v2, int v3)
        {
            triangles.Add(new TriangleVertice(v1, v2, v3));
            return triangles.Count - 1;
        }
        public int AddVertex(Vector3 coords)
        {
            vertices.Add(coords);
            return vertices.Count - 1;
        }

        public void SetVertexColor(int index, Vector3 color)
        {
            Debug.Assert(index >= 0 && index < vertices.Count);
            if (colors == null)
                colors = new List<Vector3>(vertices.Count);

            if (colors.Count <= index)
            {
                int add = index - colors.Count;
                for (int i = 0; i < add; i++)
                    colors.Add(new Vector3());
            }

            colors[index] = color;
        }

        public void SetTriangleNormals(int triangleIndex, Vector3 n1, Vector3 n2, Vector3 n3)
        {
            Debug.Assert(triangleIndex >= 0 && triangleIndex < triangles.Count);
            if (normals == null)
                normals = new List<TriangleNormals>(triangleIndex);

            while (triangleIndex >= normals.Count)
                normals.Add(new TriangleNormals());

            normals[triangleIndex] = new TriangleNormals(n1, n2, n3);
        }

        public void SetTriangleTextureCoords(int triangleIndex, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            if (textureCoords == null)
                textureCoords = new List<TextureCoords>(TrianglesCount);
            while (textureCoords.Count <= triangleIndex)
                textureCoords.Add(TextureCoords.Invalid);
            textureCoords[triangleIndex] = new TextureCoords(v1, v2, v3);
        }

        public Vector3 GetVertex(int index)
        {
            return vertices[index];
        }

        public void GetNormals(int triangleIndex, out Vector3 n1, out Vector3 n2, out Vector3 n3)
        {
            var norm = normals[triangleIndex];
            n1 = norm.n1;
            n2 = norm.n2;
            n3 = norm.n3;
        }

        public bool HasNormals(int triangleIndex)
        {
            if (normals != null && normals.Count > triangleIndex)
            {
                return !normals[triangleIndex].IsEmpty();
            }
            return false;
        }

        public void GetTexureCoords(int triangleIndex, out Vector2 v1, out Vector2 v2, out Vector2 v3)
        {
            var tc = textureCoords[triangleIndex];
            v1 = tc.v1;
            v2 = tc.v2;
            v3 = tc.v3;
        }

        public bool HasTextureCoords(int triangleIndex)
        {
            if (textureCoords != null && textureCoords.Count > triangleIndex)
                return !TextureCoords.IsInvalid(textureCoords[triangleIndex]);
            return false;
        }

        public Vector3 GetVertexColor(int index)
        {
            Debug.Assert(normals != null, "Colors aren't defined.");
            Debug.Assert(index >= 0 && index < normals.Count, $"There is no corresponding color to given vertex handle {index}.");
            return colors[index];
        }

        public int AddMesh(Mesh mesh)
        {
            meshes.Add(mesh);
            return meshes.Count - 1;
        }

        public Mesh GetTrianglesMesh(int triangleIndex)
        {
            if (triangleMesh == null || triangleIndex >= triangleMesh.Count)
                return DefaultMesh;
            return meshes[triangleMesh[triangleIndex]];
        }

        public Mesh GetMesh(int meshIndex)
        {
            return meshes[meshIndex];
        }

        public void LinkTriangleToMesh(int triangleIndex, int meshIndex)
        {
            if (triangleMesh == null)
                triangleMesh = new List<int>(TrianglesCount);
            if (triangleMesh.Count <= triangleIndex)
            {
                while (triangleMesh.Count < TrianglesCount)
                    triangleMesh.Add(0);
            }

            triangleMesh[triangleIndex] = meshIndex;
        }

        public void LinkTriangleToMesh(int triangleIndex, Mesh mesh)
        {
            int i = meshes.IndexOf(mesh);
            if (i == -1)
            {
                i = AddMesh(mesh);
            }

            LinkTriangleToMesh(triangleIndex, i);
        }

        public void UnlinkTriangleFromMesh(int triangleIndex, int meshIndex)
        {
            triangleMesh[triangleIndex] = 0; // first mesh should always be default mesh
        }
        #endregion

        public void GetBoundingBox(out Vector3d corner1, out Vector3d corner2)
        {
            Debug.Assert(root != null);
            corner1 = (Vector3d)root.bounds.pointMin;
            corner2 = (Vector3d)root.bounds.pointMax;
        }

        public int getSerial()
        {
            throw new NotImplementedException();
        }

        public Vector2 GetTriangleUV(int triangleIndex, Vector2 intersection_uv)
        {
            // Convert from baryocentric coords to cartesian coords
            Vector2 a, b, c;
            if (HasTextureCoords(triangleIndex))
                GetTexureCoords(triangleIndex, out a, out b, out c);
            else
                return Vector2.Zero;

            double u = intersection_uv.X;
            double v = intersection_uv.Y;
            double w = 1 - u - v;

            return (Vector2)(u * (Vector2d)(b) + v * (Vector2d)(c) + w * (Vector2d)(a));
        }

        struct TriangleInfo
        {
            public readonly int triangleIndex;
            public BoundingBox bounds;
            public Vector3 centroid;

            /// <summary>
            /// Auxiliary structure used when construing BVH
            /// </summary>
            /// <param name="triangleIndex"></param>
            public TriangleInfo(int triangleIndex, TriangleManager node)
            {
                this.triangleIndex = triangleIndex;
                this.bounds = default;
                this.centroid = default;

                FindBoundsAndCentroid(node);
            }

            public void FindBoundsAndCentroid(TriangleManager node) //
            {
                Vector3 v1, v2, v3;
                node.GetTriangleVertices(triangleIndex, out v1, out v2, out v3);

                bounds = new BoundingBox(new Vector3(Math.Min(v1.X, Math.Min(v2.X, v3.X)), Math.Min(v1.Y, Math.Min(v2.Y, v3.Y)), Math.Min(v1.Z, Math.Min(v2.Z, v3.Z))),
                  new Vector3(Math.Max(v1.X, Math.Max(v2.X, v3.X)), Math.Max(v1.Y, Math.Max(v2.Y, v3.Y)), Math.Max(v1.Z, Math.Max(v2.Z, v3.Z))));

                centroid = (v1 + v2 + v3) / 3;
            }
        }

        public struct BucketInfo
        {
            public int count;
            public BoundingBox bounds;
        }

        public class AbstractNode
        {
            public BoundingBox bounds;
        }

        public class LeafNode : AbstractNode
        {
            public IList<int> triangleIndexes;
            public LeafNode(BoundingBox boundingBox, IList<int> triangles) // leaf node
            {
                this.bounds = boundingBox;
                this.triangleIndexes = triangles;
            }
        }

        public class InnerNode : AbstractNode
        {
            public AbstractNode[] children = null;
            public InnerNode(BoundingBox boundingBox, AbstractNode child0, AbstractNode child1) // inner node
            {
                this.bounds = boundingBox;
                this.children = new AbstractNode[] { child0, child1 };
            }
        }
    }

    class Mesh
    {
        public TriangleManager Parent { get; set; }
        public IMaterial Material
        {
            get
            {
                if (_material != null)
                    return _material;
                else
                    return Parent.Parent.Material;
            }
            set => _material = value;
        }
        protected IMaterial _material;

        public Matrix4d ToParent { get; set; }
        public Matrix4d FromParent { get; set; }

        public bool Smooth { get; set; } = true;
        public bool Shell { get; set; } = false;

        public LinkedList<Intersection> Intersect(Vector3d p0, Vector3d p1)
        {
            return null;
        }

        public virtual void IntersectTriangle(int triangleIndex, Ray ray, ref List<Intersection> result)
        {
            TriangleManager tm = Parent;

            Vector3 a, b, c;
            tm.GetTriangleVertices(triangleIndex, out a, out b, out c);
            Vector2d uv;

            double t = Help.RayTriangleIntersection(ref ray, ref a, ref b, ref c, out uv);
            if (double.IsInfinity(t) || t <= double.Epsilon)
                return;

            Vector3 normal = Vector3.Cross(b - a, c - a); // I need approx. normal to determine Front and Enter
            float x = Vector3.Dot(normal, ((Vector3)ray.direction));

            if (Smooth && tm.HasNormals(triangleIndex))
            {
                normal = FindSmoothNormal(triangleIndex, uv, tm);
            }

            bool front = x < 0; // does ray and normal have different direction?

            Vector3d newColor = Vector3d.Zero;
            double localAlpha = 1.0;

            uv = (Vector2d)Parent.GetTriangleUV(triangleIndex, (Vector2)uv); // TODO: Rework

            // 1st intersection
            //Intersection i = new Intersection(this)
            //bool enter = Vector3d.Dot(ray.direction.Normalized(), (Vector3d)normal) < 0.0 ? true : false;
            Intersection i = new Intersection()
            {
                t = t,
                material = Material,
                position = ray.origin + t * ray.direction,
                normal = (Vector3d)normal,
                color = (Vector3)newColor,
                uv = front ? (Vector2)uv : (Vector2) uv,
                //uv = enter ? (Vector2)uv : new Vector2((float)(1-uv.X), (float)uv.Y), // TODO: Check - leaving ray might not need color information
                Enter = front,
            };

            i.localAlpha = localAlpha;

            result.Add(i);

            // 2nd intersection
            if (Shell) // TODO: Implement shell mode
            {

            }
        }

        protected Vector3 FindSmoothNormal(int triangleIndex, Vector2d uv, TriangleManager tm)
        {
            Vector3 n1, n2, n3;
            tm.GetNormals(triangleIndex, out n1, out n2, out n3);

            Vector3 normal = n1 * (float)(1.0 - uv.X - uv.Y);
            normal += n2 * (float)uv.X;
            normal += n3 * (float)uv.Y;

            return normal;
        }

        public int getSerial()
        {
            throw new NotImplementedException();
        }
    }

    #region Extensions
    public static partial class Extensions
    {
        public static float Get(this Vector3 v, int dim)
        {
            Debug.Assert(dim >= 0 && dim <= 2);

            if (dim == 0)
                return v.X;
            if (dim == 1)
                return v.Y;
            return v.Z;
        }
    }
    #endregion

    #region Auxiliary structures
    

    public struct TextureCoords
    {
        public readonly Vector2 v1;
        public readonly Vector2 v2;
        public readonly Vector2 v3;

        public TextureCoords(Vector2 v1, Vector2 v2, Vector2 v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }

        public static TextureCoords Invalid = new TextureCoords(new Vector2(-100, -100), new Vector2(-100, -100), new Vector2(-100, -100));
        public static bool IsInvalid(TextureCoords tc)
        {
            if (tc.v1 == Invalid.v1 && tc.v2 == Invalid.v2 && tc.v3 == Invalid.v3)
                return true;
            return false;
        }
    }

    public struct TriangleVertice
    {
        public readonly int v1;
        public readonly int v2;
        public readonly int v3;

        public TriangleVertice(int v1, int v2, int v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }

    public struct TriangleNormals
    {
        public readonly Vector3 n1;
        public readonly Vector3 n2;
        public readonly Vector3 n3;

        public TriangleNormals(Vector3 n1, Vector3 n2, Vector3 n3)
        {
            this.n1 = n1;
            this.n2 = n2;
            this.n3 = n3;
        }

        public bool IsEmpty()
        {
            if (n1 == n2 && n1 == n3 && n1 == Vector3.Zero)
                return true;
            return false;
        }
    }

    public class TriangleIntersectionInfo
    {
        public int triangleIndex;
        public Vector2d uv;
    }

    public struct BoundingBox
    {
        // We are using axis aligned bounding boxes, so only two points are necessary to represent them;
        public Vector3 pointMin;
        public Vector3 pointMax;

        public static BoundingBox INIT = new BoundingBox(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), new Vector3(float.MinValue, float.MinValue, float.MinValue));

        public BoundingBox(Vector3 pointMin, Vector3 pointMax)
        {
            this.pointMax = pointMax;
            this.pointMin = pointMin;
        }

        public static BoundingBox Union(BoundingBox a, BoundingBox b)
        {
            return new BoundingBox(new Vector3(Math.Min(a.pointMin.X, b.pointMin.X), Math.Min(a.pointMin.Y, b.pointMin.Y), Math.Min(a.pointMin.Z, b.pointMin.Z)),
                                   new Vector3(Math.Max(a.pointMax.X, b.pointMax.X), Math.Max(a.pointMax.Y, b.pointMax.Y), Math.Max(a.pointMax.Z, b.pointMax.Z))
                                   );
        }

        public int MaximumExtent() // does it work?
        {
            var d = (pointMax - pointMin);

            if (d.X > d.Y && d.X > d.Z)
                return 0;
            else if (d.Y > d.Z)
                return 1;
            else
                return 2;
        }

        public float SurfaceArea()
        {
            Vector3 d = pointMax - pointMin;
            return 2 * (d.X * d.Y + d.X * d.Z + d.Y * d.Z);
        }

        public static Vector3 Abs(Vector3 v)
        {
            return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }

        internal Vector3 Offset(Vector3 point)
        {
            Vector3 off = point - pointMin;
            if (pointMax.X > pointMin.X) // we have to check if we aren't dividing by zero
                off.X /= pointMax.X - pointMin.X;
            if (pointMax.Y > pointMin.Y)
                off.Y /= pointMax.Y - pointMin.Y;
            if (pointMax.Z > pointMin.Z)
                off.Z /= pointMax.Z - pointMin.Z;
            return off;
        }

        /// <summary>
        /// True if this box has intersection with ray p0->p1
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <returns></returns>
        public bool Intersect(Vector3 p0, Vector3 p1)
        {
            //return true;
            float tMin = 0;
            float tMax = float.PositiveInfinity;

            for (int i = 0; i < 3; i++)
            {
                float invRayDir = 1 / p1.Get(i);
                float tNear = (pointMin.Get(i) - p0.Get(i)) * invRayDir;
                float tFar = (pointMax.Get(i) - p0.Get(i)) * invRayDir;

                if (tNear > tFar) // swap if necessary
                {
                    var temp = tNear;
                    tNear = tFar;
                    tFar = temp;
                }

                tFar *= 1 + 2 * ((3 * float.Epsilon) / (1 - 3 * float.Epsilon));
                //              ^^^^^ - gamma(3)
                tMin = tNear > tMin ? tNear : tMin;
                tMax = tFar < tMax ? tFar : tMax;

                if (tMin > tMax)
                    return false;
            }
            return true;
        }
    }

    #endregion
}