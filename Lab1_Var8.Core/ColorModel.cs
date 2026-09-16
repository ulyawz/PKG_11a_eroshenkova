using System;

namespace Lab1_Var8.Core
{

    public enum ColorSource { Rgb, Xyz, Hsv, Settings }

    public class ColorChangedEventArgs : EventArgs
    {
        public byte R, G, B;
        public double X, Y, Z;
        public double H, S, V;
        public bool GamutWarning;
        public ColorSource Source;
    }

    public class ColorModel
    {
        public Illuminant Illuminant { get; private set; } = Illuminant.D65;

        public byte R { get; private set; }
        public byte G { get; private set; }
        public byte B { get; private set; }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public double H { get; private set; }
        public double S { get; private set; }
        public double V { get; private set; }

        public bool LastGamutWarning { get; private set; }

        public event EventHandler<ColorChangedEventArgs> ColorChanged;

        public ColorModel()
        {
            SetFromRgb(255, 128, 0);
        }

        public void SetIlluminant(Illuminant illuminant)
        {
            Illuminant = illuminant;
            RecalcFromCurrentRgb(ColorSource.Settings);
        }

        public void SetFromRgb(byte r, byte g, byte b)
        {
            R = r; G = g; B = b;
            (X, Y, Z) = ColorMath.RgbToXyz(r, g, b, Illuminant);
            (H, S, V) = ColorMath.RgbToHsv(r, g, b);
            LastGamutWarning = false;
            Notify(ColorSource.Rgb);
        }

        public void SetFromXyz(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
            var (r, g, b, warn) = ColorMath.XyzToRgb(x, y, z, Illuminant);
            R = r; G = g; B = b;
            (H, S, V) = ColorMath.RgbToHsv(r, g, b);
            LastGamutWarning = warn;
            Notify(ColorSource.Xyz);
        }

        public void SetFromHsv(double h, double s, double v)
        {
            H = h; S = s; V = v;
            var (r, g, b) = ColorMath.HsvToRgb(h, s, v);
            R = r; G = g; B = b;
            (X, Y, Z) = ColorMath.RgbToXyz(r, g, b, Illuminant);
            LastGamutWarning = false;
            Notify(ColorSource.Hsv);
        }

        private void RecalcFromCurrentRgb(ColorSource source)
        {
            (X, Y, Z) = ColorMath.RgbToXyz(R, G, B, Illuminant);
            (H, S, V) = ColorMath.RgbToHsv(R, G, B);
            LastGamutWarning = false;
            Notify(source);
        }

        private void Notify(ColorSource source)
        {
            ColorChanged?.Invoke(this, new ColorChangedEventArgs
            {
                R = R, G = G, B = B,
                X = X, Y = Y, Z = Z,
                H = H, S = S, V = V,
                GamutWarning = LastGamutWarning,
                Source = source
            });
        }
    }
}
