using Godot;
using System;

namespace scenes.autoload {
	public partial class GameMan : Node {
		static GameMan singleton;
		public static GameMan Singleton { get => singleton; }
		Game game;
		public Game Game { get => game; }

		public float GameSpeed = 30.0f;

		public override void _Ready() {
			singleton = this;

			game = new(new Map());
		}

		public override void _Process(double delta) {
			Game.PassTime((float)delta * GameSpeed * Time.SECS_TO_HOURS);
		}
	}
}
