using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using OpenTK;
using System.Threading.Tasks;

namespace mhn_rt
{
    static class ObjLoader
    {
        public static SceneNode LoadObjFile(string filename)
        {
            TriangleManager currentObject = new TriangleManager();
            Mesh currentMaterial = currentObject.GetMesh(0);
            StreamReader sr = new StreamReader(filename);

            // obj file specification http://paulbourke.net/dataformats/obj/
            // only interested in triangle geometry and materials

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> textureCoords = new List<Vector2>();

            Dictionary<string, Mesh> materials = new Dictionary<string, Mesh>();
            materials.Add("default", currentMaterial);

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                int cp = line.IndexOf("#"); // Remove comment
                if (cp != -1)
                    line = line.Substring(0, cp);

                if (line.Length <= 0)
                    continue;

                var tokens = line.Split(new char[] { ' ', '\t' }, options : StringSplitOptions.RemoveEmptyEntries); // split by whitespace

                bool smooth = false;

                float a, b, c; // TODO: Invariant culture
                switch (tokens[0])
                {
                    case "v":
                        //Debug.Assert(tokens.Length == 4);
                        float.TryParse(tokens[1], out a);
                        float.TryParse(tokens[2], out b);
                        float.TryParse(tokens[3], out c);

                        vertices.Add(new Vector3(a, b, c));
                        currentObject.AddVertex(new Vector3(a, b, c));

                        break;
                    case "vt": // only u v, TODO: v is optional (w is optional)
                        float u, v; // TODO: Invariant culture
                        //Debug.Assert(tokens.Length == 3);
                        float.TryParse(tokens[1], out u);
                        float.TryParse(tokens[2], out v);

                        textureCoords.Add(new Vector2(u, v));

                        break;
                    case "vn":
                        //float a, b, c; // TODO: Invariant culture
                        //Debug.Assert(tokens.Length == 4);
                        float.TryParse(tokens[1], out a);
                        float.TryParse(tokens[2], out b);
                        float.TryParse(tokens[3], out c);

                        normals.Add(new Vector3(a, b, c));

                        break;
                    case "f":
                        int v1, v2, v3;
                        int n1, n2, n3;
                        int t1, t2, t3;

                        var p1 = tokens[1].Split('/');
                        var p2 = tokens[2].Split('/');
                        var p3 = tokens[3].Split('/');

                        int.TryParse(p1[0], out v1);
                        int.TryParse(p2[0], out v2);
                        int.TryParse(p3[0], out v3);

                        int triangleIndex;

                        if (v1 < 0)
                            v1 = vertices.Count + v1 + 1;

                        if (v2 < 0)
                            v2 = vertices.Count + v2 + 1;

                        if (v3 < 0)
                            v3 = vertices.Count + v3 + 1;

                        triangleIndex = currentObject.AddTriangleFace(v1 - 1, v2 - 1, v3 - 1);
                        // if not default material
                        currentObject.LinkTriangleToMesh(triangleIndex, currentMaterial);

                        if (p1.Length >= 2)
                        {
                            if (int.TryParse(p1[1], out t1) && int.TryParse(p2[1], out t2) && int.TryParse(p3[1], out t3))
                            {
                                if (t1 < 0)
                                    t1 = textureCoords.Count + t1 + 1;
                                if (t2 < 0)
                                    t2 = textureCoords.Count + t2 + 1;
                                if (t1 < 0)
                                    t3 = textureCoords.Count + t3 + 1;

                                currentObject.SetTriangleTextureCoords(triangleIndex, textureCoords[t1 - 1], textureCoords[t2 - 1], textureCoords[t3 - 1]);
                            }
                        }

                        if (p1.Length >= 3)
                        {
                            if (int.TryParse(p1[2], out n1) && int.TryParse(p2[2], out n2) && int.TryParse(p3[2], out n3))
                            {
                                if (n1 < 0)
                                    n1 = normals.Count + n1 + 1;
                                if (n2 < 0)
                                    n2 = normals.Count + n2 + 1;
                                if (n3 < 0)
                                    n3 = normals.Count + n3 + 1;

                                currentObject.SetTriangleNormals(triangleIndex, normals[n1 - 1], normals[n2 - 1], normals[n3 - 1]);
                            }
                        }

                        break;
                    case "g":
                        break;
                    case "o":
                        break;
                    case "s": // going to ignore smooth groups for now
                        if (tokens.Length >= 2)
                        {
                            string groupName = tokens[1];
                            int groupNumber;

                            if (groupName == "off" || groupName == "0")
                                smooth = false;
                            else
                                int.TryParse(groupName, out groupNumber);
                        }
                        smooth = true;
                        break;
                    case "usemtl":
                        string name = tokens[1];
                        if (materials.ContainsKey(name))
                        {
                            currentMaterial = materials[name];
                            currentMaterial.Parent = currentObject;
                        }
                        break;
                    case "mtllib":
                        if (tokens[1] == "default" || tokens[1] == "Default")
                            break;
                        LoadMtlFile(tokens[1], materials);
                        break;
                    default:
                        Console.WriteLine($"{tokens[0]} is unsupported.");
                        break;
                }


            }

