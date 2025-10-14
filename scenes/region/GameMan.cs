using Godot;
using Godot.Collections;
using scenes.region.view;
using scenes.region.view.buildings;
using System;
using System.Collections.Generic;

namespace scenes.region {

	public partial class GameMan : Node {

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

			region = Region.GetTestCircleRegion(6);

			tilemaps.DisplayGround(region);

			camera.Region = region;
		}

		// building

		public bool CanPlaceBuilding(BuildingView building, Vector2I tilepos) {
			return region.CanPlaceBuilding(building.BuildingType, tilepos);
		}

		public void PlaceBuilding(BuildingView buildingView, Vector2I tilepos) {
			var building = region.PlaceBuilding(buildingView.BuildingType, tilepos);
			BuildingView duplicate = (BuildingView)buildingView.Duplicate();
			buildingsParent.AddChild(duplicate);
			duplicate.Position = Camera.TilePosToWorldPos(tilepos);
			duplicate.Modulate = new Color(1f, 1f, 1f);
			duplicate.Initialise(building);
		}

		private void OnUIBuildingPlaceRequested(BuildingView building, Vector2I tilePosition) {
			if (CanPlaceBuilding(building, tilePosition)) {
				PlaceBuilding(building, tilePosition);
			}
		}
	}

}
