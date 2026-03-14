using System;
using Godot;
using scenes.autoload;

namespace scenes.region.ui;

public partial class Notifications : VBoxContainer {

	[Export] PackedScene notificationPacked;
	[Export] Container notificationContainer;


	public override void _Process(double delta) {
		foreach (Node child in notificationContainer.GetChildren()) {
			if (child is Notification notif && notif.TimeLimit > 0f && !GameMan.Singleton.IsPaused) {
				notif.IncreaseTime((float)delta);
				if (notif.Time >= notif.TimeLimit && !notif.IsDismissing) {
					notif.Dismiss();
				}
			}
		}
	}

	public Notification Notify(
		string text,
		Action callback = null,
        float timeLimit = 0f,
        bool isDismissable = true,
        (Color, Color)? gradientColors = null,
        bool isPulsing = false
	) {
		var notif = notificationPacked.Instantiate<Notification>();
		notif
            .SetText(text)
            .SetCallback(callback)
		    .SetDismissable(isDismissable);
		if (timeLimit > 0f) notif.SetTimeLimit(timeLimit);
		notificationContainer.AddChild(notif);
		notificationContainer.MoveChild(notif, 0);
        if (gradientColors is (Color, Color) cols) notif.SetGradient(cols.Item1, cols.Item2);
        notif.SetPulsing(isPulsing);

		var tw = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tw.TweenProperty(notif, "modulate:a", 1.0f, 0.3f).From(0.0f);
		//tw.SetTrans(Tween.TransitionType.Elastic).TweenProperty(notif, "position:y", notif.Position.Y, 0.0f).From(notificationContainer.Size.Y);

		return notif;
	}

}
