using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Scene3D;
using Rendering;
using OpenTK;
using System.Globalization;
using System.IO.Compression;

namespace Scene3D
{

  public partial class SceneBrep
  {
    const double lambda = 4;

    List<int>[,,] trianglesGrid;
    public Vector3d bMin, bMax;
    Vector3d gridRes;
    Vector3d cellSize;

    public void Build()
    {
      BuildCornerTable();
      BuildBoundingBox();
      // TODO
      //BuildGrid();
    }

    public void BuildBoundingBox ()
    {
      bMin = new Vector3d(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
      bMax = new Vector3d(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

      for (int i = 0; i < geometry.Count; i++)
      {
        Vector3 v = geometry[i];

        if (v.X < bMin.X)
        { bMin.X = v.X; }
        if (v.Y < bMin.Y)
        { bMin.Y = v.Y; }
        if (v.Z < bMin.Z)
        { bMin.Z = v.Z; }

        if (v.X > bMax.X)
        { bMax.X = v.X; }
        if (v.Y > bMax.Y)
        { bMax.Y = v.Y; }
        if (v.Z > bMax.Z)
        { bMax.Z = v.Z; }
      }

    }

    public void BuildGrid ()
    {
      // Create the grid
      Vector3d gridSize = bMax - bMin;
      double volume = gridSize.X * gridSize.Y * gridSize.Z;
      double coeff = Math.Pow(lambda * Triangles / volume, 1/3.0);


      gridRes = gridSize * coeff;
      cellSize = Vector3d.Divide(gridSize, gridRes);

      Vector3d minCell = Vector3d.Divide(bMin, cellSize);
      Vector3d maxCell = Vector3d.Divide(bMax, cellSize);

      trianglesGrid = new List<int>[(int)gridRes.X + 1, (int)gridRes.Y + 1, (int)gridRes.Z + 1];

      // Fill in the triangles
      for (int x = 0; x < gridRes.X; x++)
      {
      }
    }

    public bool BoundingBoxIntersection (Vector3d ori, Vector3d dir)
    {
      Vector3d invDir = Vector3d.Divide(new Vector3d(1, 1, 1), dir);

      Vector3d t1 = (bMin - ori) * invDir;
      Vector3d t2 = (bMax - ori) * invDir;

      double tx1 = (bMin.X - ori.X)*invDir.X;
      double tx2 = (bMax.X - ori.X)*invDir.X;

      double tmin = Math.Min(tx1, tx2);
      double tmax = Math.Max(tx1, tx2);

      double ty1 = (bMin.Y - ori.Y)*invDir.Y;
      double ty2 = (bMax.Y - ori.Y)*invDir.Y;

      tmin = Math.Max(tmin, Math.Min(ty1, ty2));
      tmax = Math.Min(tmax, Math.Max(ty1, ty2));

      double tz1 = (bMin.Z - ori.Z)*invDir.Z;
      double tz2 = (bMax.Z - ori.Z)*invDir.Z;

      tmin = Math.Max(tmin, Math.Min(tz1, tz2));
      tmax = Math.Min(tmax, Math.Max(tz1, tz2));

      return tmax >= Math.Max(0.0, tmin);
    }
  }
}

namespace FilipSedlak_SonaMolnarova
{
  // Not yet used
  interface ISceneBrep
  {
    int Triangles { get; }
    int Lines { get; }
    float LineWidth { get; }
    int Corners { get; }
    int AddVertex (Vector3 v);
    Vector3 GetVertex (int v);
    void SetVertex (int v, Vector3 pos);
    void SetNormal (int v, Vector3 normal);
    Vector3 GetNormal (int v);
    void SetColor (int v, Vector3 color);
    Vector3 GetColor (int v);
    void SetTxtCoord (int v, Vector2 txt);
    Vector2 GetTxtCoord (int v);
    int AddTriangle (int v1, int v2, int v3);
    int AddLine (int v1, int v2);
    void GetTriangleVertices (int tr, out int v1, out int v2, out int v3);
    void GetLineVertices (int li, out int v1, out int v2);
    void SetTriangleVertices (int tr, int v1, int v2, int v3);
    void GetTriangleVertices (int tr, out Vector3 v1, out Vector3 v2, out Vector3 v3);
    void GetTriangleVertices (int tr, out Vector4 v1, out Vector4 v2, out Vector4 v3);
    void TriangleBoundingBox (int tr, ref Vector3 min, ref Vector3 max);
    void GenerateColors (int seed);
    void ComputeNormals ();
    float GetDiameter (out Vector3 center);
    bool IntersectsBoundingBox (Vector3 ori, Vector3 dir);
  }

