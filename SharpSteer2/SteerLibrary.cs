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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SharpSteer2.Helpers;
using SharpSteer2.Obstacles;
using SharpSteer2.Pathway;

namespace SharpSteer2
{
	public abstract class SteerLibrary : BaseVehicle
	{
	    protected IAnnotationService annotation { get; private set; }

	    // Constructor: initializes state
	    protected SteerLibrary(IAnnotationService annotationService = null)
		{
            annotation = annotationService ?? new NullAnnotationService();

			// set inital state
			Reset();
		}

		// reset state
		public virtual void Reset()
		{
			// initial state of wander behavior
			WanderSide = 0;
			WanderUp = 0;
		}

		// -------------------------------------------------- steering behaviors

		// Wander behavior
		public float WanderSide;
		public float WanderUp;

	    protected Vector3 SteerForWander(float dt)
		{
			// random walk WanderSide and WanderUp between -1 and +1
			float speed = 12 * dt; // maybe this (12) should be an argument?
			WanderSide = Utilities.ScalarRandomWalk(WanderSide, speed, -1, +1);
			WanderUp = Utilities.ScalarRandomWalk(WanderUp, speed, -1, +1);

			// return a pure lateral steering vector: (+/-Side) + (+/-Up)
			return (Side * WanderSide) + (Up * WanderUp);
		}

		// Flee behavior
	    protected Vector3 SteerForFlee(Vector3 target)
		{
			//  const Vector3 offset = position - target;
            Vector3 offset = Position - target;
            Vector3 desiredVelocity = Vector3Helpers.TruncateLength(offset, MaxSpeed); //xxxnew
			return desiredVelocity - Velocity;
		}

        // Seek behavior
	    protected Vector3 SteerForSeek(Vector3 target)
		{
			//  const Vector3 offset = target - position;
            Vector3 offset = target - Position;
            Vector3 desiredVelocity = Vector3Helpers.TruncateLength(offset, MaxSpeed); //xxxnew
			return desiredVelocity - Velocity;
		}

		// Path Following behaviors
	    protected Vector3 SteerToFollowPath(int direction, float predictionTime, BasePathway path)
		{
			// our goal will be offset from our path distance by this amount
			float pathDistanceOffset = direction * predictionTime * Speed;

			// predict our future position
            Vector3 futurePosition = PredictFuturePosition(predictionTime);

			// measure distance along path of our current and predicted positions
			float nowPathDistance = path.MapPointToPathDistance(Position);
			float futurePathDistance = path.MapPointToPathDistance(futurePosition);

			// are we facing in the correction direction?
			bool rightway = ((pathDistanceOffset > 0) ?
								   (nowPathDistance < futurePathDistance) :
								   (nowPathDistance > futurePathDistance));

			// find the point on the path nearest the predicted future position
			// XXX need to improve calling sequence, maybe change to return a
			// XXX special path-defined object which includes two Vector3s and a 
			// XXX bool (onPath,tangent (ignored), withinPath)
            Vector3 tangent;
			float outside;
            Vector3 onPath = path.MapPointToPath(futurePosition, out tangent, out outside);

			// no steering is required if (a) our future position is inside
			// the path tube and (b) we are facing in the correct direction
			if ((outside < 0) && rightway)
				return Vector3.Zero; //all is well, return zero steering

			// otherwise we need to steer towards a target point obtained
			// by adding pathDistanceOffset to our current path position
			float targetPathDistance = nowPathDistance + pathDistanceOffset;
            Vector3 target = path.MapPathDistanceToPoint(targetPathDistance);
			annotation.PathFollowing(futurePosition, onPath, target, outside);

			// return steering to seek target on path
			return SteerForSeek(target);
		}

	    protected Vector3 SteerToStayOnPath(float predictionTime, BasePathway path)
		{
			// predict our future position
            Vector3 futurePosition = PredictFuturePosition(predictionTime);

			// find the point on the path nearest the predicted future position
            Vector3 tangent;
			float outside;
            Vector3 onPath = path.MapPointToPath(futurePosition, out tangent, out outside);

			if (outside < 0)
                return Vector3.Zero;    // our predicted future position was in the path, return zero steering.

			// our predicted future position was outside the path, need to
			// steer towards it.  Use onPath projection of futurePosition
			// as seek target
			annotation.PathFollowing(futurePosition, onPath, onPath, outside);
			return SteerForSeek(onPath);
		}

