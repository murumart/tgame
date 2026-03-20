using System;
using Godot;

namespace sound.ui;

[GlobalClass]
public partial class UISoundCreator : AudioStreamPlayer {

	[Export] AudioStream hoverSound;
	[Export] AudioStream clickSound;


	public override void _Ready() {
		ApplyToButtons(GetParent());
	}

	public void ApplyToButtons(Node n) {
		if (n is Button b) {
			b.MouseEntered += ElementHovered;
			b.FocusEntered += ElementHovered;
			b.Pressed += ElementClicked;
		}
		foreach (Node c in n.GetChildren()) ApplyToButtons(c);
	}

	void ElementHovered() {
		if (!IsInsideTree()) return;
		Stream = hoverSound;
		Play();
	}

	void ElementClicked() {
		if (!IsInsideTree()) return;
		Stream = clickSound;
		Play();
	}

}
