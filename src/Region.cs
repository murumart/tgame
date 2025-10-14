using System;
using System.Collections.Generic;
using Godot;
using scenes.region.view.buildings;

public enum GroundTileType : short {
	VOID,
	GRASS,
}

public class Region {

	Dictionary<Vector2I, GroundTileType> groundTiles = new();
	public Dictionary<Vector2I, GroundTileType> GroundTiles { get => groundTiles; }
	Dictionary<Vector2I, int> higherTiles = new();
	Dictionary<Vector2I, Building> buildings = new();

	// debugging
	public static Region GetTestCircleRegion(int radius) {
		var tiles = new Dictionary<Vector2I, GroundTileType>();
		for (int i = -radius; i < radius; i++) {
			for (int j = -radius; j < radius; j++) {
				if (i * i + j * j < radius * radius) {
					tiles[new Vector2I(i, j)] = GroundTileType.GRASS;
				}
			}
		}
		var reg = new Region() {
			groundTiles = tiles,
		};
		return reg;
	}

	public bool CanPlaceBuilding(BuildingType type, Vector2I position) {
		return !buildings.ContainsKey(position);
	}

	public Building PlaceBuilding(BuildingType type, Vector2I position) {
		var building = new Building(type);
		buildings[position] = building;
		return building;
	}

}
