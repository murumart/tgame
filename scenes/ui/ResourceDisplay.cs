using System;
using System.Collections.Generic;
using Godot;

public partial class ResourceDisplay : PanelContainer {

	static readonly LabelSettings labelSettings = GD.Load<LabelSettings>("res://resources/visual/theme/label_styles/8px.tres");

	[Export] Container labelsParent;

	readonly struct LaBel(Label label, Func<string> display) {

		public readonly void Display() {
			label.Text = display();
		}

	}

	readonly List<LaBel> labels = new();


	public void Display(Func<string> what) {
		Display(what, new());
	}

	public void Display(Func<string> what, Label label) {
		Debug.Assert(label != null && IsInstanceValid(label), $"Label {label} is invalid");
		labelsParent.AddChild(label);
		label.LabelSettings = labelSettings;
		labels.Add(new(label, what));
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
