using System;
using Godot;

namespace scenes.ui;

public partial class MainMenu : Control {

	[Export] Button scenarioPlayButton;
	[Export] Button freePlayButton;
	[Export] Button exitButton;

    public static bool useScenarioWorld = false;


	public override void _Ready() {
		scenarioPlayButton.Pressed += () => {
			useScenarioWorld = true;
			GetTree().ChangeSceneToFile("res://scenes/map/world_man.tscn");
		};
		freePlayButton.Pressed += () => {
			GetTree().ChangeSceneToFile("res://scenes/map/world_man.tscn");
		};
		exitButton.Pressed += () => {
			GetTree().Quit();
		};

	}

}
