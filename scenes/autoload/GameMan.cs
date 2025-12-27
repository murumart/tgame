using System;
using System.Collections.Generic;
using Godot;
using resources.game;
using Environment = System.Environment;

namespace scenes.autoload {

	public partial class GameMan : Node {

		[Export] DataStorage dataRegistry;

		static GameMan singleton;
		public static GameMan Singleton { get => singleton; }
		Game game;
		public Game Game { get => game; }

		public enum GameSpeedChanger {
			UI,
		}

		Dictionary<GameSpeedChanger, float> gameSpeedMults = new();
		float gameSpeed;
		bool paused = false; public bool IsPaused { get => paused; }


		public override void _Ready() {
			AppDomain.CurrentDomain.UnhandledException += OnUnhandled;
			singleton = this;

			dataRegistry.RegisterThings();


			var map = Map.GetDebugMap();
			NewGame(map.GetRegion(0), map);

			GD.Print("GameMan is set up");

			//game.PassTime(60 * 7); // start game at 7:00
		}

		void OnUnhandled(object sender, UnhandledExceptionEventArgs e) {
			GD.PrintErr("Groaning in pain and dying");
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
		}

	}

}
