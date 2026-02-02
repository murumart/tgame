using Godot;
using scenes.region.buildings;
using scenes.region.ui;
using System;
using System.Linq;

namespace scenes.autoload {
	public partial class UILayer : CanvasLayer {

		[Export] HoverInfoPanel infoPanel;
		[Export] Node debugLabelParent;

		static UILayer singleton;


		public override void _Ready() {
			singleton = this;
		}

		public override void _Process(double delta) {
			foreach (Label child in debugLabelParent.GetChildren().Cast<Label>()) {
				child.Text = child.GetMeta("callback").AsCallable().Call().AsString();
			}
		}

		public static void AddUIChild(Node node) {
			singleton.AddChild(node);
		}

		public static void DisplayInfopanel() {
			singleton.infoPanel.Show();
		}

		public static void HideInfopanel() {
			singleton.infoPanel.Hide();
		}

		public static void DebugDisplay(Func<string> output) {
			var label = new Label();
			singleton.debugLabelParent.AddChild(label);
			label.SetMeta("callback", Callable.From(output));
		}

	}

}