		// ------------------------------------------------------------------------
		// Obstacle Avoidance behavior
		//
		// Returns a steering force to avoid a given obstacle.  The purely
		// lateral steering force will turn our this towards a silhouette edge
		// of the obstacle.  Avoidance is required when (1) the obstacle
		// intersects the this's current path, (2) it is in front of the
		// this, and (3) is within minTimeToCollision seconds of travel at the
		// this's current velocity.  Returns a zero vector value (Vector3::zero)
		// when no avoidance is required.
        protected Vector3 SteerToAvoidObstacle(float minTimeToCollision, IObstacle obstacle)
		{
            Vector3 avoidance = obstacle.SteerToAvoid(this, minTimeToCollision);

			// XXX more annotation modularity problems (assumes spherical obstacle)
			if (avoidance != Vector3.Zero)
			{
				annotation.AvoidObstacle(minTimeToCollision * Speed);
			}
			return avoidance;
		}

		// avoids all obstacles in an ObstacleGroup
	    protected Vector3 SteerToAvoidObstacles<Obstacle>(float minTimeToCollision, List<Obstacle> obstacles)
			where Obstacle : IObstacle
		{
            Vector3 avoidance = Vector3.Zero;
			PathIntersection nearest = new PathIntersection();
			PathIntersection next = new PathIntersection();
			float minDistanceToCollision = minTimeToCollision * Speed;

			next.Intersect = false;
			nearest.Intersect = false;

			// test all obstacles for intersection with my forward axis,
			// select the one whose point of intersection is nearest
			foreach (Obstacle o in obstacles)
			{
				//FIXME: this should be a generic call on Obstacle, rather than this code which presumes the obstacle is spherical
				FindNextIntersectionWithSphere(o as SphericalObstacle, ref next);

				if (nearest.Intersect == false || (next.Intersect && next.Distance < nearest.Distance))
					nearest = next;
			}

			// when a nearest intersection was found
			if (nearest.Intersect && (nearest.Distance < minDistanceToCollision))
			{
				// show the corridor that was checked for collisions
				annotation.AvoidObstacle(minDistanceToCollision);

				// compute avoidance steering force: take offset from obstacle to me,
				// take the component of that which is lateral (perpendicular to my
				// forward direction), set length to maxForce, add a bit of forward
				// component (in capture the flag, we never want to slow down)
                Vector3 offset = Position - nearest.Obstacle.Center;
                avoidance = Vector3Helpers.PerpendicularComponent(offset, Forward);
				avoidance.Normalize();
				avoidance *= MaxForce;
				avoidance += Forward * MaxForce * 0.75f;
			}

			return avoidance;
		}

		// ------------------------------------------------------------------------
		// Unaligned collision avoidance behavior: avoid colliding with other
		// nearby vehicles moving in unconstrained directions.  Determine which
		// (if any) other other this we would collide with first, then steers
		// to avoid the site of that potential collision.  Returns a steering
		// force vector, which is zero length if there is no impending collision.
	    protected Vector3 SteerToAvoidNeighbors<TVehicle>(float minTimeToCollision, List<TVehicle> others)
			where TVehicle : IVehicle
		{
			// first priority is to prevent immediate interpenetration
            Vector3 separation = SteerToAvoidCloseNeighbors(0, others);
			if (separation != Vector3.Zero) return separation;

			// otherwise, go on to consider potential future collisions
			float steer = 0;
			IVehicle threat = null;

			// Time (in seconds) until the most immediate collision threat found
			// so far.  Initial value is a threshold: don't look more than this
			// many frames into the future.
			float minTime = minTimeToCollision;

			// xxx solely for annotation
            Vector3 xxxThreatPositionAtNearestApproach = Vector3.Zero;
            Vector3 xxxOurPositionAtNearestApproach = Vector3.Zero;

			// for each of the other vehicles, determine which (if any)
			// pose the most immediate threat of collision.
			foreach (IVehicle other in others)
			{
				if (other != this/*this*/)
				{
					// avoid when future positions are this close (or less)
					float collisionDangerThreshold = Radius * 2;

					// predicted time until nearest approach of "this" and "other"
					float time = PredictNearestApproachTime(other);

					// If the time is in the future, sooner than any other
					// threatened collision...
					if ((time >= 0) && (time < minTime))
					{
						// if the two will be close enough to collide,
						// make a note of it
						if (ComputeNearestApproachPositions(other, time) < collisionDangerThreshold)
						{
							minTime = time;
							threat = other;
							xxxThreatPositionAtNearestApproach = _hisPositionAtNearestApproach;
							xxxOurPositionAtNearestApproach = _ourPositionAtNearestApproach;
						}
					}
				}
			}

			// if a potential collision was found, compute steering to avoid
			if (threat != null)
			{
				// parallel: +1, perpendicular: 0, anti-parallel: -1
                float parallelness = Vector3.Dot(Forward, threat.Forward);
				const float angle = 0.707f;

				if (parallelness < -angle)
				{
					// anti-parallel "head on" paths:
					// steer away from future threat position
                    Vector3 offset = xxxThreatPositionAtNearestApproach - Position;
                    float sideDot = Vector3.Dot(offset, Side);
					steer = (sideDot > 0) ? -1.0f : 1.0f;
				}
				else
				{
					if (parallelness > angle)
					{
						// parallel paths: steer away from threat
						Vector3 offset = threat.Position - Position;
                        float sideDot = Vector3.Dot(offset, Side);
						steer = (sideDot > 0) ? -1.0f : 1.0f;
					}
					else
					{
						// perpendicular paths: steer behind threat
						// (only the slower of the two does this)
						if (threat.Speed <= Speed)
						{
                            float sideDot = Vector3.Dot(Side, threat.Velocity);
							steer = (sideDot > 0) ? -1.0f : 1.0f;
						}
					}
				}

				annotation.AvoidNeighbor(threat, steer, xxxOurPositionAtNearestApproach, xxxThreatPositionAtNearestApproach);
			}

			return Side * steer;
		}

