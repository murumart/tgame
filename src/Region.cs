using System.Collections.Generic;
using Godot;
using Jobs;
using IBuildingType = Building.IBuildingType;

public enum GroundTileType : short {
	VOID,
	GRASS,
}

public class Region : ITimePassing {

	readonly Dictionary<Vector2I, GroundTileType> groundTiles = new();
	public Dictionary<Vector2I, GroundTileType> GroundTiles { get => groundTiles; }
	readonly Dictionary<Vector2I, int> higherTiles = new();

	readonly Dictionary<Vector2I, MapObject> mapObjects = new();

	readonly List<Region> neighbors = new();


	public Region() { }

	public Region(Dictionary<Vector2I, GroundTileType> groundTiles) : this() {
		this.groundTiles = groundTiles;
	}

	public void PassTime(float hours) {
		foreach (MapObject ob in mapObjects.Values) {
			ob.PassTime(hours);
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

	public bool CanPlaceBuilding(Vector2I position) {
		return !mapObjects.ContainsKey(position);
	}

	public Building CreateBuildingSpotAndPlace(IBuildingType type, Vector2I position) {
		var building = type.CreateBuildingObject(position);
		mapObjects[position] = building;
		return building;
	}

}
