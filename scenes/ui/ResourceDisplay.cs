using System;
using System.Collections.Generic;
using Godot;

public partial class ResourceDisplay : PanelContainer {

	static readonly LabelSettings labelSettings = GD.Load<LabelSettings>("res://resources/visual/theme/label_styles/8px.tres");
	static readonly PackedScene panelScene = GD.Load<PackedScene>("res://scenes/ui/resource_display_hover_info.tscn");

	[Export] Container labelsParent;

	class LabelInfo(Label label, Func<string> display, Func<string> hoverInfo = null, Control hoverInfoControl = null, RichTextLabel hoverInfoLabel = null) {

		public readonly Label Label = label;
		public readonly Control HoverInfoPanel = hoverInfoControl;
		public readonly RichTextLabel HoverInfoLabel = hoverInfoLabel;
		public readonly Func<string> MainDisplay = display;
		public readonly Func<string> HoverInfo = hoverInfo;
		public bool Hovered = false;

		public void Display() {
			Label.Text = MainDisplay();
			if (HoverInfo != null && Hovered) {
				Debug.Assert(IsInstanceValid(HoverInfoLabel));
				HoverInfoLabel.Text = HoverInfo();
			}
		}

	}

	readonly List<LabelInfo> labels = new();


	public void Display(Func<string> what, Func<string> extraInfo = null) {
		Display(what, new() { MouseFilter = MouseFilterEnum.Stop }, extraInfo: extraInfo);
	}

	public void Display(Func<string> what, Label label, Func<string> extraInfo = null) {
		Debug.Assert(label != null && IsInstanceValid(label), $"Label {label} is invalid");
		labelsParent.AddChild(label);
		label.LabelSettings = labelSettings;

		var panel = panelScene.Instantiate<Control>();
		label.AddChild(panel);

		var labelInfo = new LabelInfo(label, what, hoverInfo: extraInfo, hoverInfoControl: panel, hoverInfoLabel: panel.GetNode<RichTextLabel>(panel.GetMeta("label", new NodePath()).AsNodePath()));

		if (extraInfo != null) {
			label.MouseEntered += () => {
				labelInfo.Hovered = true;
				panel.Show();
			};
			label.MouseExited += () => {
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