  /**/

  /**/

  public class FastTriangleMesh : TriangleMesh, ISolid {
    public FastTriangleMesh (SceneBrep m) : base(m) { }

    public override LinkedList<Intersection> Intersect (Vector3d ori, Vector3d dir)
    {

      if (mesh.BoundingBoxIntersection(ori, dir))
      {
        return base.Intersect(ori, dir);
      }


      return null;
    }

    public new void GetBoundingBox(out Vector3d corner1, out Vector3d corner2)
    {
      corner1 = mesh.bMin;
      corner2 = mesh.bMax;
    }
  }


  public class ObjLoader
  {
    /// <summary>
    /// Parses all objects stored in .obj input file. All corresponding .mtl files have to be in the same directory!
    /// </summary>
    /// <param name="fileName"> File path into input .obj file.</param>
    /// <param name="loadMaterial"> Flag indicating if the material will be loaded or not.</param>
    /// <returns> Array of the parsed and instantiated objects (triangle meshes).</returns>
    public List<FastTriangleMesh> ParseObjects (string fileName, bool loadMaterial)
    {
      if (fileName == null ||
          fileName.Length == 0)
        throw new Exception("Missing file name in ParseMaterial().");

      StreamReader streamReader;
      try
      {
        if (fileName.EndsWith(".gz"))
          streamReader = new StreamReader(new GZipStream(new FileStream(fileName, FileMode.Open),
                                                   CompressionMode.Decompress));
        else
          streamReader = new StreamReader(new FileStream(fileName, FileMode.Open));
      }
      catch (Exception e)
      {
        throw e;
      }

      List<FastTriangleMesh> resultMeshes = new List<FastTriangleMesh>();
      char[] delimiters = {' ', '\t'};
      bool isNamedObject = false;
      Dictionary<string, IMaterial> materials = new Dictionary<string, IMaterial>();
      // Count of all indices, texture coordinates and normals indices.
      int verticesCount = 0;
      int txtCoordsCount = 0;
      int normalsCount = 0;

      // The material has to be in the same directory as obj. object!
      string directoryName = Path.GetDirectoryName(fileName);

      // Find all defined objects in input file and parse them.
      while (!streamReader.EndOfStream)
      {
        // Check if there is defined "o" tag.
        // If there is "v" flag, but no "o" tag was defined before, parse input as one object.
        if ((char)streamReader.Peek() == 'v' && !isNamedObject)
        {
          Tuple<string, FastTriangleMesh> mesh = parseObject(streamReader, ref verticesCount, ref txtCoordsCount, ref normalsCount);
          // If some material was loaded, add to mesh.
          if (materials.Count != 0)
            mesh.Item2.SetAttribute(PropertyName.MATERIAL, materials[mesh.Item1]);
          resultMeshes.Add(mesh.Item2);
          continue;
        }

        string line = streamReader.ReadLine();
        string[] tokens = line.Split();
        // Skip empty line.
        if (tokens.Length == 0)
          continue;
        switch (tokens[0])
        {
          case "mtllib":
            if (!loadMaterial)
              break;

            if (tokens.Length != 2)
              throw new Exception("Incorrect .obj file.");
            // Parse material file, from the same directory as the .obj file.
            materials = ParseMaterial(Path.Combine(directoryName, tokens[1]));
            break;
          case "o":
            isNamedObject = true;
            // If new object definition, parse it.
            Tuple<string, FastTriangleMesh> mesh = parseObject(streamReader, ref verticesCount, ref txtCoordsCount, ref normalsCount);
            // If some material was loaded, add to mesh.
            if (materials.Count != 0)
              mesh.Item2.SetAttribute(PropertyName.MATERIAL, materials[mesh.Item1]);
            resultMeshes.Add(mesh.Item2);
            break;
          default:
            // Skip all other lines.
            break;
        }
      }

      streamReader.Close();
      return resultMeshes;
    }

