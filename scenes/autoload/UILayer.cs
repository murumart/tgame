using System;
using System.Linq;
using Godot;
using scenes.region.ui;

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
				var cb = child.GetMeta("callback").AsCallable();

				if (!IsInstanceValid(cb.Target)) {
					child.QueueFree();
					continue;
				}
				child.Text = cb.Call().AsString();
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
			label.LabelSettings = GD.Load<LabelSettings>("res://resources/visual/theme/label_styles/debug.tres");
			singleton.debugLabelParent.AddChild(label);
			label.SetMeta("callback", Callable.From(output));
		}

	}

}
