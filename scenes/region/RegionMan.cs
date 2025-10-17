using Godot;
using Godot.Collections;
using scenes.autoload;
using scenes.region.view;
using scenes.region.view.buildings;
using System;
using System.Collections.Generic;

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
			ui.BuildingTypes = new();
			foreach (var type in buildingTypes) ui.BuildingTypes.Add(type);

			region = GameMan.Singleton.Game.Map.GetRegion(0);
			ui.GetPopulationCount += region.GetPopulationCount;
			ui.GetHomelessPopulationCount += GetHomelessPopulationCount;

			tilemaps.DisplayGround(region);

			camera.Region = region;
		}

		public override void _ExitTree() {
			ui.GetPopulationCount -= region.GetPopulationCount;
			ui.GetPopulationCount -= GetHomelessPopulationCount;
		}

		// building

		public bool CanPlaceBuilding(BuildingView building, Vector2I tilepos) {
			return region.CanPlaceBuilding(building.BuildingType, tilepos);
		}

		public void PlaceBuilding(BuildingView buildingView, Vector2I tilepos) {
			var building = region.PlaceBuilding(buildingView.BuildingType, tilepos);
			BuildingView duplicate = (BuildingView)buildingView.Duplicate();
			DisplayBuilding(duplicate, building, tilepos);
		}

		public void LoadBuildingView(Building building) {
			var view = GD.Load<PackedScene>(building.Type.ScenePath).Instantiate<BuildingView>();
			DisplayBuilding(view, building, building.Position);
		}

		private void DisplayBuilding(BuildingView buildingView, Building building, Vector2I tilepos) {
			buildingsParent.AddChild(buildingView);
			buildingView.Position = Camera.TilePosToWorldPos(tilepos);
			buildingView.Modulate = new Color(1f, 1f, 1f);
			buildingView.Initialise(building);
			buildingView.BuildingClicked += ui.OnBuildingClicked;
		}

		private void OnUIBuildingPlaceRequested(BuildingView building, Vector2I tilePosition) {
			if (CanPlaceBuilding(building, tilePosition)) {
				PlaceBuilding(building, tilePosition);
			}
		}

		public int GetHomelessPopulationCount() {
			return region.HomelessPopulation.Pop;
		}
	}

}
