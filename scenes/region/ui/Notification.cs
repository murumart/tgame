using System;
using Godot;

namespace scenes.region.ui;

public partial class Notification : Control {

	[Export] Label label;
	[Export] Button dismissButton;
	[Export] ProgressBar timeProgress;

	public float TimeLimit { get; private set; }
	public float Time { get; private set; }

	public override void _Ready() {
		dismissButton.Pressed += () => { if (!IsDismissing) Dismiss(); };
	}

	public void SetText(string text) {
		label.Text = text;
	}

	public void SetCallback(Action callback) {
		if (callback != null) dismissButton.Pressed += callback;
	}

	public void SetTimeLimit(float timeLimit) {
		TimeLimit = timeLimit;
		timeProgress.MaxValue = TimeLimit;
		timeProgress.Value = TimeLimit;
	}

	public void IncreaseTime(float delta) {
		Time += delta;
		timeProgress.Value = TimeLimit - Time;
	}

	public bool IsDismissing { get; private set; }

	public void Dismiss() {
		Debug.Assert(!IsDismissing);
		IsDismissing = true;
		var tw = CreateTween().SetTrans(Tween.TransitionType.Cubic);
		tw.TweenProperty(this, "modulate:a", 0f, 0.2f);
		tw.TweenCallback(Callable.From(QueueFree));
	}

}