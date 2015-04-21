using Microsoft.Xna.Framework;
using SharpSteer2.Pathway;

namespace SharpSteer2.WinDemo.PlugIns.MeshPathFollowing
{
    public class PathWalker
        :SimpleVehicle
    {
        public readonly IPathway Path;

        public override float MaxForce { get { return 1; } }
        public override float MaxSpeed { get { return 4; } }

        private readonly Trail _trail = new Trail(30, 300);

        public PathWalker(IPathway path, IAnnotationService annotation)
            :base(annotation)
        {
            Path = path;
        }

        private float _time;
        public void Update(float dt)
        {
            const float PREDICTION = 3;

            var f = SteerToFollowPath(true, PREDICTION, Path);
            ApplySteeringForce(f, dt);

            _time += dt;
            _trail.Record(_time, Position);

            annotation.VelocityAcceleration(this);
        }

        internal void Draw()
        {
            Drawing.DrawBasic2dCircularVehicle(this, Color.Gray);

            _trail.Draw(annotation);
        }
    }
}
