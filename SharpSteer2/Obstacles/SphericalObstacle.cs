// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// Copyright (C) 2007 Michael Coles <michael@digini.com>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using System;
using Microsoft.Xna.Framework;
using SharpSteer2.Helpers;

namespace SharpSteer2.Obstacles
{
	/// <summary>
	/// SphericalObstacle a simple concrete type of obstacle.
	/// </summary>
	public class SphericalObstacle : IObstacle
	{
	    public float Radius;
	    public Vector3 Center;

	    public SphericalObstacle()
			: this(1, Vector3.Zero)
		{
		}

        public SphericalObstacle(float r, Vector3 c)
		{
			Radius = r;
			Center = c;
		}

	    // XXX 4-23-03: Temporary work around (see comment above)
		//
		// Checks for intersection of the given spherical obstacle with a
		// volume of "likely future vehicle positions": a cylinder along the
		// current path, extending minTimeToCollision seconds along the
		// forward axis from current position.
		//
		// If they intersect, a collision is imminent and this function returns
		// a steering force pointing laterally away from the obstacle's center.
		//
		// Returns a zero vector if the obstacle is outside the cylinder
		//
		// xxx couldn't this be made more compact using localizePosition?

        public Vector3 SteerToAvoid(IVehicle v, float minTimeToCollision)
		{
			// minimum distance to obstacle before avoidance is required
			float minDistanceToCollision = minTimeToCollision * v.Speed;
			float minDistanceToCenter = minDistanceToCollision + Radius;

			// contact distance: sum of radii of obstacle and vehicle
			float totalRadius = Radius + v.Radius;

			// obstacle center relative to vehicle position
			Vector3 localOffset = Center - v.Position;

			// distance along vehicle's forward axis to obstacle's center
            float forwardComponent = Vector3.Dot(localOffset, v.Forward);
			Vector3 forwardOffset = v.Forward * forwardComponent;

			// offset from forward axis to obstacle's center
			Vector3 offForwardOffset = localOffset - forwardOffset;

			// test to see if sphere overlaps with obstacle-free corridor
			bool inCylinder = offForwardOffset.Length() < totalRadius;
			bool nearby = forwardComponent < minDistanceToCenter;
			bool inFront = forwardComponent > 0;

			// if all three conditions are met, steer away from sphere center
            if (inCylinder && nearby && inFront)
            {
                var avoidance = Vector3Helpers.PerpendicularComponent(-localOffset, v.Forward);
                avoidance.Normalize();
                avoidance *= v.MaxForce;
                avoidance += v.Forward * v.MaxForce * 0.75f;
                return avoidance;

                //return offForwardOffset * -1;
            }

            return Vector3.Zero;
		}

        // xxx experiment cwr 9-6-02
        public float? NextIntersection(IVehicle vehicle)
        {
            // This routine is based on the Paul Bourke's derivation in:
            //   Intersection of a Line and a Sphere (or circle)
            //   http://www.swin.edu.au/astronomy/pbourke/geometry/sphereline/

            // find "local center" (lc) of sphere in boid's coordinate space
            Vector3 lc = vehicle.LocalizePosition(Center);

            // computer line-sphere intersection parameters
            float b = -2 * lc.Z;
            var totalRadius = Radius + vehicle.Radius;
            float c = (lc.X * lc.X) + (lc.Y * lc.Y) + (lc.Z * lc.Z) - (totalRadius * totalRadius);
            float d = (b * b) - (4 * c);

            // when the path does not intersect the sphere
            if (d < 0)
                return null;

            // otherwise, the path intersects the sphere in two points with
            // parametric coordinates of "p" and "q".
            // (If "d" is zero the two points are coincident, the path is tangent)
            float s = (float)Math.Sqrt(d);
            float p = (-b + s) / 2;
            float q = (-b - s) / 2;

            // both intersections are behind us, so no potential collisions
            if ((p < 0) && (q < 0))
                return null;

            // at least one intersection is in front of us
            return
                ((p > 0) && (q > 0)) ?
                // both intersections are in front of us, find nearest one
                ((p < q) ? p : q) :
                // otherwise only one intersections is in front, select it
                ((p > 0) ? p : q);
        }
	}
}
