mhn-rt
======
A simple ray-tracer for rendering scenes composed of triangles.

Supported features:
- loading a variant of text based .obj (Wavefront) files
	- only triangle based scenes are supported
	- some example scenes will be provided
- rendering triangle based scenes
	- a Bounding Volume Hierarchy (constructed using Surface Area Heuristic) will be used as a data structure for triangles in order to support fast ray intersection search (O(log N) instead of naive O(N))
		- construction will be implemented as described in a book Physically Based Rendering: http://www.pbr-book.org/3ed-2018/Primitives_and_Intersection_Acceleration/Bounding_Volume_Hierarchies.html
- parallel rendering

Possible extensions:
- simple GUI for selecting scene and showing/exporting resulting image