		// Given two vehicles, based on their current positions and velocities,
		// determine the time until nearest approach
	    private float PredictNearestApproachTime(IVehicle other)
		{
			// imagine we are at the origin with no velocity,
			// compute the relative velocity of the other this
            Vector3 myVelocity = Velocity;
            Vector3 otherVelocity = other.Velocity;
            Vector3 relVelocity = otherVelocity - myVelocity;
			float relSpeed = relVelocity.Length();

			// for parallel paths, the vehicles will always be at the same distance,
			// so return 0 (aka "now") since "there is no time like the present"
			if (Math.Abs(relSpeed - 0) < float.Epsilon) return 0;

			// Now consider the path of the other this in this relative
			// space, a line defined by the relative position and velocity.
			// The distance from the origin (our this) to that line is
			// the nearest approach.

			// Take the unit tangent along the other this's path
            Vector3 relTangent = relVelocity / relSpeed;

			// find distance from its path to origin (compute offset from
			// other to us, find length of projection onto path)
            Vector3 relPosition = Position - other.Position;
            float projection = Vector3.Dot(relTangent, relPosition);

			return projection / relSpeed;
		}

		// Given the time until nearest approach (predictNearestApproachTime)
		// determine position of each this at that time, and the distance
		// between them
		public float ComputeNearestApproachPositions(IVehicle other, float time)
		{
            Vector3 myTravel = Forward * Speed * time;
            Vector3 otherTravel = other.Forward * other.Speed * time;

            Vector3 myFinal = Position + myTravel;
            Vector3 otherFinal = other.Position + otherTravel;

			// xxx for annotation
			_ourPositionAtNearestApproach = myFinal;
			_hisPositionAtNearestApproach = otherFinal;

			return Vector3.Distance(myFinal, otherFinal);
		}

		/// XXX globals only for the sake of graphical annotation
        Vector3 _hisPositionAtNearestApproach;
        Vector3 _ourPositionAtNearestApproach;

		// ------------------------------------------------------------------------
		// avoidance of "close neighbors" -- used only by steerToAvoidNeighbors
		//
		// XXX  Does a hard steer away from any other agent who comes withing a
		// XXX  critical distance.  Ideally this should be replaced with a call
		// XXX  to steerForSeparation.
        public Vector3 SteerToAvoidCloseNeighbors<TVehicle>(float minSeparationDistance, List<TVehicle> others)
			where TVehicle : IVehicle
		{
			// for each of the other vehicles...
			foreach (IVehicle other in others)
			{
				if (other != this/*this*/)
				{
					float sumOfRadii = Radius + other.Radius;
					float minCenterToCenter = minSeparationDistance + sumOfRadii;
					Vector3 offset = other.Position - Position;
					float currentDistance = offset.Length();

					if (currentDistance < minCenterToCenter)
					{
						annotation.AvoidCloseNeighbor(other, minSeparationDistance);
                        return Vector3Helpers.PerpendicularComponent(-offset, Forward);
					}
				}
			}

			// otherwise return zero
			return Vector3.Zero;
		}

