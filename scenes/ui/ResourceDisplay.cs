using System;
using System.Collections.Generic;
using Godot;

public partial class ResourceDisplay : PanelContainer {

	static readonly LabelSettings labelSettings = GD.Load<LabelSettings>("res://resources/visual/theme/label_styles/8px.tres");
	static readonly PackedScene panelScene = GD.Load<PackedScene>("res://scenes/ui/resource_display_hover_info.tscn");

	[Export] Container labelsParent;

	class LabelInfo(Control target, Action<Control> display, Func<string> hoverInfo = null, Control hoverInfoControl = null, RichTextLabel hoverInfoLabel = null) {

		public readonly Control Target = target;
		public readonly Control HoverInfoPanel = hoverInfoControl;
		public readonly RichTextLabel HoverInfoLabel = hoverInfoLabel;
		public readonly Action<Control> MainDisplay = display;
		public readonly Func<String> HoverInfo = hoverInfo;
		public bool Hovered = false;

		public void Display() {
			MainDisplay(Target);
			if (HoverInfo != null && Hovered) {
				Debug.Assert(IsInstanceValid(HoverInfoLabel));
				HoverInfoLabel.Text = HoverInfo();
			}
		}

	}

	readonly List<LabelInfo> labels = new();


	public void Display(Action<Control> what, Func<string> extraInfo = null) {
		Display(what, new Label() {
			MouseFilter = MouseFilterEnum.Stop,
			LabelSettings = labelSettings,
		}, extraInfo: extraInfo);
	}

	public void Display(Action<Control> what, Control display, Func<string> extraInfo = null) {
		Debug.Assert(display != null && IsInstanceValid(display), $"Display {display} is invalid");
		labelsParent.AddChild(display);

		var panel = panelScene.Instantiate<Control>();
		display.AddChild(panel);

		var labelInfo = new LabelInfo(display, what, hoverInfo: extraInfo, hoverInfoControl: panel, hoverInfoLabel: panel.GetNode<RichTextLabel>(panel.GetMeta("label", new NodePath()).AsNodePath()));

		if (extraInfo != null) {
			display.MouseEntered += () => {
				labelInfo.Hovered = true;
				panel.Show();
			};
			display.MouseExited += () => {
				labelInfo.Hovered = false;
				panel.Hide();
			};
		}
		panel.Hide();

		labels.Add(labelInfo);
	}

	public void DisplayFat() {
		var fat = new Control {
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.Fill
		};
		labelsParent.AddChild(fat);
	}

	public void Display() {
		foreach (var l in labels) {
			l.Display();
		}
	}

	public void Reset() {
		labels.Clear();
		foreach (var child in labelsParent.GetChildren()) child.QueueFree();
	}

}
