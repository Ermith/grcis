# FastTriangleMesh

Regular triangle mesh is slow in comuting intersection with the ray.
We accelerated this procedure by extending old SceneBrep and
constructing for it Axis Aligned Bounding Box (AABB).
Each ray tests the for an intersection with AABB, and only when hit,
will continue onto more complex procedures. (Testing all the triangles.)
Additional speedups are to be done in future versions.

FastTriangleMesh is an extension of TriangleMesh, therefore it is used in the same way
and all of the old methods are available. Fast and Easy!

# ObjLoader
This extension implements faster loader of .obj files with possibility of loading basic material. Loaded material only sets coefficients and base color of already implemented PhongMaterial.

## Usage
Object loader is used in scene scripts. Class ObjectLoader contains method ParseObjects. This method takes file path of .obj file and bool flag indicating if material file will be loaded. This object loader returns list of FastTriangleMesh and each mesh can be added into the root as a child.

**Note:** Material file is in the .mtl format (Material template library) and it **must** be stored in the same directory as .obj file. 

## Example
Example part of scene script WITH used material loader:

```csharp
...

FilipSedlak_SonaMolnarova.ObjLoader obj = new FilipSedlak_SonaMolnarova.ObjLoader();
List<FastTriangleMesh> meshes = obj.ParseObjects("../data/ice_cube2.obj", true);
foreach(var mesh in meshes)
{
    root.InsertChild(mesh, Matrix4d.Scale(0.2) * Matrix4d.CreateTranslation(1.5, -0.7, -0.5));
}
...
```

Example part of scene script WITHOUT used material loader:

```csharp
...

FilipSedlak_SonaMolnarova.ObjLoader obj = new FilipSedlak_SonaMolnarova.ObjLoader();
List<FastTriangleMesh> meshes = obj.ParseObjects("../data/ice_cube2.obj", false);
foreach(var mesh in meshes)
{    
    root.InsertChild(mesh, Matrix4d.Scale(0.2) * Matrix4d.CreateTranslation(1.5, -0.7, -0.5));
}
...
```
