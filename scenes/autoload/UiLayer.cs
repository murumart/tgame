using Godot;
using System;

namespace scenes.autoload {
	public partial class UiLayer : CanvasLayer {
		public static UiLayer Instance;

		public override void _Ready() {
			Instance = this;
		}

		public void AddUiChild(Node node) {
			AddChild(node);
		}
	}

}
