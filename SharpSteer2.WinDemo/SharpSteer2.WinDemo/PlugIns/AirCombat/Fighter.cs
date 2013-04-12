using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SharpSteer2.Database;

namespace SharpSteer2.WinDemo.PlugIns.AirCombat
{
    class Fighter
        :SimpleVehicle
    {
        private readonly Trail _trail;
        private readonly ITokenForProximityDatabase<IVehicle> _proximityToken;

        public Fighter Enemy { get; set; }
        private readonly List<IVehicle> _neighbours = new List<IVehicle>();

        public override float MaxForce
        {
            get { return 10; }
        }
        public override float MaxSpeed
        {
            get { return 20; }
        }

        public const float WORLD_RADIUS = 30;

        private float _lastFired = -100;
        private const float REFIRE_TIME = 2f;
        private readonly Action<Fighter, Fighter> _fireMissile;

        public Color Color = Color.White;

        public Fighter(IProximityDatabase<IVehicle> proximity, IAnnotationService annotation, Action<Fighter, Fighter> fireMissile)
            :base(annotation)
        {
            _trail = new Trail(20, 200)
            {
                TrailColor = Color.WhiteSmoke,
                TickColor = Color.LightGray
            };
            _proximityToken = proximity.AllocateToken(this);

            _fireMissile = fireMissile;
        }

        public void Update(float currentTime, float elapsedTime)
        {
            _trail.Record(currentTime, Position);

            _neighbours.Clear();

            if (Vector3.Dot(Vector3.Normalize(Enemy.Position - Position), Forward) > 0.7f)
            {
                if (currentTime - _lastFired > REFIRE_TIME)
                {
                    _fireMissile(this, Enemy);
                    _lastFired = currentTime;
                }
            }

            Vector3 enemyPlaneForce;
            if ((Enemy.Position - Position).Length() < 3)
                enemyPlaneForce = SteerForEvasion(Enemy, 1);
            else
                enemyPlaneForce = SteerForPursuit(Enemy, 1);
            var boundary = HandleBoundary();

            _neighbours.Clear();
            _proximityToken.FindNeighbors(Position, 10, _neighbours);
            var evasion = _neighbours
                .Where(v => v is Missile)
                .Cast<Missile>()
                .Where(m => m.Target == this)
                .Select(m => SteerForEvasion(m, 1))
                .Aggregate(Vector3.Zero, (a, b) => a + b);

            ApplySteeringForce(enemyPlaneForce + boundary + evasion, elapsedTime);

            _proximityToken.UpdateForNewPosition(Position);
        }

        private Vector3 HandleBoundary()
        {
            // while inside the sphere do noting
            if (Position.Length() < WORLD_RADIUS)
                return Vector3.Zero;

            // steer back when outside
            Vector3 seek = SteerForSeek(Vector3.Zero);
            Vector3 lateral = Vector3Helpers.PerpendicularComponent(seek, Forward);
            return lateral;

        }

        public void Draw()
        {
            _trail.Draw(annotation);
            Drawing.DrawBasic3dSphericalVehicle(this, Color);
        }
    }
}
