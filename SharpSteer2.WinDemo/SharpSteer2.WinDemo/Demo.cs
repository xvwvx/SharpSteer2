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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpSteer2.Helpers;
using SharpSteer2.WinDemo.PlugIns.Boids;
using SharpSteer2.WinDemo.PlugIns.Ctf;
using SharpSteer2.WinDemo.PlugIns.LowSpeedTurn;
using SharpSteer2.WinDemo.PlugIns.MapDrive;
using SharpSteer2.WinDemo.PlugIns.MultiplePursuit;
using SharpSteer2.WinDemo.PlugIns.OneTurning;
using SharpSteer2.WinDemo.PlugIns.Pedestrian;
using SharpSteer2.WinDemo.PlugIns.Soccer;

// Boenjr?
namespace SharpSteer2.WinDemo
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Demo : Game
	{
		// these are the size of the offscreen drawing surface
		// in general, no one wants to change these as there
		// are all kinds of UI calculations and positions based
		// on these dimensions.

	    // these are the size of the output window, ignored
		// on Xbox 360
	    private const int PREFERRED_WINDOW_WIDTH = 1024;
	    private const int PREFERRED_WINDOW_HEIGHT = 640;

	    public readonly GraphicsDeviceManager Graphics;
		public Effect Effect;
		EffectParameter _effectParamWorldViewProjection;

	    readonly ContentManager _content;
		FixedFont _courierFont;
		SpriteBatch _spriteBatch;

		public Matrix WorldMatrix;
		public Matrix ViewMatrix;
		public Matrix ProjectionMatrix;
		VertexDeclaration _vertexDeclaration;

        BoidsPlugIn _boids;
        LowSpeedTurnPlugIn _lowSpeedTurn;
        PedestrianPlugIn _pedestrian;
        CtfPlugIn _ctf;
        MapDrivePlugIn _mapDrive;
        MpPlugIn _multiplePersuit;
        SoccerPlugIn _soccer;
        OneTurningPlugIn _oneTurning;

		// currently selected plug-in (user can choose or cycle through them)
		public static PlugIn SelectedPlugIn = null;

		// currently selected vehicle.  Generally the one the camera follows and
		// for which additional information may be displayed.  Clicking the mouse
		// near a vehicle causes it to become the Selected Vehicle.
		public static IVehicle SelectedVehicle = null;

		public static readonly Clock Clock = new Clock();
		public static readonly Camera Camera = new Camera();

		// some camera-related default constants
		public const float CAMERA2_D_ELEVATION = 8;
		public const float CAMERA_TARGET_DISTANCE = 13;
		public static readonly Vector3 CameraTargetOffset = new Vector3(0, CAMERA2_D_ELEVATION, 0);

	    readonly Annotation _annotations = new Annotation();

		public Demo()
		{
			Drawing.game = this;

			Graphics = new GraphicsDeviceManager(this);
			_content = new ContentManager(Services);

			Graphics.PreferredBackBufferWidth = PREFERRED_WINDOW_WIDTH;
			Graphics.PreferredBackBufferHeight = PREFERRED_WINDOW_HEIGHT;

			_texts = new List<TextEntry>();

			//FIXME: eijeijei.
			Annotation.drawer = new Drawing();

            _boids = new BoidsPlugIn(_annotations);
            _lowSpeedTurn = new LowSpeedTurnPlugIn(_annotations);
            _pedestrian = new PedestrianPlugIn(_annotations);
            _ctf = new CtfPlugIn(_annotations);
            _mapDrive = new MapDrivePlugIn(_annotations);
            _multiplePersuit = new MpPlugIn(_annotations);
            _soccer = new SoccerPlugIn(_annotations);
            _oneTurning = new OneTurningPlugIn(_annotations);

			IsFixedTimeStep = false;
		}

		public static void Init2dCamera(IVehicle selected)
		{
			Init2dCamera(selected, CAMERA_TARGET_DISTANCE, CAMERA2_D_ELEVATION);
		}

		public static void Init2dCamera(IVehicle selected, float distance, float elevation)
		{
			Position2dCamera(selected, distance, elevation);
			Camera.FixedDistanceDistance = distance;
			Camera.FixedDistanceVerticalOffset = elevation;
			Camera.Mode = Camera.CameraMode.FixedDistanceOffset;
		}

		public static void Init3dCamera(IVehicle selected)
		{
			Init3dCamera(selected, CAMERA_TARGET_DISTANCE, CAMERA2_D_ELEVATION);
		}
		public static void Init3dCamera(IVehicle selected, float distance, float elevation)
		{
			Position3dCamera(selected, distance, elevation);
			Camera.FixedDistanceDistance = distance;
			Camera.FixedDistanceVerticalOffset = elevation;
			Camera.Mode = Camera.CameraMode.FixedDistanceOffset;
		}

		public static void Position2dCamera(IVehicle selected)
		{
			Position2dCamera(selected, CAMERA_TARGET_DISTANCE, CAMERA2_D_ELEVATION);
		}

		public static void Position2dCamera(IVehicle selected, float distance, float elevation)
		{
			// position the camera as if in 3d:
			Position3dCamera(selected, distance, elevation);

			// then adjust for 3d:
			Vector3 position3d = Camera.Position;
			position3d.Y += elevation;
			Camera.Position = (position3d);
		}

		public static void Position3dCamera(IVehicle selected)
		{
			Position3dCamera(selected, CAMERA_TARGET_DISTANCE, CAMERA2_D_ELEVATION);
		}

		public static void Position3dCamera(IVehicle selected, float distance, float elevation)
		{
			SelectedVehicle = selected;
			if (selected != null)
			{
				Vector3 behind = selected.Forward * -distance;
				Camera.Position = (selected.Position + behind);
				Camera.Target = selected.Position;
			}
		}

		// camera updating utility used by several (all?) plug-ins
		public static void UpdateCamera(float currentTime, float elapsedTime, IVehicle selected)
		{
			Camera.VehicleToTrack = selected;
			Camera.Update(currentTime, elapsedTime, Clock.PausedState);
		}

		// ground plane grid-drawing utility used by several plug-ins
		public static void GridUtility(Vector3 gridTarget)
		{
			// Math.Round off target to the nearest multiple of 2 (because the
			// checkboard grid with a pitch of 1 tiles with a period of 2)
			// then lower the grid a bit to put it under 2d annotation lines
			Vector3 gridCenter = new Vector3((float)(Math.Round(gridTarget.X * 0.5f) * 2),
								   (float)(Math.Round(gridTarget.Y * 0.5f) * 2) - .05f,
								   (float)(Math.Round(gridTarget.Z * 0.5f) * 2));

			// colors for checkboard
			Color gray1 = new Color(new Vector3(0.27f));
			Color gray2 = new Color(new Vector3(0.30f));

			// draw 50x50 checkerboard grid with 50 squares along each side
			Drawing.DrawXZCheckerboardGrid(50, 50, gridCenter, gray1, gray2);

			// alternate style
			//Bnoerj.AI.Steering.Draw.drawXZLineGrid(50, 50, gridCenter, Color.Black);
		}

		// draws a gray disk on the XZ plane under a given vehicle
		public static void HighlightVehicleUtility(IVehicle vehicle)
		{
			if (vehicle != null)
			{
				Drawing.DrawXZDisk(vehicle.Radius, vehicle.Position, Color.LightGray, 20);
			}
		}

		// draws a gray circle on the XZ plane under a given vehicle
		public static void CircleHighlightVehicleUtility(IVehicle vehicle)
		{
			if (vehicle != null)
			{
				Drawing.DrawXZCircle(vehicle.Radius * 1.1f, vehicle.Position, Color.LightGray, 20);
			}
		}

		// draw a box around a vehicle aligned with its local space
		// xxx not used as of 11-20-02
		public static void DrawBoxHighlightOnVehicle(IVehicle v, Color color)
		{
			if (v != null)
			{
				float diameter = v.Radius * 2;
				Vector3 size = new Vector3(diameter, diameter, diameter);
				Drawing.DrawBoxOutline(v, size, color);
			}
		}

		// draws a colored circle (perpendicular to view axis) around the center
		// of a given vehicle.  The circle's radius is the vehicle's radius times
		// radiusMultiplier.
		public static void DrawCircleHighlightOnVehicle(IVehicle v, float radiusMultiplier, Color color)
		{
			if (v != null)
			{
				Vector3 cPosition = Camera.Position;
				Drawing.Draw3dCircle(
					v.Radius * radiusMultiplier,  // adjusted radius
					v.Position,                   // center
					v.Position - cPosition,       // view axis
					color,                        // drawing color
					20);                          // circle segments
			}
		}

		// Find the AbstractVehicle whose screen position is nearest the current the
		// mouse position.  Returns NULL if mouse is outside this window or if
		// there are no AbstractVehicle.
		internal static IVehicle VehicleNearestToMouse()
		{
			return null;//findVehicleNearestScreenPosition(mouseX, mouseY);
		}

		// Find the AbstractVehicle whose screen position is nearest the given window
		// coordinates, typically the mouse position.  Returns NULL if there are no
		// AbstractVehicles.
		//
		// This works by constructing a line in 3d space between the camera location
		// and the "mouse point".  Then it measures the distance from that line to the
		// centers of each AbstractVehicle.  It returns the AbstractVehicle whose
		// distance is smallest.
		//
		// xxx Issues: Should the distanceFromLine test happen in "perspective space"
		// xxx or in "screen space"?  Also: I think this would be happy to select a
		// xxx vehicle BEHIND the camera location.
		internal static IVehicle findVehicleNearestScreenPosition(int x, int y)
		{
			// find the direction from the camera position to the given pixel
			Vector3 direction = DirectionFromCameraToScreenPosition(x, y);

			// iterate over all vehicles to find the one whose center is nearest the
			// "eye-mouse" selection line
			float minDistance = float.MaxValue;       // smallest distance found so far
			IVehicle nearest = null;   // vehicle whose distance is smallest
			List<IVehicle> vehicles = AllVehiclesOfSelectedPlugIn();
			foreach (IVehicle vehicle in vehicles)
			{
				// distance from this vehicle's center to the selection line:
				float d = Vector3Helpers.DistanceFromLine(vehicle.Position, Camera.Position, direction);

				// if this vehicle-to-line distance is the smallest so far,
				// store it and this vehicle in the selection registers.
				if (d < minDistance)
				{
					minDistance = d;
					nearest = vehicle;
				}
			}

			return nearest;
		}

		// return a normalized direction vector pointing from the camera towards a
		// given point on the screen: the ray that would be traced for that pixel
		static Vector3 DirectionFromCameraToScreenPosition(int x, int y)
		{
#if TODO
			// Get window height, viewport, modelview and projection matrices
			// Unproject mouse position at near and far clipping planes
			gluUnProject(x, h - y, 0, mMat, pMat, vp, &un0x, &un0y, &un0z);
			gluUnProject(x, h - y, 1, mMat, pMat, vp, &un1x, &un1y, &un1z);

			// "direction" is the normalized difference between these far and near
			// unprojected points.  Its parallel to the "eye-mouse" selection line.
			Vector3 diffNearFar = new Vector3(un1x - un0x, un1y - un0y, un1z - un0z);
			Vector3 direction = diffNearFar.normalize();
			return direction;
#else
			return Vector3.Up;
#endif
		}

		// select the "next" plug-in, cycling through "plug-in selection order"
		static void SelectDefaultPlugIn()
		{
			PlugIn.SortBySelectionOrder();
			SelectedPlugIn = PlugIn.FindDefault();
		}

		// open the currently selected plug-in
		static void OpenSelectedPlugIn()
		{
			Camera.Reset();
			SelectedVehicle = null;
			SelectedPlugIn.Open();
		}

		static void ResetSelectedPlugIn()
		{
			SelectedPlugIn.Reset();
		}

		static void CloseSelectedPlugIn()
		{
			SelectedPlugIn.Close();
			SelectedVehicle = null;
		}

		// return a group (an STL vector of AbstractVehicle pointers) of all
		// vehicles(/agents/characters) defined by the currently selected PlugIn
		static List<IVehicle> AllVehiclesOfSelectedPlugIn()
		{
			return SelectedPlugIn.Vehicles;
		}

		// select the "next" vehicle: the one listed after the currently selected one
		// in allVehiclesOfSelectedPlugIn
		static void SelectNextVehicle()
		{
			if (SelectedVehicle != null)
			{
				// get a container of all vehicles
				List<IVehicle> all = AllVehiclesOfSelectedPlugIn();

				// find selected vehicle in container
				int i = all.FindIndex(v => v != null && v == SelectedVehicle);
				if (i >= 0 && i < all.Count)
				{
					if (i == all.Count - 1)
					{
						// if we are at the end of the container, select the first vehicle
						SelectedVehicle = all[0];
					}
					else
					{
						// normally select the next vehicle in container
						SelectedVehicle = all[i + 1];
					}
				}
				else
				{
					// if the search failed, use NULL
					SelectedVehicle = null;
				}
			}
		}

		void UpdateSelectedPlugIn(float currentTime, float elapsedTime)
		{
			// switch to Update phase
			PushPhase(Phase.Update);

			// service queued reset request, if any
			DoDelayedResetPlugInXXX();

			// if no vehicle is selected, and some exist, select the first one
			if (SelectedVehicle == null)
			{
				List<IVehicle> all = AllVehiclesOfSelectedPlugIn();
				if (all.Count > 0)
					SelectedVehicle = all[0];
			}

			// invoke selected PlugIn's Update method
			SelectedPlugIn.Update(currentTime, elapsedTime);

			// return to previous phase
			PopPhase();
		}

		static bool _delayedResetPlugInXXX = false;
		internal static void QueueDelayedResetPlugInXXX()
		{
			_delayedResetPlugInXXX = true;
		}

		static void DoDelayedResetPlugInXXX()
		{
			if (_delayedResetPlugInXXX)
			{
				ResetSelectedPlugIn();
				_delayedResetPlugInXXX = false;
			}
		}

		void PushPhase(Phase newPhase)
		{
			// update timer for current (old) phase: add in time since last switch
			UpdatePhaseTimers();

			// save old phase
			_phaseStack[_phaseStackIndex++] = _phase;

			// set new phase
			_phase = newPhase;

			// check for stack overflow
			if (_phaseStackIndex >= PHASE_STACK_SIZE)
			{
				throw new ArgumentOutOfRangeException("phaseStack overflow");
			}
		}

		void PopPhase()
		{
			// update timer for current (old) phase: add in time since last switch
			UpdatePhaseTimers();

			// restore old phase
			_phase = _phaseStack[--_phaseStackIndex];
		}

		// redraw graphics for the currently selected plug-in
		void RedrawSelectedPlugIn(float currentTime, float elapsedTime)
		{
			// switch to Draw phase
			PushPhase(Phase.Draw);

			// invoke selected PlugIn's Draw method
			SelectedPlugIn.Redraw(currentTime, elapsedTime);

			// draw any annotation queued up during selected PlugIn's Update method
			Drawing.AllDeferredLines();
			Drawing.AllDeferredCirclesOrDisks();

			// return to previous phase
			PopPhase();
		}

		int frameRatePresetIndex = 0;

		// cycle through frame rate presets  (XXX move this to OpenSteerDemo)
		void SelectNextPresetFrameRate()
		{
			// note that the cases are listed in reverse order, and that 
			// the default is case 0 which causes the index to wrap around
			switch (++frameRatePresetIndex)
			{
			case 3:
				// animation mode at 60 fps
				Clock.FixedFrameRate = 60;
				Clock.AnimationMode = true;
				Clock.VariableFrameRateMode = false;
				break;
			case 2:
				// real-time fixed frame rate mode at 60 fps
				Clock.FixedFrameRate = 60;
				Clock.AnimationMode = false;
				Clock.VariableFrameRateMode = false;
				break;
			case 1:
				// real-time fixed frame rate mode at 24 fps
				Clock.FixedFrameRate = 24;
				Clock.AnimationMode = false;
				Clock.VariableFrameRateMode = false;
				break;
			case 0:
			default:
				// real-time variable frame rate mode ("as fast as possible")
				frameRatePresetIndex = 0;
				Clock.FixedFrameRate = 0;
				Clock.AnimationMode = false;
				Clock.VariableFrameRateMode = true;
				break;
			}
		}

		private void SelectNextPlugin()
		{
			CloseSelectedPlugIn();
			SelectedPlugIn = SelectedPlugIn.Next();
			OpenSelectedPlugIn();
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// TODO: Add your initialization logic here
			SelectDefaultPlugIn();
			OpenSelectedPlugIn();

			base.Initialize();
		}

		/// <summary>
		/// Load your graphics content.  If loadAllContent is true, you should
		/// load content from both ResourceManagementMode pools.  Otherwise, just
		/// load ResourceManagementMode.Manual content.
		/// </summary>
        protected override void LoadContent() {
            base.LoadContent();
			// TODO: Load any ResourceManagementMode.Automatic content
			_courierFont = new FixedFont(_content.Load<Texture2D>("Content/Fonts/Courier"));

			_spriteBatch = new SpriteBatch(Graphics.GraphicsDevice);

			Effect = _content.Load<Effect>("Content/Shaders/Simple");
			_effectParamWorldViewProjection = Effect.Parameters["WorldViewProjection"];

			// TODO: Load any ResourceManagementMode.Manual content
            _vertexDeclaration = new VertexDeclaration(VertexPositionTexture.VertexDeclaration.GetVertexElements());
		}

		/// <summary>
		/// Unload your graphics content.  If unloadAllContent is true, you should
		/// unload content from both ResourceManagementMode pools.  Otherwise, just
		/// unload ResourceManagementMode.Manual content.  Manual content will get
		/// Disposed by the GraphicsDevice during a Reset.
		/// </summary>
		protected override void UnloadContent()
		{
			_content.Unload();
		}

		KeyboardState _prevKeyState = new KeyboardState();

		bool IsKeyDown(KeyboardState keyState, Keys key)
		{
			return _prevKeyState.IsKeyDown(key) == false && keyState.IsKeyDown(key) == true;
		}

		protected override void Update(GameTime gameTime)
		{
			// Allows the default game to exit on Xbox 360 and Windows
			GamePadState padState = GamePad.GetState(PlayerIndex.One);
			KeyboardState keyState = Keyboard.GetState();
			if (padState.Buttons.Back == ButtonState.Pressed ||
				keyState.IsKeyDown(Keys.Escape))
			{
				Exit();
			}

			if (IsKeyDown(keyState, Keys.R))
			{
				ResetSelectedPlugIn();
			}
			if (IsKeyDown(keyState, Keys.S))
			{
				SelectNextVehicle();
			}
			if (IsKeyDown(keyState, Keys.A))
			{
                _annotations.IsEnabled = !_annotations.IsEnabled;
			}
			if (IsKeyDown(keyState, Keys.Space))
			{
				Clock.TogglePausedState();
			}
			if (IsKeyDown(keyState, Keys.C))
			{
				Camera.SelectNextMode();
			}
			if (IsKeyDown(keyState, Keys.F))
			{
				SelectNextPresetFrameRate();
			}
			if (IsKeyDown(keyState, Keys.Tab))
			{
				SelectNextPlugin();
			}

			for (Keys key = Keys.F1; key <= Keys.F10; key++)
			{
				if (IsKeyDown(keyState, key))
				{
					SelectedPlugIn.HandleFunctionKeys(key);
				}
			}

			_prevKeyState = keyState;

			// TODO: Add your update logic here

			// update global simulation clock
			Clock.Update();

			//  start the phase timer (XXX to accurately measure "overhead" time this
			//  should be in displayFunc, or somehow account for time outside this
			//  routine)
			InitPhaseTimers();

			// run selected PlugIn (with simulation's current time and step size)
			UpdateSelectedPlugIn(Clock.TotalSimulationTime, Clock.ElapsedSimulationTime);

			WorldMatrix = Matrix.Identity;

			Vector3 pos = Camera.Position;
			Vector3 lookAt = Camera.Target;
			Vector3 up = Camera.Up;
			ViewMatrix = Matrix.CreateLookAt(new Vector3(pos.X, pos.Y, pos.Z), new Vector3(lookAt.X, lookAt.Y, lookAt.Z), new Vector3(up.X, up.Y, up.Z));

			ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45),  // 45 degree angle
				(float)Graphics.GraphicsDevice.Viewport.Width / (float)Graphics.GraphicsDevice.Viewport.Height,
				1.0f, 400.0f);

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			Graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            Graphics.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            Graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            //HACK
            //graphics.GraphicsDevice.RasterizerState.CullMode = CullMode.CullClockwiseFace;

            //original
            //graphics.GraphicsDevice.RenderState.DepthBufferEnable = true;
            //graphics.GraphicsDevice.RenderState.DepthBufferWriteEnable = true;
            //graphics.GraphicsDevice.RenderState.DepthBufferFunction = CompareFunction.Less;
            //graphics.GraphicsDevice.RenderState.CullMode = CullMode.CullClockwiseFace;
            //graphics.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            //graphics.GraphicsDevice.RenderState.AlphaBlendOperation = BlendFunction.Add;

			Matrix worldViewProjection = WorldMatrix * ViewMatrix * ProjectionMatrix;
			_effectParamWorldViewProjection.SetValue(worldViewProjection);

            //TODO this might break
            //graphics.GraphicsDevice.SetVertexBuffer(vertexDeclaration);

			//FIXME: Probably not the best way to do it, but it works...
            //effect.Begin();
			Effect.CurrentTechnique.Passes[0].Apply();

			// redraw selected PlugIn (based on real time)
			RedrawSelectedPlugIn(Clock.TotalRealTime, Clock.ElapsedRealTime);

            //effect.CurrentTechnique.Passes[0].End();
            //effect.End();

			// Draw some sample text.
			_spriteBatch.Begin();

			float cw = _courierFont.Size.X; // xxx character width
			float lh = _courierFont.Size.Y; // xxx line height

			foreach (TextEntry text in _texts)
			{
				_courierFont.Draw(text.Text, text.Position, 1.0f, text.Color, _spriteBatch);
			}
			_texts.Clear();

			// get smoothed phase timer information
			float ptd = PhaseTimerDraw;
			float ptu = PhaseTimerUpdate;
			float pto = PhaseTimerOverhead;
			float smoothRate = Clock.SmoothingRate;
			Utilities.BlendIntoAccumulator(smoothRate, ptd, ref _smoothedTimerDraw);
			Utilities.BlendIntoAccumulator(smoothRate, ptu, ref _smoothedTimerUpdate);
			Utilities.BlendIntoAccumulator(smoothRate, pto, ref _smoothedTimerOverhead);

			// keep track of font metrics and start of next line
			Vector2 screenLocation = new Vector2(lh, lh);

			String str;
			str = String.Format("Camera: {0}", Camera.ModeName);
			_courierFont.Draw(str, screenLocation, 1.0f, Color.White, _spriteBatch);
			screenLocation.Y += lh;
			str = String.Format("PlugIn: {0}", SelectedPlugIn.Name);
			_courierFont.Draw(str, screenLocation, 1.0f, Color.White, _spriteBatch);

			screenLocation = new Vector2(lh, PREFERRED_WINDOW_HEIGHT - 5.5f * lh);

			str = String.Format("Update: {0}", GetPhaseTimerFps(_smoothedTimerUpdate));
			_courierFont.Draw(str, screenLocation, 1.0f, Color.White, _spriteBatch);
			screenLocation.Y += lh;
			str = String.Format("Draw:   {0}", GetPhaseTimerFps(_smoothedTimerDraw));
			_courierFont.Draw(str, screenLocation, 1.0f, Color.White, _spriteBatch);
			screenLocation.Y += lh;
			str = String.Format("Other:  {0}", GetPhaseTimerFps(_smoothedTimerOverhead));
			_courierFont.Draw(str, screenLocation, 1.0f, Color.White, _spriteBatch);
			screenLocation.Y += 1.5f * lh;

			// target and recent average frame rates
			int targetFPS = Clock.FixedFrameRate;
			float smoothedFPS = Clock.SmoothedFPS;

			// describe clock mode and frame rate statistics
			StringBuilder sb = new StringBuilder();
			sb.Append("Clock: ");
			if (Clock.AnimationMode)
			{
				float ratio = smoothedFPS / targetFPS;
				sb.AppendFormat("animation mode ({0} fps, display {1} fps {2}% of nominal speed)",
					targetFPS, Math.Round(smoothedFPS), (int)(100 * ratio));
			}
			else
			{
				sb.Append("real-time mode, ");
				if (Clock.VariableFrameRateMode)
				{
					sb.AppendFormat("variable frame rate ({0} fps)", Math.Round(smoothedFPS));
				}
				else
				{
					sb.AppendFormat("fixed frame rate (target: {0} actual: {1}, ", targetFPS, Math.Round(smoothedFPS));

					// create usage description character string
					str = String.Format("usage: {0:0}%", Clock.SmoothedUsage);
					float x = screenLocation.X + sb.Length * cw;

					for (int i = 0; i < str.Length; i++) sb.Append(" ");
					sb.Append(")");

					// display message in lower left corner of window
					// (draw in red if the instantaneous usage is 100% or more)
					float usage = Clock.Usage;
					Color color = (usage >= 100) ? Color.Red : Color.White;
					_courierFont.Draw(str, new Vector2(x, screenLocation.Y), 1, color, _spriteBatch);
				}
			}
			str = sb.ToString();
			_courierFont.Draw(str, screenLocation, 1.0f, Color.White, _spriteBatch);

			_spriteBatch.End();

			base.Draw(gameTime);
		}

		static String GetPhaseTimerFps(float phaseTimer)
		{
			// different notation for variable and fixed frame rate
			if (Clock.VariableFrameRateMode)
			{
				// express as FPS (inverse of phase time)
				return String.Format("{0:0.00000} ({1:0} FPS)", phaseTimer, 1 / phaseTimer);
			}
			else
			{
				// quantify time as a percentage of frame time
				double fps = Clock.FixedFrameRate;// 1.0f / TargetElapsedTime.TotalSeconds;
				return String.Format("{0:0.00000} ({1:0}% of 1/{2}sec)", phaseTimer, (100.0f * phaseTimer) / (1.0f / fps), (int)fps);
			}
		}

		public enum Phase
		{
			Overhead,
			Update,
			Draw,
			Count
		}
		static Phase _phase;
		const int PHASE_STACK_SIZE = 5;
		static readonly Phase[] _phaseStack = new Phase[PHASE_STACK_SIZE];
		static int _phaseStackIndex = 0;
		static readonly float[] _phaseTimers = new float[(int)Phase.Count];
		static float _phaseTimerBase = 0;

		// draw text showing (smoothed, rounded) "frames per second" rate
		// (and later a bunch of related stuff was dumped here, a reorg would be nice)
		static float _smoothedTimerDraw = 0;
		static float _smoothedTimerUpdate = 0;
		static float _smoothedTimerOverhead = 0;

		public static bool IsDrawPhase
		{
			get { return _phase == Phase.Draw; }
		}

		float PhaseTimerDraw
		{
			get { return _phaseTimers[(int)Phase.Draw]; }
		}
		float PhaseTimerUpdate
		{
			get { return _phaseTimers[(int)Phase.Update]; }
		}
		// XXX get around shortcomings in current implementation, see note
		// XXX in updateSimulationAndRedraw
#if IGNORE
		float phaseTimerOverhead
		{
			get { return phaseTimers[(int)Phase.overheadPhase]; }
		}
#else
		float PhaseTimerOverhead
		{
			get { return Clock.ElapsedRealTime - (PhaseTimerDraw + PhaseTimerUpdate); }
		}
#endif

		void InitPhaseTimers()
		{
			_phaseTimers[(int)Phase.Draw] = 0;
			_phaseTimers[(int)Phase.Update] = 0;
			_phaseTimers[(int)Phase.Overhead] = 0;
			_phaseTimerBase = Clock.TotalRealTime;
		}

		void UpdatePhaseTimers()
		{
			float currentRealTime = Clock.RealTimeSinceFirstClockUpdate();
			_phaseTimers[(int)_phase] += currentRealTime - _phaseTimerBase;
			_phaseTimerBase = currentRealTime;
		}

	    readonly List<TextEntry> _texts;
		public void AddText(TextEntry text)
		{
			_texts.Add(text);
		}
	}
}
