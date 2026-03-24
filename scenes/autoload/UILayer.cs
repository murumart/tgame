using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using scenes.region.ui;

namespace scenes.autoload {

	public partial class UILayer : CanvasLayer {

		[Export] HoverInfoPanel infoPanel;
		[Export] Control debugLabelParent;

		static UILayer singleton;

		struct DebugLabel(Label label, Func<string> callback) { public Label Label = label; public Func<string> Callback = callback; }
		readonly List<DebugLabel> labels = new();


		public override void _Ready() {
			Debug.Assert(singleton == null);
			singleton = this;
		}

		public override void _Process(double delta) {
			if (!debugLabelParent.Visible) return;
			for (int i = labels.Count - 1; i > -1; i--) {
				var label = labels[i];
				try {
					var str = label.Callback();
					label.Label.Text = str;
				} catch (Exception e) {
					GD.Print($"UILayer::_Process : debug label threw {e}");
					label.Label.QueueFree();
					labels.RemoveAt(i);
					continue;
				}
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

		public static void DebugDisplay(Func<string> output, string name = null) {
			var label = new Label {
				LabelSettings = GD.Load<LabelSettings>("res://resources/visual/theme/label_styles/debug.tres"),
				TabStops = [32f],
			};
			singleton.debugLabelParent.AddChild(label);
			singleton.labels.Add(new(label, output));
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
