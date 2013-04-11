// Copyright (c) 2002-2003, Sony Computer Entertainment America
// Copyright (c) 2002-2003, Craig Reynolds <craig_reynolds@playstation.sony.com>
// Copyright (C) 2007 Bjoern Graf <bjoern.graf@gmx.net>
// All rights reserved.
//
// This software is licensed as described in the file license.txt, which
// you should have received as part of this distribution. The terms
// are also available at http://www.codeplex.com/SharpSteer/Project/License.aspx.

using Microsoft.Xna.Framework;

namespace SharpSteer2.WinDemo.PlugIns.MultiplePursuit
{
	public class MpBase : SimpleVehicle
	{
		protected Trail trail;

		// constructor
        public MpBase(IAnnotationService annotations = null)
            :base(annotations)
		{
			Reset();
		}

		// reset state
		public override void Reset()
		{
			base.Reset();			// reset the vehicle 

			Speed = 0;            // speed along Forward direction.
			MaxForce = 5.0f;       // steering force is clipped to this magnitude
			MaxSpeed = 3.0f;       // velocity is clipped to this magnitude
			trail = new Trail();
			trail.Clear();    // prevent long streaks due to teleportation 
		}

		// draw into the scene
		public void Draw()
		{
			Drawing.DrawBasic2dCircularVehicle(this, bodyColor);
			trail.Draw(Annotation.drawer);
		}

		// for draw method
		protected Color bodyColor;
	}
}
