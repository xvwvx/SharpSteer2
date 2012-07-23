using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework;

namespace SharpSteer2.Tests
{
    [TestClass]
    public class BruteForceProximityDatabaseTest
    {
        [TestMethod]
        public void Construct()
        {
            var db = new BruteForceProximityDatabase<object>();

            Assert.AreEqual(0, db.Count);
        }

        [TestMethod]
        public void AllocateToken()
        {
            var db = new BruteForceProximityDatabase<object>();

            var obj = new object();
            var token = db.AllocateToken(obj);

            Assert.AreEqual(1, db.Count);

            token.UpdateForNewPosition(Vector3.Zero);
        }

        private ITokenForProximityDatabase<object> CreateToken(BruteForceProximityDatabase<object> db, Vector3 position, Dictionary<object, Vector3> lookup)
        {
            var obj = new object();

            lookup.Add(obj, position);

            var token = db.AllocateToken(obj);
            token.UpdateForNewPosition(position);

            return token;
        }

        [TestMethod]
        public void LocateNeighbours()
        {
            var db = new BruteForceProximityDatabase<object>();

            Dictionary<object, Vector3> positionLookup = new Dictionary<object, Vector3>();

            var x0y0z0 = CreateToken(db, new Vector3(0, 0, 0), positionLookup);
            var x1y0z0 = CreateToken(db, new Vector3(1, 0, 0), positionLookup);
            var x3y0z0 = CreateToken(db, new Vector3(3, 0, 0), positionLookup);

            var list = new List<object>();
            x0y0z0.FindNeighbors(Vector3.Zero, 2, list);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(new Vector3(0, 0, 0), positionLookup[list[0]]);
            Assert.AreEqual(new Vector3(1, 0, 0), positionLookup[list[1]]);

            //Check tokens handle being disposed twice
            x1y0z0.Dispose();
            x1y0z0.Dispose();

            //Check tokens handle being collected after being disposed
            GC.Collect();
            GC.WaitForPendingFinalizers();

            list.Clear();
            x0y0z0.FindNeighbors(Vector3.Zero, 2, list);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(new Vector3(0, 0, 0), positionLookup[list[0]]);
        }
    }
}
