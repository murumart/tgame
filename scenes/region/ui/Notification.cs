using System;
using Godot;

namespace scenes.region.ui;

public partial class Notification : Control {

	[Export] Label label;
	[Export] Button dismissButton;


	public override void _Ready() {
		dismissButton.Pressed += QueueFree;
	}
	
	public void SetText(string text) {
		label.Text = text;
	}

	public void SetCallback(Action callback) {
		if (callback != null) dismissButton.Pressed += callback;
	}

}