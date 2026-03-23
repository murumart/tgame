using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.game;
using scenes.autoload;

namespace scenes.region;

public partial class RegionDisplay : Node2D {

	[Export] Tilemaps tilemaps;
	[Export] Node2D buildingsParent;
	[Export] VisibleOnScreenNotifier2D visibilityArea;
	[Export] ColorRect visibilityDebug;

	readonly Dictionary<Vector2I, MapObjectView> mapObjectViews = new();
	readonly Dictionary<Vector2I, MapObjectView> problemViews = new();
	Region region;
	Camera camera;

	bool valid = false;

	readonly Queue<Job> jobsToDisplay = new();
	readonly Queue<Job> jobsToUndisplay = new();
	readonly Queue<Problem> problemsToDisplay = new();
	readonly Queue<Problem> problemsToUndisplay = new();

	public int Lod { get; private set; }
	public Region Region { get => region; }


	// setup

	public static RegionDisplay Instantiate() => (RegionDisplay)GD.Load<PackedScene>("uid://cpeaxmkgldt5g").Instantiate();

	public override void _Notification(int notif) {
		switch (notif) {
			case (int)NotificationPredelete:
				DisconnectEvents();
				valid = false;
				GameMan.Singleton.Game.Time.TimePassedEvent -= TimeUpdate;
				break;
			case (int)NotificationReady:
				visibilityArea.ScreenEntered += OnScreenEntered;
				visibilityArea.ScreenExited += OnScreenExited;
				GameMan.Singleton.Game.Time.TimePassedEvent += TimeUpdate;
				break;
		}
	}

	public override void _Process(double delta) {
		if (jobsToDisplay.Count > 0) {
			var j = jobsToDisplay.Dequeue();
			DisplayRegionJob(j);
		}
		if (jobsToUndisplay.Count > 0) {
			var j = jobsToUndisplay.Dequeue();
			UndisplayRegionJob(j);
		}
		if (problemsToDisplay.Count > 0) {
			var p = problemsToDisplay.Dequeue();
			DisplayRegionProblem(p);
		}
		if (problemsToUndisplay.Count > 0) {
			var p = problemsToUndisplay.Dequeue();
			UndisplayRegionProblem(p);
		}
		if (GameMan.Singleton.IsPaused && Engine.GetFramesDrawn() % 16 == 0) {
			DisplayJobProgress();
		}
	}

	void TimeUpdate(TimeT time) {
		DisplayJobProgress();
	}

	public void LoadRegion(Region region, int lod, RegionCamera camera) {
		if (valid) {
			DisconnectEvents();
			valid = false;
		}
		Lod = lod;

		this.region = region;
		this.camera = camera;
		ConnectEvents();
		tilemaps.DisplayGround(region);
		var wposes = region.GroundTiles.Keys.Select(p => Tilemaps.TilePosToWorldPos(p) + Vector2.Up * Tilemaps.TileElevationVerticalOffset(region.WorldPosition + p, GameMan.Singleton.Game.Map.World));
		float minx = wposes.MinBy(p => p.X).X - Tilemaps.TILE_SIZE.X * 0.5f;
		float miny = wposes.MinBy(p => p.Y).Y - Tilemaps.TILE_SIZE.Y * 0.5f;
		float maxx = wposes.MaxBy(p => p.X).X - Tilemaps.TILE_SIZE.X * 0.5f;
		float maxy = wposes.MaxBy(p => p.Y).Y - Tilemaps.TILE_SIZE.Y * 0.5f;

		var visiblerect = new Rect2(minx, miny, maxx - minx, maxy - miny);
		visibilityArea.Rect = visiblerect;
		//visibilityDebug.Position = visiblerect.Position;
		//visibilityDebug.Size = visiblerect.Size;
		//visibilityDebug.Visible = region == GameMan.Singleton.Game.PlayRegion;

		if (Lod < 2) {
			foreach (var m in region.GetMapObjects()) {
				DisplayMapObject(m);
			}
		}
		valid = true;
		OnScreenExited();

		// debug
		//if (region == GameMan.Singleton.Game.PlayRegion) {
		//	UILayer.DebugDisplay(() => {
		//		return ""
		//			+ $"mouse: {tilemaps.Ground.GetLocalMousePosition()}\n"
		//			+ $"to tile: {LocalToTile(tilemaps.Ground.GetLocalMousePosition())}\n"
		//			+ $"final: {this.GetMouseHoveredTilePos()}\n"
		//		;
		//	});
		//}
	}

