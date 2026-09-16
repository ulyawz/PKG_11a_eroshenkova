using System;

namespace Lab1_Var8.Core
{
    internal static class MatrixMath
    {
        public static double[,] Invert3x3(double[,] m)
        {
            double a = m[0, 0], b = m[0, 1], c = m[0, 2];
            double d = m[1, 0], e = m[1, 1], f = m[1, 2];
            double g = m[2, 0], h = m[2, 1], i = m[2, 2];

            double A = e * i - f * h;
            double B = -(d * i - f * g);
            double C = d * h - e * g;
            double D = -(b * i - c * h);
            double E = a * i - c * g;
            double F = -(a * h - b * g);
            double G = b * f - c * e;
            double H = -(a * f - c * d);
            double I = a * e - b * d;

            double det = a * A + b * B + c * C;
            if (Math.Abs(det) < 1e-12)
                throw new InvalidOperationException("Матрица вырождена — обращение невозможно.");

            double inv = 1.0 / det;
            return new double[,]
            {
                { A * inv, D * inv, G * inv },
                { B * inv, E * inv, H * inv },
                { C * inv, F * inv, I * inv }
            };
        }

        public static double[] MultiplyVector(double[,] m, double[] v)
        {
            return new[]
            {
                m[0, 0] * v[0] + m[0, 1] * v[1] + m[0, 2] * v[2],
                m[1, 0] * v[0] + m[1, 1] * v[1] + m[1, 2] * v[2],
                m[2, 0] * v[0] + m[2, 1] * v[1] + m[2, 2] * v[2]
            };
        }
    }
}
