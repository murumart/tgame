using System;
using Godot;

namespace sound.ui;

[GlobalClass]
public partial class UISoundCreator : AudioStreamPlayer {

	readonly static AudioStream hoverSound = GD.Load<AudioStream>("res://sound/ui/menusounds-01.ogg");
	readonly static AudioStream clickSound = GD.Load<AudioStream>("res://sound/ui/menusounds-02.ogg");


	public override void _Ready() {
		VolumeDb = -20f;
		MaxPolyphony = 4;
		ApplyToButtons(GetParent());
	}

	public void ApplyToButtons(Node n) {
		if (n is Button b) {
			b.MouseEntered += () => ElementHovered(b);
			b.FocusEntered += () => ElementHovered(b);
			b.ButtonDown += () => ElementMouseDown(b);
			b.Pressed += () => ElementMouseUp(b);
		}
		foreach (Node c in n.GetChildren()) ApplyToButtons(c);
	}

	void ElementHovered(Control c) {
		if (!IsInsideTree()) return;
		if (c is Button b && b.Disabled) return;
		Stream = hoverSound;
		Play();
	}

	void ElementMouseUp(Control c) {
		if (!IsInsideTree()) return;
		if (c is Button b && b.Disabled) return;
		Stream = clickSound;
		Play();
	}

	void ElementMouseDown(Control c) {
		if (!IsInsideTree()) return;
		if (c is Button b && b.Disabled) return;
		Stream = hoverSound;
		Play();
	}

}
