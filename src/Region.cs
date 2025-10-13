using System;
using System.Collections.Generic;
using Godot;

public enum GroundTileType : short {
	VOID,
	GRASS,
}

public class Region {

	Dictionary<Vector2I, GroundTileType> groundTiles = new();
	public Dictionary<Vector2I, GroundTileType> GroundTiles {
		get { return groundTiles; }
		set { }
	}
	Dictionary<Vector2I, int> higherTiles = new();
	Dictionary<Vector2I, Building> buildings = new();

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

}
