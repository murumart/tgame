using System;
using System.Collections.Generic;
using Godot;
using scenes.region.view.buildings;

public enum GroundTileType : short {
	VOID,
	GRASS,
}

public class Region : ITimePassing {

	readonly Dictionary<Vector2I, GroundTileType> groundTiles = new();
	public Dictionary<Vector2I, GroundTileType> GroundTiles { get => groundTiles; }
	readonly Dictionary<Vector2I, int> higherTiles = new();
	readonly Dictionary<Vector2I, Building> buildings = new();
	Population homelessPopulation; public Population HomelessPopulation { get => homelessPopulation; }

	public Region() {
		homelessPopulation = new Population(100);
		homelessPopulation.Pop = 10;
	}

	public Region(Dictionary<Vector2I, GroundTileType> groundTiles) : this() {
		this.groundTiles = groundTiles;
	}

	public void PassTime(float secs) {
		foreach (Building building in buildings.Values) {
			building.PassTime(secs);
			if (building.ConstructionProgress >= 1.0 && homelessPopulation.Pop > 0) {
				if (homelessPopulation.CanTransfer(ref building.Population, 1)) {
					homelessPopulation.Transfer(ref building.Population, 1);
				}
			}
		}
	}

	public static Region GetTestCircleRegion(int radius) { // debugging
		var tiles = new Dictionary<Vector2I, GroundTileType>();
		for (int i = -radius; i <= radius; i++) {
			for (int j = -radius; j <= radius; j++) {
				if (i * i + j * j <= radius * radius) {
					tiles[new Vector2I(i, j)] = GroundTileType.GRASS;
				}
			}
		}
		var reg = new Region(tiles);
		return reg;
	}

	public bool CanPlaceBuilding(BuildingType type, Vector2I position) {
		return !buildings.ContainsKey(position);
	}

	public Building PlaceBuilding(BuildingType type, Vector2I position) {
		var building = new Building(type, position);
		buildings[position] = building;
		return building;
	}

	public int GetPopulationCount() {
		int count = homelessPopulation.Pop;
		foreach (var b in buildings.Values) {
			count += b.Population.Pop;
		}
		return count;
	}

}
