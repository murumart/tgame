using System;
using System.Collections.Generic;
using Godot;
using resources.game;
using Environment = System.Environment;

namespace scenes.autoload {

	public partial class GameMan : Node {

		[Export] DataStorage dataRegistry;

		static GameMan singleton;
		public static Map DebugMap { get; private set; }
		public static GameMan Singleton => singleton;
		Game game;
		public Game Game => game;

		public enum GameSpeedChanger {
			UI,
		}

		readonly Dictionary<GameSpeedChanger, float> gameSpeedMults = new();
		float gameSpeed = 1f;
		bool paused = true;
		public bool IsPaused => paused;
		public float GameSpeed => gameSpeed;


		public override void _Ready() {
			Debug.Assert(singleton == null, "There GameMan can only GameMan Be One");
			singleton = this;

			dataRegistry.RegisterThings();
			GD.Print("GameMan::_Ready : GameMan is set up");

			DebugMap = Map.GetDebugMap();
			NewGame(DebugMap);
			Game.PlayRegion = DebugMap.GetRegion(0);
			GD.Print("GameMan::_Ready : debug map generated");
		}

		double timeAccum = 0.0;
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
			while (timeAccum >= 5) {
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

		public void NewGame(Map map) {
			game = new Game(map);
			if (Game.Time.Minutes == 0) Game.PassTime(60 * 7); // start game at 7:00
		}

	}

}
