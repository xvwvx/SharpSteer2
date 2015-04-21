using Microsoft.Xna.Framework;
using SharpSteer2.Pathway;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpSteer2.WinDemo.PlugIns.MeshPathFollowing
{
    public class MeshPathFollowingPlugin
        :PlugIn
    {
        private TrianglePathway _path;
        private PathWalker _walker;

        public override bool RequestInitialSelection
        {
            get
            {
                return true;
            }
        }

        public MeshPathFollowingPlugin(IAnnotationService annotations)
            :base(annotations)
        {
        }

        public override void Open()
        {
            GeneratePath();
            _walker = new PathWalker(_path, Annotations) {
                Position = new Vector3(10, 0, 0)
            };
        }

        private void GeneratePath()
        {
            var rand = new Random();

            float xOffsetDeriv = 0;
            float xOffset = 0;

            var points = new List<Vector3>();
            for (var i = 0; i < 200; i++)
            {
                xOffsetDeriv = MathHelper.Clamp((float)rand.NextDouble() - (xOffsetDeriv * 0.0125f), -15, 15);
                xOffset += xOffsetDeriv;

                points.Add(new Vector3(xOffset + 1, 0, i));
                points.Add(new Vector3(xOffset - 1, 0, i));
            }

            _path = new TrianglePathway(points);
        }

        public override void Update(float currentTime, float elapsedTime)
        {
            _walker.Update(elapsedTime);
        }

        public override void Redraw(float currentTime, float elapsedTime)
        {
            Demo.UpdateCamera(elapsedTime, _walker);
            _walker.Draw();

            var tri = _path.Triangles.ToArray();
            for (int i = 0; i < tri.Length; i++)
            {
                var triangle = tri[i];

                Drawing.Draw2dLine(triangle.A, triangle.A + triangle.Edge0, Color.Black);
                Drawing.Draw2dLine(triangle.A, triangle.A + triangle.Edge1, Color.Black);
                Drawing.Draw2dLine(triangle.A + triangle.Edge0, triangle.A + triangle.Edge1, Color.Black);
            }

            float o;
            Vector3 t;
            var pop = _walker.Path.MapPointToPath(_walker.Position, out t, out o);
            Drawing.Draw3dCircle(0.1f, pop, Vector3.Up, Color.Red, 5);
        }

        public override void Close()
        {
            
        }

        public override string Name
        {
            get { return "Nav Mesh Path Following"; }
        }

        public override IEnumerable<IVehicle> Vehicles
        {
            get { yield return _walker; }
        }
    }
}
