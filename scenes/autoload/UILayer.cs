using Godot;
using System;

namespace scenes.autoload {
	public partial class UILayer : CanvasLayer {
		static UILayer singleton;
		public static UILayer Singleton { get => singleton; }

		public override void _Ready() {
			singleton = this;
		}

		public void AddUiChild(Node node) {
			AddChild(node);
		}
	}

}
