using System;
using Godot;

namespace scenes.region.ui;

public partial class Notification : Control {

	[Export] RichTextLabel label;
	[Export] Button dismissButton;
	[Export] ProgressBar timeProgress;
	[Export] TextureRect gradientDisplay;
	[Export] AnimationPlayer pulser;

	public float TimeLimit { get; private set; }
	public float Time { get; private set; }

	public override void _Ready() {
		dismissButton.Pressed += () => { if (!IsDismissing) Dismiss(); };
	}

	public Notification SetText(string text) {
		label.Text = text;
		return this;
	}

	public Notification SetCallback(Action callback) {
		if (callback != null) dismissButton.Pressed += callback;
		return this;
	}

	public Notification SetTimeLimit(float timeLimit) {
		TimeLimit = timeLimit;
		timeProgress.MaxValue = TimeLimit;
		timeProgress.Value = TimeLimit;
		return this;
	}

	public void SetDismissable(bool isDismissable) {
		dismissButton.Visible = isDismissable;
	}

	public Notification SetGradient(Color from, Color to) {
		var texture = gradientDisplay.Texture.DuplicateDeep(Resource.DeepDuplicateMode.Internal) as GradientTexture2D;
		texture.Gradient.SetColor(0, from);
		texture.Gradient.SetColor(1, to);
		gradientDisplay.Texture = texture;
		return this;
	}

	public Notification SetPulsing(bool to)  {
		if (to) pulser.Play("pulse");
		return this;
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
