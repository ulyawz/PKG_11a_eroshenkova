using System;

namespace Lab1_Var8.Core
{
   
    public static class ColorMath
    {
        public static double[,] BuildRgbToXyzMatrix(Illuminant illuminant)
        {
            var (xr, yr) = RgbPrimaries.Red;
            var (xg, yg) = RgbPrimaries.Green;
            var (xb, yb) = RgbPrimaries.Blue;

            double Xr = xr / yr, Yr = 1.0, Zr = (1 - xr - yr) / yr;
            double Xg = xg / yg, Yg = 1.0, Zg = (1 - xg - yg) / yg;
            double Xb = xb / yb, Yb = 1.0, Zb = (1 - xb - yb) / yb;

            var white = IlluminantData.GetWhitePoint(illuminant);
            double xw = white.X / 100.0, yw = white.Y / 100.0, zw = white.Z / 100.0;

            var primariesMatrix = new double[,]
            {
                { Xr, Xg, Xb },
                { Yr, Yg, Yb },
                { Zr, Zg, Zb }
            };

            var inv = MatrixMath.Invert3x3(primariesMatrix);
            var s = MatrixMath.MultiplyVector(inv, new[] { xw, yw, zw });

            return new double[,]
            {
                { s[0] * Xr, s[1] * Xg, s[2] * Xb },
                { s[0] * Yr, s[1] * Yg, s[2] * Yb },
                { s[0] * Zr, s[1] * Zg, s[2] * Zb }
            };
        }

        public static (double X, double Y, double Z) RgbToXyz(byte r, byte g, byte b, Illuminant illuminant)
        {
            var m = BuildRgbToXyzMatrix(illuminant);

            double rn = InverseGamma(r / 255.0);
            double gn = InverseGamma(g / 255.0);
            double bn = InverseGamma(b / 255.0);

            double X = (m[0, 0] * rn + m[0, 1] * gn + m[0, 2] * bn) * 100.0;
            double Y = (m[1, 0] * rn + m[1, 1] * gn + m[1, 2] * bn) * 100.0;
            double Z = (m[2, 0] * rn + m[2, 1] * gn + m[2, 2] * bn) * 100.0;

            return (X, Y, Z);
        }

        private static double InverseGamma(double x)
        {
            return x >= 0.04045 ? Math.Pow((x + 0.055) / 1.055, 2.4) : x / 12.92;
        }

        public static (byte R, byte G, byte B, bool OutOfGamut) XyzToRgb(
            double X, double Y, double Z, Illuminant illuminant)
        {
            var m = BuildRgbToXyzMatrix(illuminant);
            var inv = MatrixMath.Invert3x3(m);

            double xn = X / 100.0, yn = Y / 100.0, zn = Z / 100.0;

            double rn = inv[0, 0] * xn + inv[0, 1] * yn + inv[0, 2] * zn;
            double gn = inv[1, 0] * xn + inv[1, 1] * yn + inv[1, 2] * zn;
            double bn = inv[2, 0] * xn + inv[2, 1] * yn + inv[2, 2] * zn;

            double r = Gamma(rn) * 255.0;
            double g = Gamma(gn) * 255.0;
            double b = Gamma(bn) * 255.0;

            return ClipToRgb(r, g, b);
        }

        private static double Gamma(double x)
        {
            return x >= 0.0031308 ? 1.055 * Math.Pow(x, 1.0 / 2.4) - 0.055 : 12.92 * x;
        }


        public static (byte R, byte G, byte B, bool OutOfGamut) ClipToRgb(double r, double g, double b)
        {
            bool outOfGamut = r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255;
            return (ClampByte(r), ClampByte(g), ClampByte(b), outOfGamut);
        }

        private static byte ClampByte(double v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)Math.Round(v);
        }

        public static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
        {
            double rn = r / 255.0, gn = g / 255.0, bn = b / 255.0;

            double max = Math.Max(rn, Math.Max(gn, bn));
            double min = Math.Min(rn, Math.Min(gn, bn));
            double delta = max - min;

            double V = max;
            double S = max == 0 ? 0 : delta / max;

            double H;
            if (delta == 0)
                H = 0;
            else if (max == rn)
                H = 60 * (((gn - bn) / delta) % 6);
            else if (max == gn)
                H = 60 * (((bn - rn) / delta) + 2);
            else
                H = 60 * (((rn - gn) / delta) + 4);

            if (H < 0) H += 360;

            return (H, S * 100.0, V * 100.0);
        }

        public static (byte R, byte G, byte B) HsvToRgb(double H, double sPercent, double vPercent)
        {
            double S = Clamp01(sPercent / 100.0);
            double V = Clamp01(vPercent / 100.0);

            if (S == 0)
            {
                byte gray = (byte)Math.Round(V * 255.0);
                return (gray, gray, gray);
            }

            double h = H;
            if (h >= 360) h = 0;
            h = h / 60.0;

            int i = (int)Math.Floor(h);
            double f = h - i;

            double M = V * (1 - S);
            double N = V * (1 - S * f);
            double K = V * (1 - S * (1 - f));

            double r, g, b;
            switch (i)
            {
                case 0: r = V; g = K; b = M; break;
                case 1: r = N; g = V; b = M; break;
                case 2: r = M; g = V; b = K; break;
                case 3: r = M; g = N; b = V; break;
                case 4: r = K; g = M; b = V; break;
                default: r = V; g = M; b = N; break; 
            }

            return ((byte)Math.Round(Clamp01(r) * 255),
                    (byte)Math.Round(Clamp01(g) * 255),
                    (byte)Math.Round(Clamp01(b) * 255));
        }

        private static double Clamp01(double x) => x < 0 ? 0 : (x > 1 ? 1 : x);
    }
}