		// ------------------------------------------------------------------------
		// used by boid behaviors
	    private bool IsInBoidNeighborhood(IVehicle other, float minDistance, float maxDistance, float cosMaxAngle)
		{
			if (other == this)
				return false;

		    Vector3 offset = other.Position - Position;
		    float distanceSquared = offset.LengthSquared();

		    // definitely in neighborhood if inside minDistance sphere
		    if (distanceSquared < (minDistance * minDistance))
		        return true;

		    // definitely not in neighborhood if outside maxDistance sphere
		    if (distanceSquared > (maxDistance * maxDistance))
		        return false;

		    // otherwise, test angular offset from forward axis
		    Vector3 unitOffset = offset / (float)Math.Sqrt(distanceSquared);
		    float forwardness = Vector3.Dot(Forward, unitOffset);
		    return forwardness > cosMaxAngle;
		}

		// ------------------------------------------------------------------------
		// Separation behavior -- determines the direction away from nearby boids
	    protected Vector3 SteerForSeparation(float maxDistance, float cosMaxAngle, IEnumerable<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
            Vector3 steering = Vector3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
	        foreach (var other in flock)
            {
			    if (!IsInBoidNeighborhood(other, Radius * 3, maxDistance, cosMaxAngle))
			        continue;

			    // add in steering contribution
			    // (opposite of the offset direction, divided once by distance
			    // to normalize, divided another time to get 1/d falloff)
			    Vector3 offset = other.Position - Position;
			    float distanceSquared = Vector3.Dot(offset, offset);
			    steering += (offset / -distanceSquared);

			    // count neighbors
			    neighbors++;
			}

			// divide by neighbors, then normalize to pure direction
            if (neighbors > 0)
            {
                steering = (steering / neighbors);
                steering.Normalize();
            }

			return steering;
		}

		// ------------------------------------------------------------------------
		// Alignment behavior
	    protected Vector3 SteerForAlignment(float maxDistance, float cosMaxAngle, List<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
			Vector3 steering = Vector3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
			for (int i = 0; i < flock.Count; i++)
			{
				IVehicle other = flock[i];
				if (IsInBoidNeighborhood(other, Radius * 3, maxDistance, cosMaxAngle))
				{
					// accumulate sum of neighbor's heading
					steering += other.Forward;

					// count neighbors
					neighbors++;
				}
			}

			// divide by neighbors, subtract off current heading to get error-
			// correcting direction, then normalize to pure direction
            if (neighbors > 0)
            {
                steering = ((steering / neighbors) - Forward);
                steering.Normalize();
            }

			return steering;
		}

		// ------------------------------------------------------------------------
		// Cohesion behavior
	    protected Vector3 SteerForCohesion(float maxDistance, float cosMaxAngle, List<IVehicle> flock)
		{
			// steering accumulator and count of neighbors, both initially zero
			Vector3 steering = Vector3.Zero;
			int neighbors = 0;

			// for each of the other vehicles...
			for (int i = 0; i < flock.Count; i++)
			{
				IVehicle other = flock[i];
				if (IsInBoidNeighborhood(other, Radius * 3, maxDistance, cosMaxAngle))
				{
					// accumulate sum of neighbor's positions
					steering += other.Position;

					// count neighbors
					neighbors++;
				}
			}

			// divide by neighbors, subtract off current position to get error-
			// correcting direction, then normalize to pure direction
			if (neighbors > 0)
            {
                steering = ((steering / neighbors) - Position);
                steering.Normalize();
            }

			return steering;
		}

		// ------------------------------------------------------------------------
		// pursuit of another this (& version with ceiling on prediction time)

	    readonly static float[,] _pursuitFactors = new float[3, 3]
	    {
            { 2, 2, 0.5f },         //Behind
            { 4, 0.8f, 1 },         //Aside
            { 0.85f, 1.8f, 4 },     //Ahead
	    };

	    protected Vector3 SteerForPursuit(IVehicle quarry, float maxPredictionTime = float.MaxValue)
		{
			// offset from this to quarry, that distance, unit vector toward quarry
            Vector3 offset = quarry.Position - Position;
			float distance = offset.Length();
            Vector3 unitOffset = offset / distance;

			// how parallel are the paths of "this" and the quarry
			// (1 means parallel, 0 is pependicular, -1 is anti-parallel)
            float parallelness = Vector3.Dot(Forward, quarry.Forward);

			// how "forward" is the direction to the quarry
			// (1 means dead ahead, 0 is directly to the side, -1 is straight back)
            float forwardness = Vector3.Dot(Forward, unitOffset);

			float directTravelTime = distance / Math.Max(0.001f, Speed);
			int f = Utilities.IntervalComparison(forwardness, -0.707f, 0.707f);
			int p = Utilities.IntervalComparison(parallelness, -0.707f, 0.707f);

	        // Break the pursuit into nine cases, the cross product of the
			// quarry being [ahead, aside, or behind] us and heading
			// [parallel, perpendicular, or anti-parallel] to us.
	        float timeFactor = _pursuitFactors[f + 1, p + 1];

			// estimated time until intercept of quarry
			float et = directTravelTime * timeFactor;

			// xxx experiment, if kept, this limit should be an argument
			float etl = (et > maxPredictionTime) ? maxPredictionTime : et;

			// estimated position of quarry at intercept
			Vector3 target = quarry.PredictFuturePosition(etl);

			// annotation
			annotation.Line(Position, target, Color.DarkGray);

			return SteerForSeek(target);
		}

