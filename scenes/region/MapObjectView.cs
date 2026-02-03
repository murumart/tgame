using System;
using System.Linq;
using Godot;
using scenes.autoload;

namespace scenes.region {

	[GlobalClass]
	public partial class MapObjectView : Node2D {

		public enum IconSetIcons: int {
			Hammer,
			Max
		}

		[Export] Node2D iconTransformParent;
		[Export] Container iconContainer;
		TextureRect Icon(int i) => (TextureRect)iconContainer.GetChild(i);

		[Export] ProgressBar buildingProgressBar;
		[Export] Node2D inProgressDisplay;


		public override void _Ready() {
			//RemoveChild(iconTransformParent);
			//UILayer.AddUiChild(iconTransformParent);
			IconSetHide();
		}

		public override void _Process(double delta) {
			if (!Visible) return;
			if (!iconContainer.Visible) return;
			var tf = GetGlobalTransformWithCanvas();
			iconTransformParent.Scale = tf.Scale.Inverse();
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

		public void DisplayBuildingProgress(float progress, bool show = true) {
			Debug.Assert(progress >= 0f && progress <= 1f, "Progress bar value aout of range");
			buildingProgressBar.Visible = show;
			buildingProgressBar.Value = progress;
			inProgressDisplay.Visible = show;
		}

	}

}