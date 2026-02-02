using System;
using System.Collections.Generic;
using Godot;
using resources.game;
using Environment = System.Environment;

namespace scenes.autoload {

	public partial class GameMan : Node {

		[Export] DataStorage dataRegistry;

		static GameMan singleton;
		public static GameMan Singleton => singleton;
		Game game;
		public Game Game => game;

		public enum GameSpeedChanger {
			UI,
		}

		readonly Dictionary<GameSpeedChanger, float> gameSpeedMults = new();
		float gameSpeed;
		bool paused = false;
		public bool IsPaused => paused;


		public override void _Ready() {
			AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
			Debug.Assert(singleton == null, "There GameMan can only GameMan Be One");
			singleton = this;

			dataRegistry.RegisterThings();
			GD.Print("GameMan::_Ready : GameMan is set up");

			// debug map
			var map = Map.GetDebugMap();
			NewGame(map.GetRegion(0), map);
		}

		void OnUnhandled(object sender, UnhandledExceptionEventArgs e) {
			GD.PrintErr("GameMan::OnUnhandled : Groaning in pain and dying");
			GetTree().Quit(1);
			Environment.Exit(1);
		}

		double timeAccum;
		const float gameMinutesPerRealSeconds = 2.0f;
		public override void _Process(double delta) {
			if (paused) return;
			float speed = gameSpeed;
			double toPass = delta * gameMinutesPerRealSeconds * (GameTime.SECS_TO_HOURS * GameTime.MINUTES_PER_HOUR) * speed;
			timeAccum += toPass;
			if (timeAccum >= 1) {
				Game.PassTime(1);
				timeAccum -= 1;
			}
		}

		public void MultiplyGameSpeed(GameSpeedChanger who, float amount) {
			gameSpeedMults[who] = amount;
			float a = 1f;
			foreach (float mult in gameSpeedMults.Values) a *= mult;
			gameSpeed = a;
		}

		public void TogglePause() {
			paused = !paused;
		}

		public void NewGame(Region playRegion, Map map) {
			game = new Game(playRegion, map);
			if (Game.Time.Minutes == 0) Game.PassTime(60 * 7); // start game at 7:00
		}

	}

}
