// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// Copyright (C) 2007 Michael Coles <michael@digini.com>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using Microsoft.Xna.Framework;

namespace SharpSteer2.WinDemo
{
	/// <summary>
	/// PolylinePathway: a simple implementation of the Pathway protocol.  The path
	/// is a "polyline" a series of line segments between specified points.  A
	/// radius defines a volume for the path which is the union of a sphere at each
	/// point and a cylinder along each segment.
	/// </summary>
	public class PolylinePathway : Pathway
	{
		public int PointCount;
        public Vector3[] Points;
		public float Radius;
	    protected bool Cyclic;

	    public float TotalPathLength { get; private set; }

	    protected PolylinePathway()
		{ }

		// construct a PolylinePathway given the number of points (vertices),
		// an array of points, and a path radius.
        public PolylinePathway(int pointCount, Vector3[] points, float radius, bool cyclic)
		{
			Initialize(pointCount, points, radius, cyclic);
		}

		// utility for constructors in derived classes
	    protected void Initialize(int pointCount, Vector3[] points, float radius, bool cyclic)
		{
			// set data members, allocate arrays
			Radius = radius;
			Cyclic = cyclic;
			PointCount = pointCount;
			TotalPathLength = 0;
			if (Cyclic) PointCount++;
			Lengths = new float[PointCount];
			Points = new Vector3[PointCount];
			Normals = new Vector3[PointCount];

			// loop over all points
			for (int i = 0; i < PointCount; i++)
			{
				// copy in point locations, closing cycle when appropriate
				bool closeCycle = Cyclic && (i == PointCount - 1);
				int j = closeCycle ? 0 : i;
				Points[i] = points[j];

				// for the end of each segment
				if (i > 0)
				{
					// compute the segment length
					Normals[i] = Points[i] - Points[i - 1];
					Lengths[i] = Normals[i].Length();

					// find the normalized vector parallel to the segment
					Normals[i] *= 1 / Lengths[i];

					// keep running total of segment lengths
					TotalPathLength += Lengths[i];
				}
			}
		}

		// Given an arbitrary point ("A"), returns the nearest point ("P") on
		// this path.  Also returns, via output arguments, the path tangent at
		// P and a measure of how far A is outside the Pathway's "tube".  Note
		// that a negative distance indicates A is inside the Pathway.
        public override Vector3 MapPointToPath(Vector3 point, out Vector3 tangent, out float outside)
		{
            float minDistance = float.MaxValue;
            Vector3 onPath = Vector3.Zero;
			tangent = Vector3.Zero;

			// loop over all segments, find the one nearest to the given point
			for (int i = 1; i < PointCount; i++)
			{
				SegmentLength = Lengths[i];
				SegmentNormal = Normals[i];
				float d = PointToSegmentDistance(point, Points[i - 1], Points[i]);
				if (d < minDistance)
				{
					minDistance = d;
					onPath = Chosen;
					tangent = SegmentNormal;
				}
			}

			// measure how far original point is outside the Pathway's "tube"
			outside = Vector3.Distance(onPath, point) - Radius;

			// return point on path
			return onPath;
		}

		// given an arbitrary point, convert it to a distance along the path
        public override float MapPointToPathDistance(Vector3 point)
		{
            float minDistance = float.MaxValue;
			float segmentLengthTotal = 0;
			float pathDistance = 0;

			for (int i = 1; i < PointCount; i++)
			{
				SegmentLength = Lengths[i];
				SegmentNormal = Normals[i];
				float d = PointToSegmentDistance(point, Points[i - 1], Points[i]);
				if (d < minDistance)
				{
					minDistance = d;
					pathDistance = segmentLengthTotal + _segmentProjection;
				}
				segmentLengthTotal += SegmentLength;
			}

			// return distance along path of onPath point
			return pathDistance;
		}

		// given a distance along the path, convert it to a point on the path
        public override Vector3 MapPathDistanceToPoint(float pathDistance)
		{
			// clip or wrap given path distance according to cyclic flag
			float remaining = pathDistance;
			if (Cyclic)
			{
				remaining = pathDistance % TotalPathLength;//FIXME: (float)fmod(pathDistance, totalPathLength);
			}
			else
			{
				if (pathDistance < 0) return Points[0];
				if (pathDistance >= TotalPathLength) return Points[PointCount - 1];
			}

			// step through segments, subtracting off segment lengths until
			// locating the segment that contains the original pathDistance.
			// Interpolate along that segment to find 3d point value to return.
			Vector3 result = Vector3.Zero;
			for (int i = 1; i < PointCount; i++)
			{
				SegmentLength = Lengths[i];
				if (SegmentLength < remaining)
				{
					remaining -= SegmentLength;
				}
				else
				{
					float ratio = remaining / SegmentLength;
                    result = Vector3.Lerp(Points[i - 1], Points[i], ratio);
					break;
				}
			}
			return result;
		}

		// utility methods

		// compute minimum distance from a point to a line segment
	    protected float PointToSegmentDistance(Vector3 point, Vector3 ep0, Vector3 ep1)
		{
			// convert the test point to be "local" to ep0
			Vector3 local = point - ep0;

			// find the projection of "local" onto "segmentNormal"
            _segmentProjection = Vector3.Dot(SegmentNormal, local);

			// handle boundary cases: when projection is not on segment, the
			// nearest point is one of the endpoints of the segment
			if (_segmentProjection < 0)
			{
				Chosen = ep0;
				_segmentProjection = 0;
				return Vector3.Distance(point, ep0);
			}
			if (_segmentProjection > SegmentLength)
			{
				Chosen = ep1;
				_segmentProjection = SegmentLength;
				return Vector3.Distance(point, ep1);
			}

			// otherwise nearest point is projection point on segment
			Chosen = SegmentNormal * _segmentProjection;
			Chosen += ep0;
			return Vector3.Distance(point, Chosen);
		}

		// XXX removed the "private" because it interfered with derived
		// XXX classes later this should all be rewritten and cleaned up
		// private:

		// xxx shouldn't these 5 just be local variables?
		// xxx or are they used to pass secret messages between calls?
		// xxx seems like a bad design
		protected float SegmentLength;
	    private float _segmentProjection;
        protected Vector3 Chosen;
        protected Vector3 SegmentNormal;

		protected float[] Lengths;
        protected Vector3[] Normals;
	}
}
