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
    /// transformation as three orthonormal unit basis vectors and the
    /// origin of the local space.  These correspond to the "rows" of
    /// a 3x4 transformation matrix with [0 0 0 1] as the final column
    /// </summary>
    public class LocalSpaceBasis
        : ILocalSpaceBasis
    {
        /// <summary>
        /// side-pointing unit basis vector
        /// </summary>
        public Vector3 Side { get; set; }

        /// <summary>
        /// upward-pointing unit basis vector
        /// </summary>
        public Vector3 Up { get; set; }

        /// <summary>
        /// forward-pointing unit basis vector
        /// </summary>
        public Vector3 Forward { get; set; }

        /// <summary>
        /// origin of local space
        /// </summary>
        public Vector3 Position { get; set; }
    }

    /// <summary>
	/// LocalSpaceMixin is a mixin layer, a class template with a paramterized base
	/// class.  Allows "LocalSpace-ness" to be layered on any class.
	/// </summary>
	public class LocalSpace : LocalSpaceBasis
    {
		public LocalSpace()
		{
			ResetLocalSpace();
		}

        public LocalSpace(Vector3 up, Vector3 forward, Vector3 position)
		{
			Up = up;
			Forward = forward;
            Position = position;
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
			Forward = Vector3.Forward;
		    Side = Vector3.Left;
			Up = Vector3.Up;
            Position = Vector3.Zero;
		}

		// ------------------------------------------------------------------------
		// set "side" basis vector to normalized cross product of forward and up
		public void SetUnitSideFromForwardAndUp()
		{
		    // derive new unit side basis vector from forward and up
		    Side = Vector3.Cross(Forward, Up);

		    Side.Normalize();
		}

	    // ------------------------------------------------------------------------
		// regenerate the orthonormal basis vectors given a new forward
		//(which is expected to have unit length)
        public void RegenerateOrthonormalBasisUF(Vector3 newUnitForward)
		{
			Forward = newUnitForward;

			// derive new side basis vector from NEW forward and OLD up
			SetUnitSideFromForwardAndUp();

			// derive new Up basis vector from new Side and new Forward
			//(should have unit length since Side and Forward are
			// perpendicular and unit length)
			Up = Vector3.Cross(Side, Forward);
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
			Up = newUp;
            newForward.Normalize();
			RegenerateOrthonormalBasis(newForward);
		}
	}
}
