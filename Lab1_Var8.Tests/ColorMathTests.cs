using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lab1_Var8.Core;

namespace Lab1_Var8.Tests
{
    [TestClass]
    public class ColorMathTests
    {

        [TestMethod]
        public void RgbToXyz_PureRed_D65_MatchesExpected()
        {
            var (x, y, z) = ColorMath.RgbToXyz(255, 0, 0, Illuminant.D65);

            Assert.AreEqual(41.246, x, 0.01);
            Assert.AreEqual(21.267, y, 0.01);
            Assert.AreEqual(1.933, z, 0.01);
        }

        [TestMethod]
        public void RgbToXyz_White_MatchesReferenceWhitePoint_ForEachIlluminant()
        {
            foreach (var illuminant in new[] { Illuminant.D65, Illuminant.D50, Illuminant.E })
            {
                var (x, y, z) = ColorMath.RgbToXyz(255, 255, 255, illuminant);
                var white = IlluminantData.GetWhitePoint(illuminant);

                Assert.AreEqual(white.X, x, 0.01, $"X for {illuminant}");
                Assert.AreEqual(white.Y, y, 0.01, $"Y for {illuminant}");
                Assert.AreEqual(white.Z, z, 0.01, $"Z for {illuminant}");
            }
        }

        [TestMethod]
        public void RgbToXyz_DifferentIlluminants_GiveDifferentResults()
        {
            var d65 = ColorMath.RgbToXyz(200, 100, 50, Illuminant.D65);
            var d50 = ColorMath.RgbToXyz(200, 100, 50, Illuminant.D50);


            Assert.AreNotEqual(d65.X, d50.X, 0.001);
        }

        [TestMethod]
        public void RoundTrip_RgbToXyzToRgb_D65_ReturnsOriginalColor()
        {
            byte[,] samples = { { 10, 20, 30 }, { 200, 50, 120 }, { 0, 0, 0 }, { 255, 255, 255 } };

            for (int i = 0; i < samples.GetLength(0); i++)
            {
                byte r = samples[i, 0], g = samples[i, 1], b = samples[i, 2];

                var (x, y, z) = ColorMath.RgbToXyz(r, g, b, Illuminant.D65);
                var (r2, g2, b2, _) = ColorMath.XyzToRgb(x, y, z, Illuminant.D65);

                Assert.AreEqual(r, r2, 1, $"R roundtrip for ({r},{g},{b})");
                Assert.AreEqual(g, g2, 1, $"G roundtrip for ({r},{g},{b})");
                Assert.AreEqual(b, b2, 1, $"B roundtrip for ({r},{g},{b})");
            }
        }

        [TestMethod]
        public void RgbToHsv_PureRed_Returns0_100_100()
        {
            var (h, s, v) = ColorMath.RgbToHsv(255, 0, 0);
            Assert.AreEqual(0, h, 0.01);
            Assert.AreEqual(100, s, 0.01);
            Assert.AreEqual(100, v, 0.01);
        }

        [TestMethod]
        public void RgbToHsv_PureGreen_Returns120_100_100()
        {
            var (h, s, v) = ColorMath.RgbToHsv(0, 255, 0);
            Assert.AreEqual(120, h, 0.01);
            Assert.AreEqual(100, s, 0.01);
            Assert.AreEqual(100, v, 0.01);
        }

        [TestMethod]
        public void RgbToHsv_White_HasZeroSaturation()
        {
            var (_, s, v) = ColorMath.RgbToHsv(255, 255, 255);
            Assert.AreEqual(0, s, 0.01);
            Assert.AreEqual(100, v, 0.01);
        }

        [TestMethod]
        public void HsvToRgb_KnownValues_MatchExpectedRgb()
        {
            var red = ColorMath.HsvToRgb(0, 100, 100);
            Assert.AreEqual((255, (byte)0, (byte)0), (red.R, red.G, red.B));

            var mixed = ColorMath.HsvToRgb(30, 50, 80);
            Assert.AreEqual(204, mixed.R);
            Assert.AreEqual(153, mixed.G);
            Assert.AreEqual(102, mixed.B);
        }

        [TestMethod]
        public void RoundTrip_RgbToHsvToRgb_ReturnsOriginalColor()
        {
            byte[,] samples = { { 128, 64, 32 }, { 10, 200, 90 }, { 0, 0, 0 }, { 255, 255, 255 } };

            for (int i = 0; i < samples.GetLength(0); i++)
            {
                byte r = samples[i, 0], g = samples[i, 1], b = samples[i, 2];

                var (h, s, v) = ColorMath.RgbToHsv(r, g, b);
                var (r2, g2, b2) = ColorMath.HsvToRgb(h, s, v);

                Assert.AreEqual(r, r2, 1);
                Assert.AreEqual(g, g2, 1);
                Assert.AreEqual(b, b2, 1);
            }
        }

        [TestMethod]
        public void ClipToRgb_OutOfRangeValues_ClampsEachChannelIndependently()
        {
            var (r, g, b, warn) = ColorMath.ClipToRgb(300, -10, 100);
            Assert.AreEqual(255, r);
            Assert.AreEqual(0, g);
            Assert.AreEqual(100, b);
            Assert.IsTrue(warn);
        }

        [TestMethod]
        public void ClipToRgb_ValueInRange_ReturnsWithoutWarning()
        {
            var (r, g, b, warn) = ColorMath.ClipToRgb(120, 60, 200);
            Assert.AreEqual(120, r);
            Assert.AreEqual(60, g);
            Assert.AreEqual(200, b);
            Assert.IsFalse(warn);
        }
    }
}
