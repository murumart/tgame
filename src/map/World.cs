using System;

public enum GroundTileType : short {
	VOID,
	GRASS,
	WATER,
}

public class World {

	public int Width { get; init; }
	public int Height { get; init; }

	public GroundTileType[] ground;


	public World(int width, int height) {
		Width = width;
		Height = height;
		ground = new GroundTileType[Width * Height];
	}

	public void SetTile(int x, int y, GroundTileType tile) {
		ground[x + y * Width] = tile;
	}

	public GroundTileType GetTile(int x, int y) {
		return ground[x + y * Width];
	}
}