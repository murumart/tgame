using System.Linq;
using Godot;
using static MapObject;

namespace scenes.region;

[GlobalClass]
public partial class MapObjectView : Node2D {

	public enum IconSetIcons : int {
		Building,
		Gathering,
		Workers,
		Max
	}

	[Export] protected IconTransform iconTransformParent;
	[Export] protected Container iconContainer;
	protected TextureRect Icon(int i) => (TextureRect)iconContainer.GetChild(i);

	[Export] protected ProgressBar jobProgressBar;
	[Export] protected Node2D inProgressDisplay;
	[Export] protected Sprite2D selectedHighlight;

	[Export] protected Node2D mapObjectDisplay;
	[Export] public bool IsProblem;

	protected MapObject mapObjectRef;


	public override void _Ready() {
		if (!IsProblem) {
			Debug.Assert(mapObjectRef != null, "MapObjectView needs a map object ref");
			Debug.Assert(mapObjectDisplay != null, "MapObjectView needs a reference to the display object");
		}
		IconSetHide();
	}

	public void ViewTransformUpdated() {
		iconTransformParent.UpdateViewTransform();
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
		foreach (var c in iconContainer.GetChildren().Cast<TextureRect>()) c.Hide();
	}

	public void DisplayJobProgress(float progress, bool show = true, bool showBuildingTape = false) {
		Debug.Assert(progress >= 0f && progress <= 1f, "Progress bar value aout of range");
		jobProgressBar.Visible = show;
		jobProgressBar.Value = progress;
		inProgressDisplay.Visible = showBuildingTape;
		mapObjectDisplay.Modulate = showBuildingTape ? new(mapObjectDisplay.Modulate, 0.5f) : new(mapObjectDisplay.Modulate, 1f);
	}

	public static MapObjectView Make(string scenePath, MapObject mapObject) {
		Debug.Assert(ResourceLoader.Exists(scenePath, "PackedScene"), $"PackedScene {scenePath} doesn't exist");
		var scn = GD.Load<PackedScene>(scenePath).Instantiate<MapObjectView>();
		scn.mapObjectRef = mapObject;
		return scn;
	}

	public static MapObjectView MakeDisplay(string scenePath, IMapObjectType mapObject) {
		Debug.Assert(ResourceLoader.Exists(scenePath, "PackedScene"), $"PackedScene {scenePath} doesn't exist");
		var scn = GD.Load<PackedScene>(scenePath).Instantiate();
		Debug.Assert(scn is MapObjectView, "Scene must be a MapObjectView");
		(scn as MapObjectView).mapObjectRef = mapObject.CreateMapObject(Vector2I.Zero);
		return (scn as MapObjectView);
	}

	public static MapObjectView MakeProblem() {
		var view = GD.Load<PackedScene>("res://scenes/region/problem_view.tscn").Instantiate<MapObjectView>();
		Debug.Assert(view is not null, "Scene must not be null");
		return view;
	}

	public void OnSelected() {
		selectedHighlight.Show();
	}

	public void OnDeselected() {
		selectedHighlight.Hide();
	}

}

