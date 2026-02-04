using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.game;

namespace scenes.region {

	public partial class RegionDisplay : Node2D {

		[Export] Tilemaps tilemaps;
		[Export] Node2D buildingsParent;

		readonly Dictionary<Vector2I, MapObjectView> mapObjectViews = new();
		Region region;

		bool valid = false;

		readonly Queue<Job> jobsToDisplay = new();
		readonly Queue<Job> jobsToUndisplay = new();


		// setup

		public static RegionDisplay Instantiate() => (RegionDisplay)GD.Load<PackedScene>("uid://cpeaxmkgldt5g").Instantiate();

		public override void _Notification(int notif) {
			switch (notif) {
				case (int)NotificationPredelete:
					DisconnectEvents();
					valid = false;
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
			foreach (var (tpos, mopview) in mapObjectViews) {
				if (region.GetMapObject(tpos) is Building building) {
					if (building.IsConstructed) mopview.DisplayBuildingProgress(1.0f, false);
					else mopview.DisplayBuildingProgress(building.GetBuildProgress());
				}
			}
		}

		public void LoadRegion(Region region) {
			if (valid) {
				DisconnectEvents();
				valid = false;
			}

			this.region = region;
			ConnectEvents();
			tilemaps.DisplayGround(region);

			foreach (var m in region.GetMapObjects()) {
				DisplayMapObject(m);
			}
			valid = true;
		}

		void ConnectEvents() {
			region.MapObjectUpdatedAtEvent += OnRegionMapObjectUpdated;
			region.LocalFaction.JobAddedEvent += OnRegionJobAdded;
			region.LocalFaction.JobRemovedEvent += OnRegionJobRemoved;
			region.TileChangedAtEvent += OnTileChanged;
		}

		void DisconnectEvents() {
			region.MapObjectUpdatedAtEvent -= OnRegionMapObjectUpdated;
			region.LocalFaction.JobAddedEvent -= OnRegionJobAdded;
			region.LocalFaction.JobRemovedEvent -= OnRegionJobRemoved;
			region.TileChangedAtEvent -= OnTileChanged;
		}

		MapObjectView LoadMapObjectView(MapObject mo) {
			MapObjectView view = mo switch {
				Building => GD.Load<PackedScene>(DataStorage.GetScenePath(((Building)mo).Type)).Instantiate<MapObjectView>(),
				ResourceSite => GD.Load<PackedScene>(DataStorage.GetScenePath(((ResourceSite)mo).Type)).Instantiate<MapObjectView>(),
				_ => throw new System.Exception("nooooo wrong wrong wrong wrong its all wrong"),
			};
			return view;
		}

		public void DisplayMapObject(MapObject mopbject) {
			var view = LoadMapObjectView(mopbject);
			buildingsParent.AddChild(view);
			mapObjectViews[mopbject.GlobalPosition - region.WorldPosition] = view;
			view.Position = Tilemaps.TilePosToWorldPos(mopbject.GlobalPosition - region.WorldPosition);
		}

		void RemoveDisplay(Vector2I tile) {
			mapObjectViews[tile].QueueFree();
			mapObjectViews.Remove(tile);
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

		void DisplayRegionJob(Job job) {
			if (job is MapObjectJob mopjob) {
				Debug.Assert(mapObjectViews.ContainsKey(mopjob.GlobalPosition - region.WorldPosition), $"Don't have the building view that the {job} is being attached to");
				mapObjectViews[mopjob.GlobalPosition - region.WorldPosition].IconSetShow(MapObjectView.IconSetIcons.Hammer);
			}
			GD.Print("RegionDisplay::OnRegionJobAdded : job added ", job);
		}

		void OnRegionJobRemoved(Job job) {
			jobsToUndisplay.Enqueue(job);
		}

		void UndisplayRegionJob(Job job) {
			if (job is MapObjectJob mopjob) {
				if (!mopjob.IsValid) {
					GD.Print("RegionDisplay::UndisplayRegionJob : ignoring job undisplay, map object isn't valid, assuming it was deleted");
					return;
				}
				if (!mapObjectViews.TryGetValue(mopjob.GlobalPosition - region.WorldPosition, out MapObjectView view)) return; // the building view has already been removed
				if (!region.LocalFaction.GetJob(mopjob.GlobalPosition - region.WorldPosition, out var __)) view.IconSetHide(MapObjectView.IconSetIcons.Hammer);
				if (region.LocalFaction.HasBuilding(mopjob.GlobalPosition - region.WorldPosition)) {
				}
			}
		}

		void OnTileChanged(Vector2I at) {
			tilemaps.DisplayGround(region);
		}

		// misc..

		public Vector2I LocalToTile(Vector2 lpos) => tilemaps.Ground.LocalToMap(lpos);

		public Vector2I GetMouseHoveredTilePos() {
			var localMousePos = tilemaps.Ground.GetLocalMousePosition();
			return LocalToTile(localMousePos);
		}

	}

}
