using System;
using System.Linq;
using Godot;
using scenes.region.ui;

namespace scenes.autoload {

	public partial class UILayer : CanvasLayer {

		[Export] HoverInfoPanel infoPanel;
		[Export] Control debugLabelParent;

		static UILayer singleton;


		public override void _Ready() {
			Debug.Assert(singleton == null);
			singleton = this;
		}

		public override void _Process(double delta) {
			foreach (Label child in debugLabelParent.GetChildren().Cast<Label>()) {
				var cb = child.GetMeta("callback").AsCallable();

				if (!IsInstanceValid(cb.Target)) {
					GD.Print($"UILayer::_Process : debug label callback cb.Target is invalid");
					child.QueueFree();
					continue;
				}
				var str = cb.Call().AsString();
				child.Text = str;
			}
		}

		public override void _UnhandledKeyInput(InputEvent @event) {
			var e = @event as InputEventKey;
			if (e.Keycode == Key.Key7 && e.Pressed) debugLabelParent.Visible = !debugLabelParent.Visible;
			if (e.Keycode == Key.F12 && e.Pressed) Screenshot();
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
			var label = new Label {
				LabelSettings = GD.Load<LabelSettings>("res://resources/visual/theme/label_styles/debug.tres"),
				TabStops = [32f],
			};
			singleton.debugLabelParent.AddChild(label);
			label.SetMeta("callback", Callable.From(output));
		}

		public static void Screenshot() {
			if (!DirAccess.DirExistsAbsolute("user://fevered_world/screenshots")) {
				DirAccess.MakeDirRecursiveAbsolute("user://fevered_world/screenshots");
			}
			var img = singleton.GetViewport().GetTexture().GetImage();
			string name = "" + Time.GetDatetimeStringFromSystem();
			if (!name.IsValidFileName()) name = "" + name.Hash();
			name += ".png";
			img.SavePng("user://fevered_world/screenshots/" + name);
		}

	}

}
