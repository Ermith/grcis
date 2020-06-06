using Rendering;

namespace FilipSedlak_SonaMolnarova
{
  static public class AdditionalMaterials
  {
    public static IMaterial IceMaterial()
    {
      var pm = new PhongMaterial(new double[]{ 0.5, 0.9, 1.0 }, 0.05, 0.2, 0.2, 4, 0.0);
      pm.n = 1.01;
      pm.Kt = 1.18;
      return pm;
    }
  }
}
