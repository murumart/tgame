using Godot;
using Godot.Collections;
using scenes.autoload;
using scenes.region.view;
using scenes.region.view.buildings;
using System;
using System.Collections.Generic;
using static Building;

namespace scenes.region {

	public partial class RegionMan : Node {

		[Export] UI ui;
		[Export] Camera camera;
		[Export] Tilemaps tilemaps;

		[ExportGroup("Building")]
		[Export] Array<BuildingType> buildingTypes;
		[Export] Node2D buildingsParent;

		Region region;

		// setup

		public override void _Ready() {
			ui.BuildRequested += OnUIBuildingPlaceRequested;
			ui.GetBuildingTypes += GetBuildingTypes;

			region = GameMan.Singleton.Game.Map.GetRegion(0);
			ui.GetPopulationCount += region.GetPopulationCount;
			ui.GetHomelessPopulationCount += GetHomelessPopulationCount;

			tilemaps.DisplayGround(region);

			camera.Region = region;
		}

		public override void _Notification(int what) {
			if (what == NotificationPredelete) {
				ui.GetPopulationCount -= region.GetPopulationCount;
				ui.GetPopulationCount -= GetHomelessPopulationCount;
				ui.GetBuildingTypes -= GetBuildingTypes;
				ui.BuildRequested -= OnUIBuildingPlaceRequested;
			}
		}

		// building

		public bool CanPlaceBuilding(IBuildingType type, Vector2I tilepos) {
			return region.CanPlaceBuilding(type, tilepos);
		}

		public void PlaceBuilding(IBuildingType type, Vector2I tilepos) {
	    	var view = GD.Load<PackedScene>(type.GetScenePath()).Instantiate<BuildingView>();
	    	var building = region.PlaceBuilding(type, tilepos);
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
			foreach (var b in buildingTypes) list.Add(b);
			return list;
		}

		public int GetHomelessPopulationCount() {
			return region.HomelessPopulation.Pop;
		}
	}

}
