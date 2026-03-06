using System;
using Godot;

namespace scenes.ui;

public partial class MainMenu : Control {

	[Export] Button freePlayButton;
	[Export] Button exitButton;


	public override void _Ready() {
		freePlayButton.Pressed += () => {
			GetTree().ChangeSceneToFile("res://scenes/map/world_man.tscn");
		};
		exitButton.Pressed += () => {
			GetTree().Quit();
		};

	}

}
