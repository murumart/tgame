using Godot;
using System;

namespace scenes.autoload {

	public partial class GameMan : Node {

		[Export] DataStorage dataRegistry;

		static GameMan singleton;
		public static GameMan Singleton { get => singleton; }
		Game game;
		public Game Game { get => game; }

		public float GameSpeed = 30.0f;

		public override void _Ready() {
			singleton = this;

			dataRegistry.RegisterThings();

			game = new(new Map());
		}

		public override void _Process(double delta) {
			Game.PassTime((float)delta * GameSpeed * Time.SECS_TO_HOURS);
		}

	}

}
