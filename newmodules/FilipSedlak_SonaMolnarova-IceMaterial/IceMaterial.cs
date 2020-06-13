using Rendering;

namespace FilipSedlak_SonaMolnarova
{
  static public class AdditionalMaterials
  {
    public static IMaterial IceMaterial()
    {
      var pm = new PhongMaterial(new double[]{ 0.6, 0.9, 0.9 }, 0.13, 0.003, 0.003, 1, 0.00);
      pm.n = 1.065;
      pm.Kt = 0.89;
      return pm;
    }
  }
}
