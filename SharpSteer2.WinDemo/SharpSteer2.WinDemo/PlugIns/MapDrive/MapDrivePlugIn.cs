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
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using SharpSteer2.Helpers;

namespace SharpSteer2.WinDemo.PlugIns.MapDrive
{
	public class MapDrivePlugIn : PlugIn
	{
        private readonly IAnnotationService _annotations;

		public MapDrivePlugIn(IAnnotationService annotations = null)
		{
            _annotations = annotations;
			_vehicles = new List<MapDriver>();
		}

		public override String Name { get { return "Driving through map based obstacles"; } }

		public override float SelectionOrderSortKey { get { return 0.07f; } }

		public override void Open()
		{
			// make new MapDriver
            _vehicle = new MapDriver(_annotations);
			_vehicles.Add(_vehicle);
			Demo.SelectedVehicle = _vehicle;

			// marks as obstacles map cells adjacent to the path
			_usePathFences = true;

			// scatter random rock clumps over map
			_useRandomRocks = true;

			// init Demo camera
			_initCamDist = 30;
			_initCamElev = 15;
			Demo.Init2dCamera(_vehicle, _initCamDist, _initCamElev);
			// "look straight down at vehicle" camera mode parameters
			Demo.Camera.LookDownDistance = 50;
			// "static" camera mode parameters
			Demo.Camera.FixedPosition = new Vector3(145);
			Demo.Camera.FixedTarget.X = 40;
            Demo.Camera.FixedTarget.Y = 0;
            Demo.Camera.FixedTarget.Z = 40;
			Demo.Camera.FixedUp = Vector3.Up;

			// reset this plugin
			Reset();
		}


		public override void Update(float currentTime, float elapsedTime)
		{
			// update simulation of test vehicle
			_vehicle.Update(currentTime, elapsedTime);

			// when vehicle drives outside the world
			if (_vehicle.HandleExitFromMap()) RegenerateMap();

			// QQQ first pass at detecting "stuck" state
			if (_vehicle.stuck && (_vehicle.RelativeSpeed() < 0.001f))
			{
				_vehicle.stuckCount++;
				Reset();
			}
		}


