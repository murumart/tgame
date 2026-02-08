using System.Linq;
using Godot;
using static MapObject;

namespace scenes.region {

	[GlobalClass]
	public partial class MapObjectView : Node2D {

		public enum IconSetIcons : int {
			Hammer,
			Max
		}

		[Export] protected Node2D iconTransformParent;
		[Export] protected Container iconContainer;
		protected TextureRect Icon(int i) => (TextureRect)iconContainer.GetChild(i);

		[Export] protected ProgressBar buildingProgressBar;
		[Export] protected Node2D inProgressDisplay;

		protected MapObject mapObjectRef;


		public override void _Ready() {
			//RemoveChild(iconTransformParent);
			//UILayer.AddUiChild(iconTransformParent);
			Debug.Assert(mapObjectRef != null, "MapObjectView needs a map object ref");
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

		public static MapObjectView Make(string scenePath, MapObject mapObject) {
			Debug.Assert(ResourceLoader.Exists(scenePath, "PackedScene"), $"PackedScene {scenePath} doesn't exist");
			var scn = GD.Load<PackedScene>(scenePath).Instantiate<MapObjectView>();
			scn.mapObjectRef = mapObject;
			return scn;
		}

		public static MapObjectView Make(string scenePath, IMapObjectType mapObject) {
			Debug.Assert(ResourceLoader.Exists(scenePath, "PackedScene"), $"PackedScene {scenePath} doesn't exist");
			var scn = GD.Load<PackedScene>(scenePath).Instantiate();
			Debug.Assert(scn is MapObjectView, "Scene must be a MapObjectView");
			(scn as MapObjectView).mapObjectRef = mapObject.CreateMapObject(Vector2I.Zero);
			return (scn as MapObjectView);
		}

	}

}