    public Dictionary<string, IMaterial> ParseMaterial (string fileName)
    {
      if (fileName == null ||
          fileName.Length == 0)
        throw new Exception("Missing file name in ParseMaterial().");

      StreamReader streamReader;
      try
      {
        streamReader = new StreamReader(fileName);
      }
      catch (Exception e)
      {
        throw e;
      }

      // Prepare result dictionary.
      Dictionary<string, IMaterial> resultMaterials = new Dictionary<string, IMaterial>();

      // Find all material definitions in input file and parse them.
      while (!streamReader.EndOfStream)
      {
        string line = streamReader.ReadLine();
        string[] tokens = line.Split();
        // Skip empty line.
        if (tokens.Length == 0)
          continue;
        switch (tokens[0])
        {
          case "#":
            // Skip commentaries.
            continue;
          case "newmtl":
            string materialName;
            // If material name is missing.
            if (tokens.Length == 2)
              materialName = tokens[1];
            else
              throw new Exception("Name of the material is missing in .mtl file:" + fileName);
            // Create new material
            IMaterial material = parseMaterial(streamReader);
            resultMaterials.Add(materialName, material);
            break;
          default:
            // Skip other lines.
            continue;
        }
      }
      streamReader.Close();
      return resultMaterials;
    }

    /// <summary>
    /// Parses one material from input reader.
    /// </summary>
    /// <param name="reader">Reader with current position in .mtl file.</param>
    /// <returns>Parsed material.</returns>
    private IMaterial parseMaterial (StreamReader reader)
    {
      // Set error values, to check if they were filled from the file, if not, throw error.
      double[] baseColor = new double[3] { -1.0f, -1.0f, -1.0f };
      double ambientTerm = -1.0f;
      double diffuseCoef = -1.0f;
      double specularCoef = -1.0f;
      double specularExponent = -1.0f;

      // Parse all needed variables.
      while (!reader.EndOfStream)
      {
        // Check if all variables were filled, if yes, break and return model.
        if (baseColor[0] != -1 &&
            baseColor[1] != -1 &&
            baseColor[2] != -1 &&
            ambientTerm != -1.0f &&
            diffuseCoef != -1.0f &&
            specularCoef != -1.0f &&
            specularExponent != -1.0f)
          break;

        string line = reader.ReadLine();
        string[] tokens = line.Split();

        if (tokens.Length == 0)
          continue;

        switch (tokens[0])
        {
          case "Ka":
            if (tokens.Length != 4)
              throw new Exception("Incorrect .mtl file.");

            // Parse rgb color.
            double r, g, b;
            if (!double.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out r) ||
                !double.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out g) ||
                !double.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out b))
              throw new Exception("Incorrect .mtl file.");

            // Set base color
            baseColor = new double[3] { r, g, b };
            // Set ambient coefficient by averaging r,g,b values.
            ambientTerm = (r + g + b) / 3;
            break;

          case "Kd":
            if (tokens.Length != 4)
              throw new Exception("Incorrect .mtl file.");

            // Parse rgb color.
            if (!double.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out r) ||
                !double.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out g) ||
                !double.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out b))
              throw new Exception("Incorrect .mtl file.");

            // Set diffuse coefficient by averaging r,g,b values.
            diffuseCoef = (r + g + b) / 3;
            break;

          case "Ks":
            if (tokens.Length != 4)
              throw new Exception("Incorrect .mtl file.");