		public override void Redraw(float currentTime, float elapsedTime)
		{
			// update camera, tracking test vehicle
			Demo.UpdateCamera(currentTime, elapsedTime, _vehicle);

			// draw "ground plane"  (make it 4x map size)
			float s = MapDriver.WORLD_SIZE * 2;
			const float u = -0.2f;
			Drawing.DrawQuadrangle(new Vector3(+s, u, +s),
							new Vector3(+s, u, -s),
							new Vector3(-s, u, -s),
							new Vector3(-s, u, +s),
							new Color((byte)(255.0f * 0.8f), (byte)(255.0f * 0.7f), (byte)(255.0f * 0.5f))); // "sand"

			// draw map and path
			if (MapDriver.demoSelect == 2) _vehicle.DrawPath();
			_vehicle.DrawMap();

			// draw test vehicle
			_vehicle.Draw();

			// QQQ mark origin to help spot artifacts
			const float tick = 2;
			Drawing.DrawLine(new Vector3(tick, 0, 0), new Vector3(-tick, 0, 0), Color.Green);
			Drawing.DrawLine(new Vector3(0, 0, tick), new Vector3(0, 0, -tick), Color.Green);

			// compute conversion factor miles-per-hour to meters-per-second
			const float metersPerMile = 1609.344f;
			const float secondsPerHour = 3600;
			const float MPSperMPH = metersPerMile / secondsPerHour;

			// display status in the upper left corner of the window
			StringBuilder status = new StringBuilder();
			status.AppendFormat("Speed: {0} mps ({1} mph), average: {2:0.0} mps\n\n",
				   (int)_vehicle.Speed,
				   (int)(_vehicle.Speed / MPSperMPH),
				   _vehicle.totalDistance / _vehicle.totalTime);
			status.AppendFormat("collisions avoided for {0} seconds",
				   (int)(Demo.Clock.TotalSimulationTime - _vehicle.timeOfLastCollision));
			if (_vehicle.countOfCollisionFreeTimes > 0)
			{
				status.AppendFormat("\nmean time between collisions: {0} ({1}/{2})",
					   (int)(_vehicle.sumOfCollisionFreeTimes / _vehicle.countOfCollisionFreeTimes),
					   (int)_vehicle.sumOfCollisionFreeTimes,
					   _vehicle.countOfCollisionFreeTimes);
			}

			status.AppendFormat("\n\nStuck count: {0} ({1} cycles, {2} off path)",
				_vehicle.stuckCount,
				_vehicle.stuckCycleCount,
				_vehicle.stuckOffPathCount);
			status.Append("\n\n[F1] ");
			if (1 == MapDriver.demoSelect) status.Append("wander, ");
			if (2 == MapDriver.demoSelect) status.Append("follow path, ");
			status.Append("avoid obstacle");

			if (2 == MapDriver.demoSelect)
			{
				status.Append("\n[F2] path following direction: ");
				if (_vehicle.pathFollowDirection > 0)
					status.Append("+1");
				else
					status.Append("-1");
				status.Append("\n[F3] path fence: ");
				if (_usePathFences)
					status.Append("on");
				else
					status.Append("off");
			}

			status.Append("\n[F4] rocks: ");
			if (_useRandomRocks)
				status.Append("on");
			else
				status.Append("off");
			status.Append("\n[F5] prediction: ");
			if (_vehicle.curvedSteering)
				status.Append("curved");
			else
				status.Append("linear");
			if (2 == MapDriver.demoSelect)
			{
				status.AppendFormat("\n\nLap {0} (completed: {1}%)",
					_vehicle.lapsStarted,
					   ((_vehicle.lapsStarted < 2) ? 0 :
						   (int)(100 * ((float)_vehicle.lapsFinished /
										 (_vehicle.lapsStarted - 1))))
					   );

				status.AppendFormat("\nHints given: {0}, taken: {1}",
					_vehicle.hintGivenCount,
					_vehicle.hintTakenCount);
			}
			status.Append("\n");
			qqqRange("WR ", MapDriver.savedNearestWR, status);
			qqqRange("R  ", MapDriver.savedNearestR, status);
			qqqRange("L  ", MapDriver.savedNearestL, status);
			qqqRange("WL ", MapDriver.savedNearestWL, status);
			Vector3 screenLocation = new Vector3(15, 50, 0);
			Vector3 color = new Vector3(0.15f, 0.15f, 0.5f);
			Drawing.Draw2dTextAt2dLocation(status.ToString(), screenLocation, new Color(color));

			{
				float v = Drawing.GetWindowHeight() - 5;
				const float m = 10;
				float w = Drawing.GetWindowWidth();
				float f = w - (2 * m);

				// limit tick mark
				float l = _vehicle.annoteMaxRelSpeed;
				Drawing.Draw2dLine(new Vector3(m + (f * l), v - 3, 0), new Vector3(m + (f * l), v + 3, 0), Color.Black);
				// two "inverse speedometers" showing limits due to curvature and
				// path alignment
				if (l != 0)
				{
					float c = _vehicle.annoteMaxRelSpeedCurve;
					float p = _vehicle.annoteMaxRelSpeedPath;
					Drawing.Draw2dLine(new Vector3(m + (f * c), v + 1, 0), new Vector3(w - m, v + 1, 0), Color.Red);
					Drawing.Draw2dLine(new Vector3(m + (f * p), v - 2, 0), new Vector3(w - m, v - 1, 0), Color.Green);
				}
				// speedometer: horizontal line with length proportional to speed
				Drawing.Draw2dLine(new Vector3(m, v, 0), new Vector3(m + (f * s), v, 0), Color.White);
				// min and max tick marks
				Drawing.Draw2dLine(new Vector3(m, v, 0), new Vector3(m, v - 2, 0), Color.White);
				Drawing.Draw2dLine(new Vector3(w - m, v, 0), new Vector3(w - m, v - 2, 0), Color.White);
			}
		}

