using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SharpSteer2.Pathway;

namespace SharpSteer2.WinDemo.PlugIns.MeshPathFollowing
{
    public class PathWalker
        :SimpleVehicle
    {
        private readonly BasePathway _path;

        public override float MaxForce { get { return 32; } }
        public override float MaxSpeed { get { return 4; } }

        public PathWalker(BasePathway path)
        {
            _path = path;
        }

        public void Update(float dt)
        {
            ApplySteeringForce(SteerToFollowPath(1, 3, _path), dt);
        }

        internal void Draw()
        {
            Drawing.DrawBasic2dCircularVehicle(this, Color.Gray);
        }
    }
}