	void ConnectEvents() {
		if (Lod < 2) {
			region.MapObjectUpdatedAtEvent += OnRegionMapObjectUpdated;
			region.LocalFaction.JobAddedEvent += OnRegionJobAdded;
			region.LocalFaction.JobRemovedEvent += OnRegionJobRemoved;
			region.LocalFaction.JobChangedEvent += OnJobChanged;

			region.LocalFaction.ProblemAddedEvent += OnRegionProblemAdded;
			region.LocalFaction.ProblemUnsolvedEvent += OnRegionProblemEnded;
			region.LocalFaction.ProblemSolvedEvent += OnRegionProblemEnded;
			camera.ZoomChanged += OnZoomChanged;
		}
		region.TileChangedAtEvent += OnTileChanged;
	}

	void DisconnectEvents() {
		if (Lod < 2) {
			region.MapObjectUpdatedAtEvent -= OnRegionMapObjectUpdated;
			region.LocalFaction.JobAddedEvent -= OnRegionJobAdded;
			region.LocalFaction.JobRemovedEvent -= OnRegionJobRemoved;
			region.LocalFaction.JobChangedEvent -= OnJobChanged;

			region.LocalFaction.ProblemAddedEvent -= OnRegionProblemAdded;
			region.LocalFaction.ProblemUnsolvedEvent -= OnRegionProblemEnded;
			region.LocalFaction.ProblemSolvedEvent -= OnRegionProblemEnded;
			camera.ZoomChanged -= OnZoomChanged;
		}
		region.TileChangedAtEvent -= OnTileChanged;
	}

	void OnZoomChanged() {
		foreach (var mopv in mapObjectViews.Values) {
			mopv.ViewTransformUpdated();
		}
		foreach (var mopv in problemViews.Values) {
			mopv.ViewTransformUpdated();
		}
	}

	MapObjectView LoadMapObjectView(MapObject mo) {
		MapObjectView view = mo switch {
			Building => MapObjectView.Make(DataStorage.GetScenePath(((Building)mo).Type), mo),
			ResourceSite => MapObjectView.Make(DataStorage.GetScenePath(((ResourceSite)mo).Type), mo),
			_ => null,
		};
		Debug.Assert(view != null, "Unimplemented map object view load");
		return view;
	}

	public void DisplayMapObject(MapObject mopbject) {
		var view = LoadMapObjectView(mopbject);
		buildingsParent.AddChild(view);
		mapObjectViews[mopbject.GlobalPosition - region.WorldPosition] = view;
		var viewpos = Tilemaps.TilePosToWorldPos(mopbject.GlobalPosition - region.WorldPosition);
		viewpos.Y -= Tilemaps.TileElevationVerticalOffset(mopbject.GlobalPosition, GameMan.Singleton.Game.Map.World);
		view.Position = viewpos;
		if (region.LocalFaction.GetJob(mopbject.GlobalPosition, out var job)) {
			OnJobChanged(job, 0);
		}
	}

	void RemoveDisplay(Vector2I tile) {
		mapObjectViews[tile].QueueFree();
		mapObjectViews.Remove(tile);
	}

	void DisplayJobProgress() {
		foreach (var (tpos, mopview) in mapObjectViews) {
			if (region.LocalFaction.HasProblem(tpos)) {
				var problem = region.LocalFaction.GetProblem(tpos);
				float progress = 1f - problem.GetProgress();
				mopview.DisplayJobProgress(progress, show: true);
			} else if (region.LocalFaction.GetJob(tpos + region.WorldPosition, out var job)) {
				float progress = job.GetProgressEstimate();
				mopview.DisplayJobProgress(progress, show: job.Workers != 0 || progress > 0f, showBuildingTape: job is ConstructBuildingJob);
			} else {
				mopview.DisplayJobProgress(0f, false);
			}
		}
	}

	// reactions to notifications

	void OnRegionMapObjectUpdated(Vector2I tile) {
		bool exists = region.HasMapObject(tile);
		if (!exists) {
			RemoveDisplay(tile);
		} else if (!mapObjectViews.ContainsKey(tile)) {
			DisplayMapObject(region.GetMapObject(tile));
		}
	}

	void OnRegionJobAdded(Job job) {
		jobsToDisplay.Enqueue(job);
	}

	void OnJobChanged(Job job, int __) {
		jobsToDisplay.Enqueue(job);
	}

	void DisplayRegionJob(Job job) {
		if (job is not MapObjectJob mopjob) return;
		var pos = mopjob.GlobalPosition - region.WorldPosition;
		if (!mapObjectViews.TryGetValue(pos, out MapObjectView view)) return; // assume this was deleted sometime
		MapObjectView.IconSetIcons icon = MapObjectView.IconSetIcons.Building;
		if (job is GatherResourceJob) icon = MapObjectView.IconSetIcons.Gathering;
		view.IconSetShow(icon);
		if (job.Workers != 0) view.IconSetShow(MapObjectView.IconSetIcons.Workers);
		else view.IconSetHide(MapObjectView.IconSetIcons.Workers);
	}

