using System;

[Flags]
public enum GroundTileType : byte {
	Void  = 0b00000000,
	Grass = 0b00000001,
	Sand  = 0b00000010,
	Land  = 0b01111111,
	Ocean = 0b10000000,
}

public static class GroundTileTypeEx {
	public static string UIString(this GroundTileType t) {
		return t switch {
			GroundTileType.Void => "...",
			GroundTileType.Grass => "grass",
			GroundTileType.Sand => "sand",
			GroundTileType.Land => "??all land",
			GroundTileType.Ocean => "sea",
			_ => throw new NotImplementedException(),
		};
	}
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
		if (x < 0 || x >= Longitude) return GroundTileType.Void;
		if (y < 0 || y >= Latitude) return GroundTileType.Void;
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