	    static void qqqRange(String text, float range, StringBuilder status)
		{
			status.AppendFormat("\n{0}", text);
            if (range >= 9999.0f)
                status.Append("--");
            else
				status.Append((int)range);
		}

		public override void Close()
		{
			_vehicles.Clear();
		}

		public override void Reset()
		{
			RegenerateMap();

			// reset vehicle
			_vehicle.Reset();

			// make camera jump immediately to new position
			Demo.Camera.DoNotSmoothNextMove();

			// reset camera position
			Demo.Position2dCamera(_vehicle, _initCamDist, _initCamElev);
		}

		public override void HandleFunctionKeys(Keys key)
		{
			switch (key)
			{
			case Keys.F1: SelectNextDemo(); break;
			case Keys.F2: ReversePathFollowDirection(); break;
			case Keys.F3: TogglePathFences(); break;
			case Keys.F4: ToggleRandomRocks(); break;
			case Keys.F5: ToggleCurvedSteering(); break;

			case Keys.F6: // QQQ draw an enclosed "pen" of obstacles to test cycle-stuck
				{
					float m = MapDriver.WORLD_SIZE * 0.4f; // main diamond size
					float n = MapDriver.WORLD_SIZE / 8;    // notch size
					Vector3 q = new Vector3(0, 0, m - n);
					Vector3 s = new Vector3(2 * n, 0, 0);
					Vector3 c = s - q;
					Vector3 d =s + q;
					const int pathPointCount = 2;
					float[] pathRadii = new float[] { 10, 10 };
					Vector3[] pathPoints = new Vector3[] { c, d };
					GCRoute r = new GCRoute(pathPointCount, pathPoints, pathRadii, false);
					DrawPathFencesOnMap(_vehicle.map, r);
					break;
				}
			}
		}

		void ReversePathFollowDirection()
		{
			_vehicle.pathFollowDirection = (_vehicle.pathFollowDirection > 0) ? -1 : +1;
		}

		void TogglePathFences()
		{
			_usePathFences = !_usePathFences;
			Reset();
		}

		void ToggleRandomRocks()
		{
			_useRandomRocks = !_useRandomRocks;
			Reset();
		}

		void ToggleCurvedSteering()
		{
			_vehicle.curvedSteering = !_vehicle.curvedSteering;
			_vehicle.incrementalSteering = !_vehicle.incrementalSteering;
			Reset();
		}

		void SelectNextDemo()
		{
			StringBuilder message = new StringBuilder();
			message.AppendFormat("{0}: ", Name);
			if (++MapDriver.demoSelect > 2)
			{
				MapDriver.demoSelect = 0;
			}
			switch (MapDriver.demoSelect)
			{
			case 0:
				message.Append("obstacle avoidance and speed control");
				Reset();
				break;
			case 1:
				message.Append("wander, obstacle avoidance and speed control");
				Reset();
				break;
			case 2:
				message.Append("path following, obstacle avoidance and speed control");
				Reset();
				break;
			}
			//FIXME: Demo.printMessage (message);
		}

		// random utility, worth moving to Utilities.h?
	    

