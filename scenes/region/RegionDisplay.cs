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
		}

		void DisconnectEvents() {
			region.MapObjectUpdatedAtEvent -= OnRegionMapObjectUpdated;
			region.LocalFaction.JobAddedEvent -= OnRegionJobAdded;
			region.LocalFaction.JobRemovedEvent -= OnRegionJobRemoved;
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
			mapObjectViews[mopbject.Position] = view;
			view.Position = Tilemaps.TilePosToWorldPos(mopbject.Position);
			view.Modulate = new Color(1f, 1f, 1f);
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
			}
		}

		void OnRegionJobAdded(Job job) {
			GD.Print("RegionDisplay::OnRegionJobAdded : job added ", job);
			if (job is MapObjectJob mopjob) {
				if (!mapObjectViews.ContainsKey(mopjob.Position) && mopjob is ConstructBuildingJob or AbsorbFromHomelessPopulationJob) {
					Callable.From(() => OnRegionJobAdded(mopjob)).CallDeferred();
					return;
				}
				Debug.Assert(mapObjectViews.ContainsKey(mopjob.Position), $"Don't have the building object that the {job} is being attached to");
				mapObjectViews[mopjob.Position].IconSetShow(MapObjectView.IconSetIcons.HAMMER);
			}
		}

		void OnRegionJobRemoved(Job job) {

			if (job is MapObjectJob mopjob) {
				if (!(region.LocalFaction.GetJobs(mopjob.Position).Any())) mapObjectViews[mopjob.Position].IconSetHide(MapObjectView.IconSetIcons.HAMMER);
			}
		}

		// misc..

		public Vector2I LocalToTile(Vector2 lpos) => tilemaps.Ground.LocalToMap(lpos);

		public Vector2I GetMouseHoveredTilePos() {
			var localMousePos = tilemaps.Ground.GetLocalMousePosition();
			return LocalToTile(localMousePos);
		}

	}

}
