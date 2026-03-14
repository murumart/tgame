using System;
using Godot;
using scenes.map.ui;

namespace scenes.ui;

public partial class MainMenu : Control {

	[Export] Button playButton;
	[Export] Button exitButton;

	[Export] WorldGenUi worldgen;


	public override void _Ready() {
		Debug.Assert(worldgen != null);
		Debug.Assert(playButton != null);
		Debug.Assert(exitButton != null);

		playButton.Pressed += OnPlayPressed;
		worldgen.GoBackEvent += OnPlayBack;
		exitButton.Pressed += () => {
			GetTree().Quit();
		};

	}

	void OnPlayPressed() {
		worldgen.Show();
	}

	void OnPlayBack() {
		worldgen.Hide();
	}

}
