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
using Microsoft.Xna.Framework;

namespace SharpSteer2
{
	public class BruteForceProximityDatabase<ContentType> : IProximityDatabase<ContentType>
		where ContentType : class
	{
		// "token" to represent objects stored in the database
		public class TokenType : ITokenForProximityDatabase<ContentType>
		{
		    readonly BruteForceProximityDatabase<ContentType> _bfpd;
			ContentType _obj;
            Vector3 _position;

			// constructor
			public TokenType(ContentType parentObject, BruteForceProximityDatabase<ContentType> pd)
			{
				// store pointer to our associated database and the obj this
				// token represents, and store this token on the database's vector
				_bfpd = pd;
				_obj = parentObject;
				_bfpd._group.Add(this);
			}

			// destructor
			//FIXME: need to move elsewhere
			//~TokenType()
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			protected virtual void Dispose(bool disposing)
			{
			    if (_obj == null)
			        return;

			    // remove this token from the database's vector
			    _bfpd._group.RemoveAt(_bfpd._group.FindIndex(item => item == this));
			    _obj = null;
			}

			// the client obj calls this each time its position changes
            public void UpdateForNewPosition(Vector3 newPosition)
			{
				_position = newPosition;
			}

			// find all neighbors within the given sphere (as center and radius)
            public void FindNeighbors(Vector3 center, float radius, ref List<ContentType> results)
			{
				// loop over all tokens
				float r2 = radius * radius;
				for (int i = 0; i < _bfpd._group.Count; i++)
				{
                    Vector3 offset = center - _bfpd._group[i]._position;
					float d2 = offset.LengthSquared();

					// push onto result vector when within given radius
					if (d2 < r2) results.Add(_bfpd._group[i]._obj);
				}
			}
		}

		// Contains all tokens in database
	    readonly List<TokenType> _group;

		// constructor
		public BruteForceProximityDatabase()
		{
			_group = new List<TokenType>();
		}

		// allocate a token to represent a given client object in this database
		public ITokenForProximityDatabase<ContentType> AllocateToken(ContentType parentObject)
		{
			return new TokenType(parentObject, this);
		}

		// return the number of tokens currently in the database
		public int Count
		{
			get { return _group.Count; }
		}
	}
}
