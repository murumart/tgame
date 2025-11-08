using System;
using System.Linq;
using Godot;
using scenes.autoload;

namespace scenes.region {

	[GlobalClass]
	public partial class MapObjectView : Node2D {

		public enum IconSetIcons: int {
			HAMMER,
			MAX,
		}

		[Export] Node2D iconTransformParent;
		[Export] Container iconContainer;
		TextureRect Icon(int i) => (TextureRect)iconContainer.GetChild(i);


		public override void _Ready() {
			RemoveChild(iconTransformParent);
			UILayer.AddUiChild(iconTransformParent);
			IconSetHide();
		}

		public override void _Process(double delta) {
			if (!Visible) return;
			if (!iconContainer.Visible) return;
			var tf = GetGlobalTransformWithCanvas();
			iconTransformParent.Position = tf.Origin;
		}

		public override void _Notification(int what) {
			if (what == NotificationPredelete) {
				iconTransformParent.QueueFree();
			}
		}

		public void IconSetShow(IconSetIcons icons) {
			Icon((int)icons).Show();
			iconContainer.Show();
		}

		public void IconSetHide(IconSetIcons icons) {
			Icon((int)icons).Hide();
			if (iconContainer.GetChildren().Cast<TextureRect>().All((t) => !t.Visible)) {
				iconContainer.Hide();
			}
		}

		public void IconSetHide() {
			iconContainer.Hide();
		}

	}

}