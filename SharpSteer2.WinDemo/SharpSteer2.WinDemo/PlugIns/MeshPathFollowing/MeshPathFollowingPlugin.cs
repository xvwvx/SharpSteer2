using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SharpSteer2.Pathway;

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
            _walker = new PathWalker(_path, Annotations);
        }

        private void GeneratePath()
        {
            var rand = new Random();

            float xOffsetDeriv = 0;
            float xOffset = 0;

            var points = new List<Vector3>();
            for (var i = 0; i < 100; i++)
            {
                xOffsetDeriv += (float)rand.NextDouble() - (xOffsetDeriv * xOffsetDeriv * 0.025f);
                xOffset += xOffsetDeriv;

                points.Add(new Vector3(xOffset - 3, 0, i));
                points.Add(new Vector3(xOffset + 3, 0, i));
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

            float o;
            Vector3 t;
            var pop = _path.MapPointToPath(_walker.Position, out t, out o);
            Drawing.Draw3dCircle(1, pop, Vector3.Up, Color.Red, 5);

            foreach (var triangle in _path.Triangles)
            {
                Drawing.Draw2dLine(triangle.A, triangle.A + triangle.Edge0, Color.Black);
                Drawing.Draw2dLine(triangle.A, triangle.A + triangle.Edge1, Color.Black);
                Drawing.Draw2dLine(triangle.A + triangle.Edge0, triangle.A + triangle.Edge1, Color.Black);
            }
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
