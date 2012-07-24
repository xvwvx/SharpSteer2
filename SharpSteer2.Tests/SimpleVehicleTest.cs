using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace SharpSteer2.Tests
{
    [TestClass]
    public class SimpleVehicleTest
    {
        [TestMethod]
        public void Construct()
        {
            SimpleVehicle v = new SimpleVehicle();

            Assert.AreEqual(Vector3.Zero, v.Acceleration);
            Assert.AreEqual(Vector3.Backward, v.Forward);
            Assert.AreEqual(Vector3.Zero, v.Velocity);
            Assert.AreEqual(0, v.Speed);
            Assert.AreEqual(Vector3.Zero, v.SmoothedPosition);
        }
    }
}