		// ------------------------------------------------------------------------
		// evasion of another this
        protected Vector3 SteerForEvasion(IVehicle menace, float maxPredictionTime)
		{
			// offset from this to menace, that distance, unit vector toward menace
			Vector3 offset = menace.Position - Position;
			float distance = offset.Length();

			float roughTime = distance / menace.Speed;
			float predictionTime = ((roughTime > maxPredictionTime) ? maxPredictionTime : roughTime);

			Vector3 target = menace.PredictFuturePosition(predictionTime);

			return SteerForFlee(target);
		}

		// ------------------------------------------------------------------------
		// tries to maintain a given speed, returns a maxForce-clipped steering
		// force along the forward/backward axis
	    protected Vector3 SteerForTargetSpeed(float targetSpeed)
		{
			float mf = MaxForce;
			float speedError = targetSpeed - Speed;
			return Forward * MathHelper.Clamp(speedError, -mf, +mf);
		}

		// ----------------------------------------------------------- utilities
		// XXX these belong somewhere besides the steering library
		// XXX above AbstractVehicle, below SimpleVehicle
		// XXX ("utility this"?)

	    protected bool IsAhead(Vector3 target, float cosThreshold = 0.707f)
		{
			Vector3 targetDirection = (target - Position);
            targetDirection.Normalize();
            return Vector3.Dot(Forward, targetDirection) > cosThreshold;
		}

	    protected bool IsAside(Vector3 target, float cosThreshold = 0.707f)
		{
			Vector3 targetDirection = (target - Position);
            targetDirection.Normalize();
            float dp = Vector3.Dot(Forward, targetDirection);
			return (dp < cosThreshold) && (dp > -cosThreshold);
		}
        public bool IsBehind(Vector3 target, float cosThreshold = -0.707f)
		{
			Vector3 targetDirection = (target - Position);
            targetDirection.Normalize();
            return Vector3.Dot(Forward, targetDirection) < cosThreshold;
		}

		// xxx cwr 9-6-02 temporary to support old code
		protected struct PathIntersection
		{
			public bool Intersect;
			public float Distance;
            public Vector3 SurfacePoint;
            public Vector3 SurfaceNormal;
			public SphericalObstacle Obstacle;
		}

		// xxx experiment cwr 9-6-02
		protected void FindNextIntersectionWithSphere(SphericalObstacle obs, ref PathIntersection intersection)
		{
			// This routine is based on the Paul Bourke's derivation in:
			//   Intersection of a Line and a Sphere (or circle)
			//   http://www.swin.edu.au/astronomy/pbourke/geometry/sphereline/

		    // initialize pathIntersection object
			intersection.Intersect = false;
			intersection.Obstacle = obs;

			// find "local center" (lc) of sphere in boid's coordinate space
			Vector3 lc = LocalizePosition(obs.Center);

			// computer line-sphere intersection parameters
			float b = -2 * lc.Z;
		    var totalRadius = obs.Radius + Radius;
			float c = (lc.X * lc.X) + (lc.Y * lc.Y) + (lc.Z * lc.Z) - (totalRadius * totalRadius);
			float d = (b * b) - (4 * c);

			// when the path does not intersect the sphere
			if (d < 0) return;

			// otherwise, the path intersects the sphere in two points with
			// parametric coordinates of "p" and "q".
			// (If "d" is zero the two points are coincident, the path is tangent)
			float s = (float)Math.Sqrt(d);
			float p = (-b + s) / 2;
			float q = (-b - s) / 2;

			// both intersections are behind us, so no potential collisions
			if ((p < 0) && (q < 0)) return;

			// at least one intersection is in front of us
			intersection.Intersect = true;
			intersection.Distance =
				((p > 0) && (q > 0)) ?
				// both intersections are in front of us, find nearest one
				((p < q) ? p : q) :
				// otherwise only one intersections is in front, select it
				((p > 0) ? p : q);
		}
	}
}
