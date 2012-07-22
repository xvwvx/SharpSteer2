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

namespace SharpSteer2
{
	/// <summary>
	/// LocalSpaceMixin is a mixin layer, a class template with a paramterized base
	/// class.  Allows "LocalSpace-ness" to be layered on any class.
	/// </summary>
	public class LocalSpace : ILocalSpace
	{
		// transformation as three orthonormal unit basis vectors and the
		// origin of the local space.  These correspond to the "rows" of
		// a 3x4 transformation matrix with [0 0 0 1] as the final column

        Vector3 _side;     //    side-pointing unit basis vector
        Vector3 _up;       //  upward-pointing unit basis vector
        Vector3 _forward;  // forward-pointing unit basis vector
        Vector3 _position; // origin of local space

		// accessors (get and set) for side, up, forward and position
        public Vector3 Side
		{
			get { return _side; }
			set { _side = value; }
		}
        public Vector3 Up
		{
			get { return _up; }
			set { _up = value; }
		}
        public Vector3 Forward
		{
			get { return _forward; }
			set { _forward = value; }
		}
        public Vector3 Position
		{
			get { return _position; }
			set { _position = value; }
		}

        public Vector3 SetUp(float x, float y, float z)
		{
            _up.X = x;
            _up.Y = y;
            _up.Z = z;

			return _up;
		}
        public Vector3 SetForward(float x, float y, float z)
		{
            _forward.X = x;
            _forward.Y = y;
            _forward.Z = z;

			return _forward;
		}
        public Vector3 SetPosition(float x, float y, float z)
		{
            _position.X = x;
            _position.Y = y;
            _position.Z = z;

			return _position;
		}

		// ------------------------------------------------------------------------
		// Global compile-time switch to control handedness/chirality: should
		// LocalSpace use a left- or right-handed coordinate system?  This can be
		// overloaded in derived types (e.g. vehicles) to change handedness.
		public bool IsRightHanded { get { return true; } }

		// ------------------------------------------------------------------------
		// constructors
		public LocalSpace()
		{
			ResetLocalSpace();
		}

        public LocalSpace(Vector3 side, Vector3 up, Vector3 forward, Vector3 position)
		{
			_side = side;
			_up = up;
			_forward = forward;
			_position = position;
		}

        public LocalSpace(Vector3 up, Vector3 forward, Vector3 position)
		{
			_up = up;
			_forward = forward;
			_position = position;
			SetUnitSideFromForwardAndUp();
		}

		// ------------------------------------------------------------------------
		// reset transform: set local space to its identity state, equivalent to a
		// 4x4 homogeneous transform like this:
		//
		//     [ X 0 0 0 ]
		//     [ 0 1 0 0 ]
		//     [ 0 0 1 0 ]
		//     [ 0 0 0 1 ]
		//
		// where X is 1 for a left-handed system and -1 for a right-handed system.
		public void ResetLocalSpace()
		{
			_forward = Vector3.Backward;
			_side = LocalRotateForwardToSide(Forward);
			_up = Vector3.Up;
			_position = Vector3.Zero;
		}

		// ------------------------------------------------------------------------
		// transform a direction in global space to its equivalent in local space
        public Vector3 LocalizeDirection(Vector3 globalDirection)
        {
			// dot offset with local basis vectors to obtain local coordiantes
            return new Vector3(Vector3.Dot(globalDirection, _side), Vector3.Dot(globalDirection, _up), Vector3.Dot(globalDirection, _forward));
		}

		// ------------------------------------------------------------------------
		// transform a point in global space to its equivalent in local space
        public Vector3 LocalizePosition(Vector3 globalPosition)
		{
			// global offset from local origin
            Vector3 globalOffset = globalPosition - _position;

			// dot offset with local basis vectors to obtain local coordiantes
			return LocalizeDirection(globalOffset);
		}

		// ------------------------------------------------------------------------
		// transform a point in local space to its equivalent in global space
        public Vector3 GlobalizePosition(Vector3 localPosition)
		{
			return _position + GlobalizeDirection(localPosition);
		}

		// ------------------------------------------------------------------------
		// transform a direction in local space to its equivalent in global space
        public Vector3 GlobalizeDirection(Vector3 localDirection)
		{
			return ((_side * localDirection.X) +
					(_up * localDirection.Y) +
					(_forward * localDirection.Z));
		}

		// ------------------------------------------------------------------------
		// set "side" basis vector to normalized cross product of forward and up
		public void SetUnitSideFromForwardAndUp()
		{
			// derive new unit side basis vector from forward and up
			if (IsRightHanded)
				_side = Vector3.Cross(_forward, _up);
			else
                _side = Vector3.Cross(_up, _forward);
			
            _side.Normalize();
		}

		// ------------------------------------------------------------------------
		// regenerate the orthonormal basis vectors given a new forward
		//(which is expected to have unit length)
        public void RegenerateOrthonormalBasisUF(Vector3 newUnitForward)
		{
			_forward = newUnitForward;

			// derive new side basis vector from NEW forward and OLD up
			SetUnitSideFromForwardAndUp();

			// derive new Up basis vector from new Side and new Forward
			//(should have unit length since Side and Forward are
			// perpendicular and unit length)
			if (IsRightHanded)
                _up = Vector3.Cross(_side, _forward);
			else
                _up = Vector3.Cross(_forward, _side);
		}

		// for when the new forward is NOT know to have unit length
        public void RegenerateOrthonormalBasis(Vector3 newForward)
		{
            newForward.Normalize();

			RegenerateOrthonormalBasisUF(newForward);
		}

		// for supplying both a new forward and and new up
        public void RegenerateOrthonormalBasis(Vector3 newForward, Vector3 newUp)
		{
			_up = newUp;
            newForward.Normalize();
			RegenerateOrthonormalBasis(newForward);
		}

		// ------------------------------------------------------------------------
		// rotate, in the canonical direction, a vector pointing in the
		// "forward"(+Z) direction to the "side"(+/-X) direction
        public Vector3 LocalRotateForwardToSide(Vector3 value)
		{
			return new Vector3(IsRightHanded ? -value.Z : +value.Z, value.Y, value.X);
		}

		// not currently used, just added for completeness
        public Vector3 GlobalRotateForwardToSide(Vector3 value)
		{
            Vector3 localForward = LocalizeDirection(value);
            Vector3 localSide = LocalRotateForwardToSide(localForward);
			return GlobalizeDirection(localSide);
		}
	}
}
