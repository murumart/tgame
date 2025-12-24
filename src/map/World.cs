using System;

public enum GroundTileType : short {
	VOID,
	Grass,
	Ocean,
}

public class World {

	public int Longitude { get; init; }
	public int Latitude { get; init; }

	readonly GroundTileType[] Ground;
	readonly byte[] Elevation;


	public World(int width, int height) {
		Longitude = width;
		Latitude = height;
		Ground = new GroundTileType[Longitude * Latitude];
		Elevation = new byte[Longitude * Latitude];
	}

	public void SetTile(int x, int y, GroundTileType tile) {
		Ground[x + y * Longitude] = tile;
	}

	public GroundTileType GetTile(int x, int y) {
		if (x < 0 || x >= Longitude) return GroundTileType.VOID;
		if (y < 0 || y >= Latitude) return GroundTileType.VOID;
		return Ground[x + y * Longitude];
	}

	// elevation float between -1 and 1
	public void SetElevation(int x, int y, float elevation) {
		Elevation[x + y * Longitude] = (byte)((elevation * 0.5f + 0.5f) * byte.MaxValue);
	}

	public float GetElevation(int x, int y) {
		if (x < 0 || x >= Longitude) return -1f;
		if (y < 0 || y >= Latitude) return -1f;
		return ((float)Elevation[x + y * Longitude] / byte.MaxValue) * 2.0f - 1.0f;
	}

}