		void RegenerateMap()
		{
			// regenerate map: clear and add random "rocks"
			_vehicle.map.Clear();
			DrawRandomClumpsOfRocksOnMap(_vehicle.map);
			ClearCenterOfMap(_vehicle.map);

			// draw fences for first two demo modes
			if (MapDriver.demoSelect < 2) DrawBoundaryFencesOnMap(_vehicle.map);

			// randomize path widths
			if (MapDriver.demoSelect == 2)
			{
				int count = _vehicle.path.PointCount;
				bool upstream = _vehicle.pathFollowDirection > 0;
				int entryIndex = upstream ? 1 : count - 1;
				int exitIndex = upstream ? count - 1 : 1;
				float lastExitRadius = _vehicle.path.Radii[exitIndex];
				for (int i = 1; i < count; i++)
				{
					_vehicle.path.Radii[i] = RandomHelpers.Random(4, 19);
				}
				_vehicle.path.Radii[entryIndex] = lastExitRadius;
			}

			// mark path-boundary map cells as obstacles
			// (when in path following demo and appropriate mode is set)
			if (_usePathFences && (MapDriver.demoSelect == 2))
				DrawPathFencesOnMap(_vehicle.map, _vehicle.path);
		}

		void DrawRandomClumpsOfRocksOnMap(TerrainMap map)
		{
			if (_useRandomRocks)
			{
				const int spread = 4;
				int r = map.Cellwidth();
				int k = RandomHelpers.RandomInt(50, 150);

				for (int p = 0; p < k; p++)
				{
					int i = RandomHelpers.RandomInt(0, r - spread);
					int j = RandomHelpers.RandomInt(0, r - spread);
					int c = RandomHelpers.RandomInt(0, 10);

					for (int q = 0; q < c; q++)
					{
						int m = RandomHelpers.RandomInt(0, spread);
						int n = RandomHelpers.RandomInt(0, spread);
						map.SetMapBit(i + m, j + n, true);
					}
				}
			}
		}


		void DrawBoundaryFencesOnMap(TerrainMap map)
		{
			// QQQ it would make more sense to do this with a "draw line
			// QQQ on map" primitive, may need that for other things too

			int cw = map.Cellwidth();
			int ch = map.Cellheight();

			int r = cw - 1;
			int a = cw >> 3;
			int b = cw - a;
			int o = cw >> 4;
			int p = (cw - o) >> 1;
			int q = (cw + o) >> 1;

			for (int i = 0; i < cw; i++)
			{
				for (int j = 0; j < ch; j++)
				{
					bool c = i > a && i < b && (i < p || i > q);
					if (i == 0 || j == 0 || i == r || j == r || (c && (i == j || i + j == r)))
						map.SetMapBit(i, j, true);
				}
			}
		}

		void ClearCenterOfMap(TerrainMap map)
		{
			int o = map.Cellwidth() >> 4;
			int p = (map.Cellwidth() - o) >> 1;
			int q = (map.Cellwidth() + o) >> 1;
			for (int i = p; i <= q; i++)
				for (int j = p; j <= q; j++)
					map.SetMapBit(i, j, false);
		}

		void DrawPathFencesOnMap(TerrainMap map, GCRoute path)
		{
			float xs = map.xSize / map.resolution;
			float zs = map.zSize / map.resolution;
			Vector3 alongRow = new Vector3(xs, 0, 0);
			Vector3 nextRow = new Vector3(-map.xSize, 0, zs);
			Vector3 g = new Vector3((map.xSize - xs) / -2, 0, (map.zSize - zs) / -2);
			for (int j = 0; j < map.resolution; j++)
			{
				for (int i = 0; i < map.resolution; i++)
				{
					float outside = path.HowFarOutsidePath(g);
					const float wallThickness = 1.0f;

					// set map cells adjacent to the outside edge of the path
					if ((outside > 0) && (outside < wallThickness))
						map.SetMapBit(i, j, true);

					// clear all other off-path map cells 
					if (outside > wallThickness) map.SetMapBit(i, j, false);

					g += alongRow;
				}
				g += nextRow;
			}
		}

		public override List<IVehicle> Vehicles
		{
			get { return _vehicles.ConvertAll<IVehicle>(v => (IVehicle) v); }
		}

		MapDriver _vehicle;
	    readonly List<MapDriver> _vehicles; // for allVehicles

		float _initCamDist, _initCamElev;

		bool _usePathFences;
		bool _useRandomRocks;
	}
}
