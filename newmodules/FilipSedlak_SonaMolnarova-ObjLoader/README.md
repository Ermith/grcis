# FastTriangleMesh

Regular triangle mesh is slow in comuting intersection with the ray.
We accelerated this procedure by extending old SceneBrep and
constructing for it Axis Aligned Bounding Box (AABB).
Each ray tests the for an intersection with AABB, and only when hit,
will continue onto more complex procedures. (Testing all the triangles.)
Additional speedups are to be done in future versions.

FastTriangleMesh is an extension of TriangleMesh, therefore it is used in the same way
and all of the old methods are available. Fast and Easy!