	void UpdateJobDisplayAt(Vector2I viewpos) {
		bool has = mapObjectViews.TryGetValue(viewpos, out MapObjectView view);
		Debug.Assert(has, "Don't have job display to update wher eat =");
		has = region.LocalFaction.GetJob(region.WorldPosition + viewpos, out var job);
		if (!has) return;
		DisplayRegionJob(job);
	}

	void OnRegionJobRemoved(Job job) {
		jobsToUndisplay.Enqueue(job);
	}

	void UndisplayRegionJob(Job job) {
		if (job is not MapObjectJob mopjob) return;
		if (!mopjob.IsValid) return;

		if (!mapObjectViews.TryGetValue(mopjob.GlobalPosition - region.WorldPosition, out MapObjectView view)) return; // the building view has already been removed
		if (!region.LocalFaction.GetJob(mopjob.GlobalPosition - region.WorldPosition, out var _)) {
			view.IconSetHide();
		}
	}

	void OnRegionProblemAdded(Problem proble) {
		Debug.Assert(!problemsToDisplay.Contains(proble), $"This problem {proble} is already queueed for displayu in regiondisplay");
		problemsToDisplay.Enqueue(proble);
	}

	void OnRegionProblemEnded(Problem proble) {
		Debug.Assert(!problemsToUndisplay.Contains(proble), $"This problem {proble} is already queueed for displayu in regiondisplay");
		problemsToUndisplay.Enqueue(proble);
	}

	void DisplayRegionProblem(Problem proble) {
		var view = MapObjectView.MakeProblem();
		buildingsParent.AddChild(view);
		problemViews[proble.LocalPosition] = view;
		var viewpos = Tilemaps.TilePosToWorldPos(proble.LocalPosition);
		viewpos.Y -= Tilemaps.TileElevationVerticalOffset(proble.LocalPosition + region.WorldPosition, GameMan.Singleton.Game.Map.World);
		view.Position = viewpos;
		bool hasBview = mapObjectViews.TryGetValue(proble.LocalPosition, out var moview);
		if (hasBview) {
			moview.IconSetHide();
		}
	}

	void UndisplayRegionProblem(Problem proble) {
		var yes = problemViews.TryGetValue(proble.LocalPosition, out var view);
		Debug.Assert(yes, "No problemview exists to undisplay");
		problemViews.Remove(proble.LocalPosition);
		view.QueueFree();
		bool hasBview = mapObjectViews.TryGetValue(proble.LocalPosition, out var moview);
		if (hasBview) {
			UpdateJobDisplayAt(proble.LocalPosition);
		}
	}

	void OnTileChanged(Vector2I at) {
		tilemaps.DisplayGround(region);
	}

	void OnScreenEntered() {
		tilemaps.Show();
		SetProcess(true);
	}

	void OnScreenExited() {
		tilemaps.Hide();
		SetProcess(false);
	}

	Vector2I? selectedTile;
	public void OnTileSelected(Vector2I tile) {
		if (mapObjectViews.TryGetValue(tile, out var obj)) {
			obj.OnSelected();
		}
		selectedTile = tile;
	}

	public void OnTileDeselected() {
		if (selectedTile != null && mapObjectViews.TryGetValue(selectedTile ?? Vector2I.One, out var obj)) {
			obj.OnDeselected();
		}
		selectedTile = null;
	}

	// misc..

	public Vector2I LocalToTile(Vector2 lpos) {
		//return tilemaps.Ground.LocalToMap(lpos);
		lpos -= (Vector2)Tilemaps.TILE_SIZE * new Vector2(0.5f, 0.25f);
		lpos += new Vector2(0, 8);
		int x = Mathf.FloorToInt(lpos.X / Tilemaps.TILE_SIZE.X + lpos.Y / Tilemaps.TILE_SIZE.Y);
		int y = Mathf.FloorToInt(-lpos.X / Tilemaps.TILE_SIZE.X + lpos.Y / Tilemaps.TILE_SIZE.Y);
		return new(x, y);
	}

	public Vector2I GetMouseHoveredTilePos() {
		return GetTilePosFromLocalPos(tilemaps.Ground.GetLocalMousePosition());
	}

	public Vector2I GetTilePosFromLocalPos(Vector2 localPos) {
		var tilepos = LocalToTile(localPos);
		Debug.Assert(region is not null, "Region shouldnät be null");
		Debug.Assert(GameMan.Singleton.Game?.Map?.World is not null, "World shouldnät be null");
		localPos.Y += Tilemaps.TileElevationVerticalOffset(region.WorldPosition + tilepos, GameMan.Singleton.Game.Map.World);
		tilepos = LocalToTile(localPos);
		return tilepos;
	}


}
