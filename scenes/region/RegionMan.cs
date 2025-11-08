using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.game;
using resources.game.building_types;
using resources.game.resource_site_types;
using scenes.autoload;
using scenes.region.buildings;
using scenes.region.ui;
using static Building;
using static Faction;

namespace scenes.region {

	public partial class RegionMan : Node {

		[Export] UI ui;
		[Export] Camera camera;
		[Export] Tilemaps tilemaps;

		[ExportGroup("Building")]
		[Export] Node2D buildingsParent;

		Region region;
		RegionFaction regionFaction;


		public override void _Ready() { // setup
			ui.MapClickEvent += MapClick;
			ui.RequestBuildEvent += OnUIBuildingPlaceRequested;
			ui.GetBuildingTypesEvent += GetBuildingTypes;
			ui.GetResourcesEvent += GetResourceStorage;
			ui.GetCanBuildEvent += CanBuild;
			ui.GetTimeStringEvent += GetDateTimeString;
			ui.GetBuildingJobsEvent += GetBuildingJobs;
			ui.AddJobRequestedEvent += AddJob;
			ui.GetMaxFreeWorkersEvent += GetJobMaxWorkers;
			ui.ChangeJobWorkerCountEvent += ChangeJobWorkerCount;
			ui.DeleteJobEvent += RemoveJob;

			region = GameMan.Singleton.Game.Map.GetRegion(0);
			regionFaction = GameMan.Singleton.Game.Map.GetFaction(0).GetOwnedRegionFaction(0);
			ui.GetPopulationCountEvent += regionFaction.GetPopulationCount;
			ui.GetHomelessPopulationCountEvent += GetHomelessPopulationCount;
			ui.GetUnemployedPopulationCountEvent += GetUnemployedPopulationCount;

			ui.PauseRequestedEvent += UiTogglePause;
			ui.GameSpeedChangeRequestedEvent += UiChangeGameSpeed;

			tilemaps.DisplayGround(region);

			camera.Region = region;
			foreach (var m in region.GetMapObjects()) {
				LoadMapObjectView(m);
			}
		}

		public override void _Notification(int what) { // teardown
			if (what == NotificationPredelete) {
				ui.MapClickEvent -= MapClick;
				ui.GetPopulationCountEvent -= regionFaction.GetPopulationCount;
				ui.GetHomelessPopulationCountEvent -= GetHomelessPopulationCount;
				ui.GetUnemployedPopulationCountEvent -= GetUnemployedPopulationCount;
				ui.GetBuildingTypesEvent -= GetBuildingTypes;
				ui.RequestBuildEvent -= OnUIBuildingPlaceRequested;
				ui.GetResourcesEvent -= GetResourceStorage;
				ui.GetCanBuildEvent -= CanBuild;
				ui.GetTimeStringEvent -= GetDateTimeString;
				ui.PauseRequestedEvent -= UiTogglePause;
				ui.GameSpeedChangeRequestedEvent -= UiChangeGameSpeed;
				ui.GetBuildingJobsEvent -= GetBuildingJobs;
				ui.AddJobRequestedEvent -= AddJob;
				ui.GetMaxFreeWorkersEvent -= GetJobMaxWorkers;
				ui.ChangeJobWorkerCountEvent -= ChangeJobWorkerCount;
			}
		}

		// map clicks

		void MapClick(Vector2I tile) {
			if (regionFaction.HasBuilding(tile)) {
				ui.OnBuildingClicked(regionFaction.GetBuilding(tile));
			}
		}

		// building

		public bool CanBuild(IBuildingType type) {
			return regionFaction.CanBuild(type);
		}

		public bool CanPlaceBuilding(IBuildingType type, Vector2I tilepos) {
			Debug.Assert(type != null, "Cant place NULL building type!!");
			return regionFaction.CanPlaceBuilding(type, tilepos);
		}

		public void PlaceBuilding(IBuildingType type, Vector2I tilepos) {
			Debug.Assert(type != null, "Cant place NULL building type!!");
			var view = GD.Load<PackedScene>(DataStorage.GetScenePath(type)).Instantiate<BuildingView>();
			var building = regionFaction.PlaceBuildingConstructionSite(type, tilepos);
			DisplayMapObject(view, building, tilepos);
		}

		public void LoadMapObjectView(MapObject mo) {
			MapObjectView view = mo switch {
				Building => GD.Load<PackedScene>(DataStorage.GetScenePath(((Building)mo).Type)).Instantiate<MapObjectView>(),
				ResourceSite => GD.Load<PackedScene>(DataStorage.GetScenePath(((ResourceSite)mo).Type)).Instantiate<MapObjectView>(),
				_ => throw new System.Exception("nooooo wrong wrong wrong wrong its all wrong"),
			};

			DisplayMapObject(view, mo, mo.Position);
		}

		private void DisplayMapObject(MapObjectView view, MapObject mopbject, Vector2I tilepos) {
			buildingsParent.AddChild(view);
			view.Position = Camera.TilePosToWorldPos(tilepos);
			view.Modulate = new Color(1f, 1f, 1f);
		}

		private void OnUIBuildingPlaceRequested(IBuildingType type, Vector2I tilePosition) {
			if (CanPlaceBuilding(type, tilePosition)) {
				PlaceBuilding(type, tilePosition);
			}
		}

		public List<BuildingType> GetBuildingTypes() {
			var list = new List<BuildingType>();
			foreach (var b in Registry.Buildings.GetAssets()) list.Add((BuildingType)b);
			return list;
		}

		// get information (for UI)

		public int GetHomelessPopulationCount() => regionFaction.HomelessPopulation.Amount;

		public int GetUnemployedPopulationCount() => regionFaction.UnemployedPopulation.Amount;

		public ResourceStorage GetResourceStorage() {
			return regionFaction.Resources;
		}

		public int GetJobMaxWorkers() => regionFaction.GetFreeWorkers();

		public string GetTimeString() => $"{GameMan.Singleton.Game.Time.GetDayHour():00}:{GameMan.Singleton.Game.Time.GetHourMinute():00}";
		public string GetDateTimeString() => $"{GetTimeString()} {GameMan.Singleton.Game.Time.GetMonthDay():00}/{GameMan.Singleton.Game.Time.GetMonth() + 1:00}";

		// UI action invokes

		public void UiChangeGameSpeed(float by) {
			GameMan.Singleton.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, by);
		}

		public bool UiTogglePause() {
			GameMan.Singleton.TogglePause();
			return GameMan.Singleton.IsPaused;
		}

		public void AddJob(MapObject place, Job job) {
			regionFaction.AddJob(place.Position, job);
		}

		public void RemoveJob(JobBox jbox) {
			regionFaction.RemoveJob(jbox.Position, jbox.Debox());
		}

		public HashSet<JobBox> GetBuildingJobs(Building building) {
			var pos = building.Position;
			var jbox = new HashSet<JobBox>();
			foreach (var job in regionFaction.GetJobs(pos)) {
				jbox.Add(new JobBox(job, building));
			}
			return jbox;
		}

		public void ChangeJobWorkerCount(JobBox jbox, int by) {
			regionFaction.EmployWorkers(jbox.Debox(), by);
		}

	}

}
