using Godot;
using scenes.region.buildings;
using scenes.region.ui;
using System;

namespace scenes.autoload {
	public partial class UILayer : CanvasLayer {
		[Export] HoverInfoPanel infoPanel;

		static UILayer singleton;
		public static UILayer Singleton { get => singleton; }

		public override void _Ready() {
			singleton = this;
		}

		public static void AddUiChild(Node node) {
			Singleton.AddChild(node);
		}

		public static void DisplayInfopanel(BuildingView info) {
			Singleton.infoPanel.Show();
		}

		public static void HideInfopanel() {
			Singleton.infoPanel.Hide();
		}
	}

}
