using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SharpSteer2.Database;
using SharpSteer2.Helpers;
using SharpSteer2.WinDemo.PlugIns.Boids;

namespace SharpSteer2.WinDemo.PlugIns.AirCombat
{
    class AirCombatPlugin
        :PlugIn
    {
        private Fighter _fighter1;
        private Fighter _fighter2;
        private readonly List<Missile> _missiles = new List<Missile>();

        private IProximityDatabase<IVehicle> _pd;

        public override bool RequestInitialSelection
        {
            get
            {
                return true;
            }
        }

        public AirCombatPlugin(IAnnotationService annotations)
            :base(annotations)
        {
        }

        public override void Open()
        {
            Vector3 center = Vector3.Zero;
			const float div = 10.0f;
			Vector3 divisions = new Vector3(div, div, div);
			const float diameter = Fighter.WORLD_RADIUS * 2;
			Vector3 dimensions = new Vector3(diameter, diameter, diameter);
			_pd = new LocalityQueryProximityDatabase<IVehicle>(center, dimensions, divisions);
            _missiles.Clear();

            _fighter1 = new Fighter(_pd, Annotations, FireMissile)
            {
                Position = new Vector3(20, 0, 0),
                Forward = Vector3Helpers.RandomUnitVector(),
                Color = Color.Green
            };
            _fighter2 = new Fighter(_pd, Annotations, FireMissile)
            {
                Position = new Vector3(-20, 0, 0),
                Forward = Vector3Helpers.RandomUnitVector(),
                Color = Color.Blue
            };
            _fighter1.Enemy = _fighter2;
            _fighter2.Enemy = _fighter1;
        }

        private void FireMissile(Fighter launcher, Fighter target)
        {
            if (_missiles.Count(m => m.Target == target) < 3)
            {
                _missiles.Add(new Missile(_pd, target, Annotations)
                {
                    Position = launcher.Position,
                    Forward = Vector3.Normalize(launcher.Forward * 0.8f + Vector3Helpers.RandomUnitVector() * 0.2f),
                    Speed = launcher.Speed,
                    Color = new Color(launcher.Color.ToVector3() * 0.5f)
                });
            }
        }

        public override void Update(float currentTime, float elapsedTime)
        {
            _fighter1.Update(currentTime, elapsedTime);
            _fighter2.Update(currentTime, elapsedTime);
            foreach (var missile in _missiles)
                missile.Update(currentTime, elapsedTime);
            _missiles.RemoveAll(m => m.IsDead);
        }

        public override void Redraw(float currentTime, float elapsedTime)
        {
            Demo.UpdateCamera(elapsedTime, _fighter1);

            _fighter1.Draw();
            _fighter2.Draw();

            foreach (var missile in _missiles)
                missile.Draw();
        }

        public override void Close()
        {
            throw new NotImplementedException();
        }

        public override string Name
        {
            get { return "Air Combat"; }
        }

        public override IEnumerable<IVehicle> Vehicles
        {
            get
            {
                yield return _fighter1;
                yield return _fighter2;
            }
        }
    }
}
