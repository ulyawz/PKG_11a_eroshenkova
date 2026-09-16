using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lab1_Var8.Core;

namespace Lab1_Var8.Tests
{
    [TestClass]
    public class ColorModelTests
    {

        [TestMethod]
        public void SetFromRgb_UpdatesXyzAndHsvTogether()
        {
            var model = new ColorModel();
            model.SetFromRgb(255, 0, 0);

            Assert.AreEqual(41.246, model.X, 0.01);
            Assert.AreEqual(0, model.H, 0.01);
            Assert.AreEqual(100, model.S, 0.01);
        }

        [TestMethod]
        public void ChangingIlluminant_RecalculatesXyz_ButKeepsRgbUnchanged()
        {
            var model = new ColorModel();
            model.SetFromRgb(200, 100, 50);
            double xBefore = model.X;

            model.SetIlluminant(Illuminant.D50);

            Assert.AreEqual(200, model.R);
            Assert.AreEqual(100, model.G);
            Assert.AreEqual(50, model.B);
            Assert.AreNotEqual(xBefore, model.X, 0.001);
        }

        [TestMethod]
        public void SetFromXyz_OutOfGamutValue_RaisesGamutWarning()
        {
            var model = new ColorModel();

            model.SetFromXyz(10, 300, 10);

            Assert.IsTrue(model.LastGamutWarning);
        }

        [TestMethod]
        public void SetFromHsv_UpdatesRgbAndXyzConsistently()
        {
            var model = new ColorModel();
            model.SetFromHsv(120, 100, 100); 

            Assert.AreEqual(0, model.R);
            Assert.AreEqual(255, model.G);
            Assert.AreEqual(0, model.B);
        }
    }
}
