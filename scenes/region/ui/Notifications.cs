using System;
using Godot;

namespace scenes.region.ui;

public partial class Notifications : VBoxContainer {

	[Export] PackedScene notificationPacked;
    [Export] Container notificationContainer;


    public void Notify(string text, Action callback = null) {
        var notif = notificationPacked.Instantiate<Notification>();
        notif.SetText(text);
        notif.SetCallback(callback);
        notificationContainer.AddChild(notif);
        notificationContainer.MoveChild(notif, 0);
    }

}
