using System;
using Godot;

namespace scenes.autoload {

	public partial class GameMan : Node {

		[Export] DataStorage dataRegistry;

		static GameMan singleton;
		public static GameMan Singleton { get => singleton; }
		Game game;
		public Game Game { get => game; }

		public float GameMinutesPerRealSeconds = 2.0f;

		public override void _Ready() {
			singleton = this;

			dataRegistry.RegisterThings();

			game = new(new Map());

			game.PassTime(60 * 7);
		}

		double timeAccum;
		public override void _Process(double delta) {
			double toPass = delta * GameMinutesPerRealSeconds * (GameTime.SECS_TO_HOURS * GameTime.MINUTES_PER_HOUR);
			timeAccum += toPass;
			if (timeAccum >= 1) {
				Game.PassTime(1);
				timeAccum -= 1;
			}
		}

	}

}
