using Godot;
using System;

namespace scenes.autoload {
	public partial class GameMan : Node {
		Game game;

		public float GameSpeed = 1.0f;

		public override void _Ready() {
			game = new(new Map());
		}

		public override void _Process(double delta) {
			game.PassTime((float)delta * GameSpeed);
		}
	}
}

