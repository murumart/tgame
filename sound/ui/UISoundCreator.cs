using Godot;
using System;

namespace sound.ui;

[GlobalClass]
public partial class UISoundCreator : AudioStreamPlayer
{

    [Export] AudioStream hoverSound;
    [Export] AudioStream clickSound;


    public override void _Ready() {

        void ApplyToButtons(Node n) {
            if (n is Button b) {
                b.MouseEntered += ElementHovered;
                b.FocusEntered += ElementHovered;
                b.Pressed += ElementClicked;
            }
            foreach (Node c in n.GetChildren()) ApplyToButtons(c);
        }
        
        ApplyToButtons(GetParent());

    }

    void ElementHovered() {
        Stream = hoverSound;
        Play();
    }

    void ElementClicked() {
        Stream = clickSound;
        Play();
    }

}
