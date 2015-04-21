using Microsoft.Xna.Framework;
using SharpSteer2.Pathway;

namespace SharpSteer2.WinDemo.PlugIns.MeshPathFollowing
{
    public class PathWalker
        :SimpleVehicle
    {
        public readonly IPathway Path;

        public override float MaxForce { get { return 1; } }
        public override float MaxSpeed { get { return 8; } }

        public PathWalker(IPathway path, IAnnotationService annotation)
            :base(annotation)
        {
            Path = path;
        }

        public void Update(float dt)
        {
            ApplySteeringForce(SteerToFollowPath(true, 3, Path), dt);

            annotation.VelocityAcceleration(this);
        }

        internal void Draw()
        {
            Drawing.DrawBasic2dCircularVehicle(this, Color.Gray);
        }
    }
}
