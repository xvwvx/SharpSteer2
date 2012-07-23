using SharpSteer2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.Xna.Framework;

namespace SharpSteer2.Tests
{
    /// <summary>
    ///This is a test class for UtilitiesTest and is intended
    ///to contain all UtilitiesTest Unit Tests
    ///</summary>
    [TestClass]
    public class UtilitiesTest
    {
        [TestMethod]
        private void Random()
        {
        }

        [TestMethod]
        private void ScalarRandomWalk()
        {
            var rand = Utilities.ScalarRandomWalk(0, 10, -5, 5);

            Assert.IsTrue(rand >= -5);
            Assert.IsTrue(rand <= 5);
        }

        [TestMethod]
        private void BoundedRandom()
        {
            const int lower = -17;
            const int upper = 24;

            var rand = Utilities.Random(lower, upper);
            Assert.IsTrue(lower <= rand);
            Assert.IsTrue(upper >= rand);
        }

        [TestMethod]
        private void RemapIntervalChangeUpperBound()
        {
            var a = 0;
            var b = 10;

            var c = 0;
            var d = 20;

            var x = 5;

            Assert.AreEqual(10, Utilities.RemapInterval(x, a, b, c, d));
        }

        [TestMethod]
        private void RemapIntervalChangeLowerBound()
        {
            var a = 0;
            var b = 10;

            var c = -10;
            var d = 10;

            var x = 5;

            Assert.AreEqual(0, Utilities.RemapInterval(x, a, b, c, d));
        }

        [TestMethod]
        private void RemapIntervalChangeBothBounds()
        {
            var a = 0;
            var b = 10;

            var c = -20;
            var d = 40;

            var x = 5;

            Assert.AreEqual(10, Utilities.RemapInterval(x, a, b, c, d));
        }

        [TestMethod]
        private void RemapIntervalBeyondBound()
        {
            var a = 0;
            var b = 10;

            var c = 0;
            var d = 20;

            var x = 20;

            Assert.AreEqual(40, Utilities.RemapInterval(x, a, b, c, d));
        }

        [TestMethod]
        private void RemapIntervalClip()
        {
            var a = 0;
            var b = 10;

            var c = 0;
            var d = 20;

            var x = 20;

            Assert.AreEqual(20, Utilities.RemapIntervalClip(x, a, b, c, d));
        }

        [TestMethod]
        private void IntervalComparison()
        {
            Assert.AreEqual(-1, Utilities.IntervalComparison(0, 1, 2));
            Assert.AreEqual(0, Utilities.IntervalComparison(1.5f, 1, 2));
            Assert.AreEqual(+1, Utilities.IntervalComparison(3, 1, 2));
        }

        [TestMethod]
        private void Square()
        {
            Assert.AreEqual(4, Utilities.Square(2));
        }

        [TestMethod]
        private void FloatBlendIntoAccumulator()
        {
            float smoothedValue = 1;
            Utilities.BlendIntoAccumulator(0.5f, 2, ref smoothedValue);

            Assert.AreEqual(MathHelper.Lerp(1, 2, 0.5f), smoothedValue);
        }

        [TestMethod]
        private void Vector3BlendIntoAccumulator()
        {
            Vector3 smoothedValue = Vector3.One;
            Utilities.BlendIntoAccumulator(0.5f, new Vector3(2, 2, 2), ref smoothedValue);

            Assert.AreEqual(Vector3.Lerp(Vector3.One, new Vector3(2), 0.5f), smoothedValue);
        }
    }
}
