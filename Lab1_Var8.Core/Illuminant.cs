using System;

namespace Lab1_Var8.Core
{
    public enum Illuminant
    {
        D65, 
        D50, 
        E    
    }

    public static class IlluminantData
    {
        public static (double X, double Y, double Z) GetWhitePoint(Illuminant illuminant)
        {
            switch (illuminant)
            {
                case Illuminant.D65: return (95.047, 100.000, 108.883);
                case Illuminant.D50: return (96.422, 100.000, 82.521);
                case Illuminant.E: return (100.000, 100.000, 100.000);
                default: throw new ArgumentOutOfRangeException(nameof(illuminant));
            }
        }
    }
}