            currentObject.Smooth = true;
            currentObject.BuildBVH();
            var ret = new SceneNode();
            ret.AddChild(currentObject);
            return ret;
        }

        public static void LoadMtlFile(string filename, Dictionary<string, Mesh>materials)
        {
            StreamReader sr = new StreamReader(filename);
            Mesh currentMaterial = new Mesh();

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();

                int cp = line.IndexOf("#"); // Remove comment
                if (cp != -1)
                    line = line.Substring(0, cp);

                if (line.Length <= 0)
                    continue;

                var tokens = line.Split(new char[] { ' ', '\t' }, options: StringSplitOptions.RemoveEmptyEntries); // split by whitespace

                if (tokens.Length < 1)
                    continue;

                double r, g, b, m;
                switch (tokens[0])
                {
                    case "newmtl":
                        currentMaterial = new Mesh();
                        currentMaterial.Material = new PhongMaterial();
                        materials.Add(tokens[1], currentMaterial);
                        break;
                    case "Ka": // currently only single coefficient for all basic colors is supported
                        r = double.Parse(tokens[1]);
                        g = double.Parse(tokens[2]);
                        b = double.Parse(tokens[3]);
                        m = Math.Max(r, Math.Max(g, b));
                        (currentMaterial.Material as PhongMaterial).Ka = m;
                        break;
                    case "Kd":
                        r = double.Parse(tokens[1]);
                        g = double.Parse(tokens[2]);
                        b = double.Parse(tokens[3]);
                        m = Math.Max(r, Math.Max(g, b));
                        r /= m;
                        g /= m;
                        b /= m;
                        (currentMaterial.Material as PhongMaterial).Color = new Vector3((float)r, (float)g, (float)b);
                        (currentMaterial.Material as PhongMaterial).Kd = m;
                        break;
                    case "Ks":
                        r = double.Parse(tokens[1]);
                        g = double.Parse(tokens[2]);
                        b = double.Parse(tokens[3]);
                        m = Math.Max(r, Math.Max(g, b));
                        r /= m;
                        g /= m;
                        b /= m;
                        (currentMaterial.Material as PhongMaterial).Color = new Vector3((float)r, (float)g, (float)b);
                        (currentMaterial.Material as PhongMaterial).Ks = m;
                        break;
                    case "Ke":
                        //(currentMaterial.Material as PhongMaterial).Ke = double.Parse(tokens[1]);
                        break;
                    case "illum":
                        break;
                    case "Ns":
                        (currentMaterial.Material as PhongMaterial).H = double.Parse(tokens[1]);
                        break;
                    case "Ni":
                        break;
                    case "d":
                        // TODO: Might fail with -halo
                        (currentMaterial.Material as PhongMaterial).KTransparency = double.Parse(tokens[1]);
                        if ((currentMaterial.Material as PhongMaterial).KTransparency < 0.0)
                            Console.WriteLine($"{(currentMaterial.Material as PhongMaterial).KTransparency}");
                        break;
                    case "map_Kd":
                        (currentMaterial.Material as PhongMaterial).Texture = new BitmapTexture(tokens[1]);
                        //(currentMaterial.Material as PhongMaterial).Texture = new CheckerTexture3D();
                        break;
                    case "bump":
                        (currentMaterial.Material as PhongMaterial).BumpMap = new BitmapTexture(tokens[1]);
                        break;
                    default:
                        Debug.WriteLine($"Token {tokens[0]} is not handled in .mtl files.");
                        break;
                }
            }
        }
    }
}
