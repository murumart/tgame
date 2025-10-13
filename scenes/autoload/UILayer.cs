using Godot;
using System;

namespace scenes.autoload {
	public partial class UILayer : CanvasLayer {
		public static UILayer Instance;

		public override void _Ready() {
			Instance = this;
		}

		public void AddUiChild(Node node) {
			AddChild(node);
		}
	}

}
