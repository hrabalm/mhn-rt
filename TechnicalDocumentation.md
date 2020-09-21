# mhn-rt Technical Documentation

`mhn-rt` is a simple ray tracer written in C#. `OpenTK` is used for its implementation of vectors, matrices and transformations.

## Code structure

### Scene related:
- Background.cs
- Camera.cs
- FastTriangleMesh.cs
- Intersectable.cs
- Light.cs
- Scene.cs
- TestScenes.cs

### Rendering related:
- Material.cs
- Ray.cs
- RayTracer.cs
- Renderer.cs
- Texture.cs

### Other:
- Help.cs (some geometry and IO)
- Program.cs
- Statistics.cs

## Interesting pieces and algorithms:

### Renderer.cs:Renderer
- takes care of generating a resulting bitmap from an instance of Scene using IRenderer it's passed
- uses supersampling with jitter
- lines are rendered in parallel

### RayTracer.cs:SimpleRayTracer(:IRayTracer)
- implements a variant of Whitted ray tracing algorithm
	- backwards ray tracing:
		- rays are sent from the camera
		- the first intersection is found
		- depending on material properties
	- direct illumination only
- recursive function `GetRayColor`
	- max depth of recursion + contribution threshold (if a ray doesn't contribute to resulting color, it is skipped)
	
### Intersectable.cs:IIntersectable
- objects in the scene implement this interface, e.g. `Intersectable.cs:Sphere` and `TriangleManager.cs:TriangleManager`

### TriangleManager.cs:TriangleManager
- represents a single object made of triangles, can be loaded from .obj using `ObjLoader`
- internally stores triangles in Bounding Volume Hierarchy(BVH) to enable fast intersection search (log N instead of N):
	- BVH is a binary tree such that Axis Aligned Bounding Box(AABB) of a parent encloses those of its children
	- When looking for intersection, if a ray doesn't intersect with the bounding box of a tree or a subtree, we know that it doesn't intersect with any object inside. If it does, we have to test intersection with its children (and potentially with the triangles themselves). In conclusion, this data structure helps us prove that a ray doesn't hit the object, not the oposite. (That is usually reasonable approach however, e.g.: A single complicated object made of many triangles may take up small portion of the screen. We learn that most rays don't intersect any of its triangles quickly and since BVH is tree data structure (as opposed to only providing top level bounding box), the same can be said when talking about smaller parts of the object itself)
	- (top-down) BVH construction used is described in-depth in [PBRT](http://www.pbr-book.org/3ed-2018/Primitives_and_Intersection_Acceleration/Bounding_Volume_Hierarchies.html), short overview:
		- two types of nodes used, both have their bounding box:
			- `LeafNode` - contains one or several triangles not worth splitting further
			- `InnerNode` - classic binary inner node, two children
		1. Precalculate bounding boxes for individual triangles, store triangles in an array
		2. Recursively build for a given segment of triangles(nodes) (starting with the whole array), method returns the root of the whole tree (or subtree):
			3. Find a bounding box of all triangles in the whole segment (bounding box of the node)
			4. If only a single triangle is left, return `LeafNode` containing the triangle, else continue
			5. Select axis which will be used to divide triangles(nodes) to two segments (position of triangle/node is determined by its centroid)
			6. If triangles can't be reasonably separated (their centroids coincide), return `LeafNode` containing all of them, else continue.
			7. Go through possible splitting points on the axis and calculate costs of splitting there using SAH
			8. Find the best splitting point (minimal cost)
			9. Either split here (return `InnerNode` and call recursively on both segments, getting children) or create `LeafNode` if the estimated cost of doing so is lower.
			
		- some performance related ideas are implemented as described in PBRT, e.g.:
			- before splitting, nodes are seperated into several buckets and only boundaries between these buckets are considered as points of split, detailed description is present in the book

### TriangleManager.cs:Mesh
- represents a subgroup of triangles in its parent `TriangleManager` sharing a common material. All `TriangleManager` have a single `.DefaultMesh`. Additional `Mesh` are for example when loading .obj files that use material groups.

## Creating a new scene

### Create a method which generates or loades scene:
```csharp
public static Scene MyNewScene()
{
    Scene scene = new Scene()
    {
        Background = GradientBackground.BasicSky,
    };
    SceneNode rootNode = scene.RootIntersectable;

    // Add your objects as children of rootNode
	rootNode.AddChild(new Sphere(new Vector3d(2.3, -0.5, -1.0), 0.5, new PhongMaterial()))
	
	// They can be 
			
	// Add light sources, e.g.: DirectionalLight, PointLight
    scene.LightSources.Add(new DirectionalLight { Direction = new Vector3d(0.4, -0.5, -0.75), Intensity = 1.0 });
			
	// A camera has to be added
    scene.Camera = new Camera(new Vector3d(0.5, 0.0, 1.5), new Vector3d(0.0, 0.0, -0.8), new Vector3d(0.0, 1.0, 0.0));

    return scene;
}
```

### Register it as a delegate in `TestScenes.cs:TestScenes.RegisterScenes()`
```csharp
public static void RegisterScenes()
{
	// ...
    SceneRegistry.Scenes.Add("My New Scene", TestScenes.MyNewScene);
}
```

### Supported .obj and .mtl statements

#### .obj (http://paulbourke.net/dataformats/obj/)
- v
- vt
- vn
- f (only triangulated faces, polynoms not supported)
- usemtl
- mtllib

#### .mtl (http://paulbourke.net/dataformats/mtl/)
- newmtl
- Ka
- Kd
- Ks
- Ns
- Ni
- d
- Map_kd
- bump

##### Caveats:
- Ka, Kd and Ks share the same color. When used with this project, they are meant to represent only coefficients of their components.