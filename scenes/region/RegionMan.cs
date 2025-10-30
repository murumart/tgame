using System.Collections.Generic;
using Godot;
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

		// setup

		public override void _Ready() {
			ui.BuildRequestedEvent += OnUIBuildingPlaceRequested;
			ui.GetBuildingTypesEvent += GetBuildingTypes;
			ui.GetResourcesEvent += GetResourceStorage;
			ui.GetCanBuildEvent += CanBuild;

			region = GameMan.Singleton.Game.Map.GetRegion(0);
			regionFaction = GameMan.Singleton.Game.Map.GetFaction(0).GetOwnedRegionFaction(0);
			ui.GetPopulationCountEvent += regionFaction.GetPopulationCount;
			ui.GetHomelessPopulationCountEvent += GetHomelessPopulationCount;

			tilemaps.DisplayGround(region);

			camera.Region = region;
		}

		public override void _Notification(int what) {
			if (what == NotificationPredelete) {
				ui.GetPopulationCountEvent -= regionFaction.GetPopulationCount;
				ui.GetPopulationCountEvent -= GetHomelessPopulationCount;
				ui.GetBuildingTypesEvent -= GetBuildingTypes;
				ui.BuildRequestedEvent -= OnUIBuildingPlaceRequested;
				ui.GetResourcesEvent -= GetResourceStorage;
				ui.GetCanBuildEvent -= CanBuild;
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
			var view = GD.Load<PackedScene>(type.GetScenePath()).Instantiate<BuildingView>();
			var building = regionFaction.PlaceBuilding(type, tilepos);
			DisplayBuilding(view, building, tilepos);
		}

		public void LoadBuildingView(Building building) {
			var view = GD.Load<PackedScene>(building.Type.GetScenePath()).Instantiate<BuildingView>();
			DisplayBuilding(view, building, building.Position);
		}

		private void DisplayBuilding(BuildingView view, Building building, Vector2I tilepos) {
			buildingsParent.AddChild(view);
			view.Position = Camera.TilePosToWorldPos(tilepos);
			view.Modulate = new Color(1f, 1f, 1f);
			view.Initialise(building);
			view.BuildingClicked += ui.OnBuildingClicked;
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

		public int GetHomelessPopulationCount() {
			return regionFaction.HomelessPopulation.Pop;
		}

		public ResourceStorage GetResourceStorage() {
			return regionFaction.Resources;
		}

	}

}
