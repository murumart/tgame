using Godot;
using Godot.Collections;
using scenes.region.view;
using scenes.region.view.buildings;
using System;

namespace scenes.region {

	public partial class GameMan : Node {

		[Export] UI ui;
		[Export] Tilemaps tilemaps;

		[ExportGroup("Building")]
		[Export] Array<PackedScene> buildingScenes;
		[Export] Node2D buildingsParent;

		Region region;

		// setup

		public override void _Ready() {
			ui.BuildTargetSet += OnUIBuildTargetPicked;
			ui.BuildRequested += OnUIBuildingPlaceRequested;

			region = Region.GetTestCircleRegion(25);

			tilemaps.DisplayGround(region);
		}

		// building

		public bool CanPlaceBuilding(BuildingView building, Vector2I tilepos) {
			return true;
		}

		public void PlaceBuilding(BuildingView building, Vector2I tilepos) {
			BuildingView duplicate = (BuildingView)building.Duplicate();
			buildingsParent.AddChild(duplicate);
			duplicate.Position = Camera.TilePosToWorldPos(tilepos);
			duplicate.Modulate = new Color(1f, 1f, 1f);
		}

		private void OnUIBuildTargetPicked(int target) {
			ui.SetBuildCursorScene(buildingScenes[target]);
		}

		private void OnUIBuildingPlaceRequested(BuildingView building, Vector2I tilePosition) {
			if (CanPlaceBuilding(building, tilePosition)) {
				PlaceBuilding(building, tilePosition);
			}
		}
	}

}
