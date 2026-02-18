using System.Collections.Generic;
using Godot;
using resources.game;
using scenes.autoload;

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
					GameMan.Singleton.Game.Time.TimePassedEvent -= TimeUpdate;
					break;
				case (int)NotificationReady:
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
		}

		void TimeUpdate(TimeT time) {
			foreach (var (tpos, mopview) in mapObjectViews) {
				if (region.LocalFaction.GetJob(tpos + region.WorldPosition, out var job)) {
					float progress = job.GetProgressEstimate();
					mopview.DisplayJobProgress(progress, show: job.Workers != 0 || progress > 0f, showBuilding: job is ConstructBuildingJob);
				} else {
					mopview.DisplayJobProgress(0f, false);
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
			region.MapObjectUpdatedAtEvent += OnRegionMapObjectUpdated;
			region.LocalFaction.JobAddedEvent += OnRegionJobAdded;
			region.LocalFaction.JobRemovedEvent += OnRegionJobRemoved;
			region.TileChangedAtEvent += OnTileChanged;
			region.LocalFaction.JobChangedEvent += OnJobChanged;
		}

		void DisconnectEvents() {
			region.MapObjectUpdatedAtEvent -= OnRegionMapObjectUpdated;
			region.LocalFaction.JobAddedEvent -= OnRegionJobAdded;
			region.LocalFaction.JobRemovedEvent -= OnRegionJobRemoved;
			region.TileChangedAtEvent -= OnTileChanged;
			region.LocalFaction.JobChangedEvent -= OnJobChanged;
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
			//GD.Print($"RegionDisplay::OnJobChanged : {job} changed");
			jobsToDisplay.Enqueue(job);
		}

		void DisplayRegionJob(Job job) {
			if (job is MapObjectJob mopjob) {
				var pos = mopjob.GlobalPosition - region.WorldPosition;
				if (!mapObjectViews.TryGetValue(pos, out MapObjectView view)) return; // assume this was deleted sometime
				Debug.Assert(mapObjectViews.ContainsKey(pos), $"Don't have the building view that the {job} is being attached to (at {pos} global {mopjob.GlobalPosition})");
				MapObjectView.IconSetIcons icon = MapObjectView.IconSetIcons.Building;
				if (job is GatherResourceJob) icon = MapObjectView.IconSetIcons.Gathering;
				view.IconSetShow(icon);
				if (job.Workers != 0) view.IconSetShow(MapObjectView.IconSetIcons.Workers);
				else view.IconSetHide(MapObjectView.IconSetIcons.Workers);
			}
			//GD.Print("RegionDisplay::DisplayRegionJob : job displayed ", job);
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
				if (!region.LocalFaction.GetJob(mopjob.GlobalPosition - region.WorldPosition, out var __)) {
					view.IconSetHide();
				}
				if (region.LocalFaction.HasBuilding(mopjob.GlobalPosition - region.WorldPosition)) {
				}
			}
		}

		void OnTileChanged(Vector2I at) {
			tilemaps.DisplayGround(region);
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
			return GetMouseHoveredTilePos(tilemaps.Ground.GetLocalMousePosition());
		}

		public Vector2I GetMouseHoveredTilePos(Vector2 localMousePos) {
			var tilepos = LocalToTile(localMousePos);
			localMousePos.Y += Tilemaps.TileElevationVerticalOffset(region.WorldPosition + tilepos, GameMan.Singleton.Game.Map.World);
			tilepos = LocalToTile(localMousePos);
			return tilepos;
		}


	}

}