            // Parse rgb color.
            if (!double.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out r) ||
                !double.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out g) ||
                !double.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out b))
              throw new Exception("Incorrect .mtl file.");

            // Set specular coefficient by averaging r,g,b values.
            specularCoef = (r + g + b) / 3;
            break;

          case "Ns":
            if (tokens.Length != 2)
              throw new Exception("Incorrect .mtl file.");

            // Parse specular exponent.
            if (!double.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out specularExponent))
              throw new Exception("Incorrect .mtl file.");
            break;

          default:
            // If other lines, ignore them.
            break;
        }
      }

      // Check if all variables were filled.
      if (baseColor[0] == -1 || baseColor[1] == -1 || baseColor[2] == -1)
        throw new Exception("Incorrect .mtl file.");
      if (ambientTerm == -1.0f ||
          diffuseCoef == -1.0f ||
          specularCoef == -1.0f ||
          specularExponent == -1.0f)
        throw new Exception("Incorrect .mtl file.");

      // Else create PhongMaterial and return it.
      return new PhongMaterial(baseColor, ambientTerm, diffuseCoef, specularCoef, (int)specularExponent);
    }

    /// <summary>
    /// Parses one object from input .obj file.
    /// </summary>
    /// <param name="reader">Reader with current position in .obj file.</param>
    /// <returns>
    /// Triangle mesh with name of the associated material.
    /// If material name is an empty string, no material was used.
    /// </returns>
    private Tuple<string, FastTriangleMesh> parseObject (StreamReader reader, ref int verticesCount, ref int txtCoordsCount, ref int normalsCount)
    {
      SceneBrep scene = new SceneBrep();
      string materialName = "";

      int v0 = scene.Vertices;
      int lastVertex = v0 - 1;

      int faces = 0;

      List<Vector2> txtCoords    = new List<Vector2>(256);
      List<Vector3> normals      = new List<Vector3>(256);
      int           lastTxtCoord = -1;
      int           lastNormal   = -1;
      int[]         f            = new int[3];

      while (!reader.EndOfStream)
      {
        // Check if there is defined "o" tag, if yes, it is the end of definition of one object
        // and return this object.
        if ((char)reader.Peek() == 'o')
        {
          break;
        }

        string line = reader.ReadLine();
        string[] tokens = line.Split();
        if (tokens.Length == 0)
          continue;
        switch (tokens[0])
        {
          case "v":
            if (tokens.Length != 4)
              throw new Exception("Incorrect .obj file.");

            // Add vertex.
            float x, y, z;
            if (!float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !float.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
              throw new Exception("Incorrect .obj file.");

            lastVertex = scene.AddVertex(new Vector3(x, y, z));
            break;

          case "vt":
            if (tokens.Length != 3)
              throw new Exception("Incorrect .obj file.");

            // Add vertex.
            float u, v;
            if (!float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out u) ||
                !float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out v))
              throw new Exception("Incorrect .obj file.");

            txtCoords.Add(new Vector2(u, v));
            ++lastTxtCoord;
            break;

          case "vn":
            if (tokens.Length != 4)
              throw new Exception("Incorrect .obj file.");

            // Add vertex.
            float nx, ny, nz;
            if (!float.TryParse(tokens[1], NumberStyles.Float, CultureInfo.InvariantCulture, out nx) ||
                !float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out ny) ||
                !float.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out nz))
              throw new Exception("Incorrect .obj file.");

            normals.Add(new Vector3(nx, ny, nz));
            break;

          case "usemtl":
            // Set name of the material used for this object.
            if (tokens.Length != 2)
              throw new Exception("Incorrect .obj file.");
            materialName = tokens[1];
            break;

          case "f":
            // Face must be formed by at least three vertices.
            if (tokens.Length < 4)
              continue;

            // Number of vertices.
            int N = tokens.Length - 1;
            // Reuse same array for each face and resize it if needed.
            if (f.Length < N)
              f = new int[N];
            // Shrink it if less vertices is used.
            if (N < f.Length)
              f = new int[N];

            int i;

            for (i = 0; i < N; i++) // read indices for one vertex
            {
              string[] vt = tokens[i + 1].Split('/');
              int      ti, ni;
              ti = ni = 0; // 0 => value not present

              // 0 .. vertex coord index
              if (!int.TryParse(vt[0], out f[i]) ||
                   f[i] == 0)
                break;

              if (f[i] > 0)
                f[i] = v0 + f[i] - 1 - verticesCount;
              else
                f[i] = lastVertex + 1 - f[i];

              if (vt.Length > 1)
              {
                // 1 .. texture coord index (not yet)
                if (!int.TryParse(vt[1], out ti))
                  ti = 0;

                if (vt.Length > 2)
                {
                  // 2 .. normal vector index
                  if (!int.TryParse(vt[2], out ni))
                    ni = 0;
                }
              }

              // there was a texture coord..
              if (ti != 0)
              {
                if (ti > 0)
                  ti = ti - txtCoordsCount - 1;
                else
                  ti = lastTxtCoord + 1 - ti;
                if (ti >= 0 && ti < txtCoords.Count)
                  scene.SetTxtCoord(f[i], txtCoords[ti]);
              }

              // there was a normal..
              if (ni != 0)
              {
                if (ni > 0)
                  ni = ni - normalsCount - 1;
                else
                  ni = lastNormal + 1 - ni;
                if (ni >= 0 && ni < normals.Count)
                  scene.SetNormal(f[i], normals[ni]);
              }
            }

            N = i;
            for (i = 1; i < N - 1; i++)
            {
              scene.AddTriangle(f[0], f[i], f[i + 1]);
              faces++;
            }

            break;
        }
      }

      // Update count of vertices, txt coords and normals.
      verticesCount += scene.Vertices;
      txtCoordsCount += txtCoords.Count;
      normalsCount += normals.Count;

      scene.BuildCornerTable();
      scene.BuildBoundingBox();

      return new Tuple<string, FastTriangleMesh>(materialName, new FastTriangleMesh(scene));
    }
  }
}
