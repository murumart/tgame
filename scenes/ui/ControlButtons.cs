using System;
using Godot;
using scenes.autoload;

public partial class ControlButtons : HBoxContainer {

	[Export] Button panButton;

	[Export] Label zoomLabel;
	[Export] Button zoomInButton;
	[Export] Button zoomResetButton;
	[Export] Button zoomOutButton;

	[Export] public Label gameSpeedLabel;
	[Export] public Button pauseButton;
	[Export] public Button normalSpeedButton;
	[Export] public Button fastSpeedButton;
	[Export] public Button fasterSpeedButton;

	[Export] public Camera Camera;

	bool timeSpeedAlteringDisabled = false;
	public bool TimeSpeedAlteringDisabled => timeSpeedAlteringDisabled;
	bool cursedPanning;


	public override void _Ready() {
		if (pauseButton is not null) pauseButton.Pressed += OnPauseButtonPressed;
		if (normalSpeedButton is not null) normalSpeedButton.Pressed += OnNormalSpeedButtonPressed;
		if (fastSpeedButton is not null) fastSpeedButton.Pressed += OnFastSpeedButtonPressed;
		if (fasterSpeedButton is not null) fasterSpeedButton.Pressed += OnFasterSpeedButtonPressed;

		zoomInButton.Pressed += () => Camera.ZoomIn();
		zoomOutButton.Pressed += () => Camera.ZoomOut();
		zoomResetButton.Pressed += () => Camera.ZoomReset();

		panButton.ButtonDown += () => {
			Camera.StartDragging(true);
			cursedPanning = true;
			panButton.MouseFilter = MouseFilterEnum.Ignore;
			panButton.Disabled = true;
		};
		//panButton.ButtonUp += () => Camera.StopDragging();
	}

	public override void _Input(InputEvent @event) {
		base._Input(@event);
		if (@event is InputEventMouseButton iemb && iemb.ButtonIndex == MouseButton.Left && iemb.IsReleased() && cursedPanning) {
			cursedPanning = false;
			Camera.StopDragging();
			panButton.Disabled = false;
			panButton.MouseFilter = MouseFilterEnum.Stop;
		}
	}

	void TogglePause() {
		if (timeSpeedAlteringDisabled) return;
		GameMan.TogglePause();
		UpdateDisplays();
	}

	void OnPauseButtonPressed() {
		TogglePause();
	}

	const int NORMAL_SPEED = 1;
	const int FAST_SPEED = 10;
	const int FASTER_SPEED = 30;

	void OnNormalSpeedButtonPressed() {
		if (timeSpeedAlteringDisabled) return;
		if (GameMan.IsPaused) TogglePause();
		GameMan.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, NORMAL_SPEED);
		UpdateDisplays();
	}

	void OnFastSpeedButtonPressed() {
		if (timeSpeedAlteringDisabled) return;
		if (GameMan.IsPaused) TogglePause();
		GameMan.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, FAST_SPEED);
		UpdateDisplays();
	}

	void OnFasterSpeedButtonPressed() {
		if (timeSpeedAlteringDisabled) return;
		if (GameMan.IsPaused) TogglePause();
		GameMan.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, FASTER_SPEED);
		UpdateDisplays();
	}

	public void SetTimeSpeedAlteringAllowed(bool to) {
		timeSpeedAlteringDisabled = !to;
		pauseButton.Disabled = !to;
		fastSpeedButton.Disabled = !to;
		fasterSpeedButton.Disabled = !to;
		normalSpeedButton.Disabled = !to;
	}

	public void UpdateDisplays() {
		zoomLabel.Text = $"zoom: {(Camera.Zoom.X):F1}";
		gameSpeedLabel.Text = GameMan.IsPaused ? "paused" : $"{GameMan.GameSpeed}x game speed";
	}
}
