using System;
using Godot;

[Flags]
public enum GroundTileType : byte {
	Sea      = 0b00000000,
	HasLand  = 0b00000001,
	HasSand  = 0b00000010,
	HasVeg   = 0b00000100,
	HasSnow  = 0b00001000,
	All      = 0b11111111
}

public static class GroundTileTypeEx {
	public static string UIString(this GroundTileType t) {
		if ((t & GroundTileType.HasLand) != 0) {
			if ((t & GroundTileType.HasSand) != 0 && (t & GroundTileType.HasVeg) != 0) return "sandy grass";
			if ((t & GroundTileType.HasSand) != 0) return "sand";
			if ((t & GroundTileType.HasVeg) != 0) return "grass";
			if ((t & GroundTileType.HasSnow) != 0) return "snow";
			return "land";
		} else {
			if ((t & GroundTileType.HasVeg) != 0) return "water bed";
			return "sea";
		}
	}
}

public class World {

	public int Width { get; init; }
	public int Height { get; init; }
	public uint Seed { get; init; }

	readonly GroundTileType[] Ground;
	readonly byte[] Elevation;
	readonly byte[] SeaWind;
	public readonly Vector2I SeaWindDirection;
	readonly byte[] Temperature;
	readonly byte[] Humidity;


	public World(int width, int height, uint seed) {
		Debug.Assert(width > 0);
		Debug.Assert(height > 0);
		Width = width;
		Height = height;
		Seed = seed;
		Ground = new GroundTileType[Width * Height];
		Elevation = new byte[Width * Height];
		Temperature = new byte[Width * Height];
		SeaWind = new byte[Width * Height];
		for (int i = 0; i < Width * Height; i++) SeaWind[i] = byte.MaxValue;
		Humidity = new byte[Width * Height];
		SeaWindDirection = new(Mathf.PosMod((int)seed * 2 - 1, 2), Mathf.PosMod((int)seed * 2 - 1, 2)); // -1 or 1
		GD.Print("World::World : seawind direction ", SeaWindDirection);
	}

	public void SetTile(int x, int y, GroundTileType tile) {
		Ground[x + y * Width] = tile;
	}

	public GroundTileType GetTile(int x, int y) {
		if (x < 0 || x >= Width) return GroundTileType.Sea;
		if (y < 0 || y >= Height) return GroundTileType.Sea;
		return Ground[x + y * Width];
	}

	public void SetArrFloat(int x, int y, float value, byte[] where) {
		if (x < 0 || x >= Width) return;
		if (y < 0 || y >= Height) return;
		where[x + y * Width] = (byte)((value * 0.5f + 0.5f) * byte.MaxValue);
	}

	public float SetArrFloat(int x, int y, byte[] where, float defauld = -1f) {
		if (x < 0 || x >= Width) return defauld;
		if (y < 0 || y >= Height) return defauld;
		return ((float)where[x + y * Width] / byte.MaxValue) * 2.0f - 1.0f;
	}

	// elevation float between -1 and 1
	public void SetElevation(int x, int y, float elevation) {
		SetArrFloat(x, y, elevation, Elevation);
	}

	public float GetElevation(int x, int y) {
		return SetArrFloat(x, y, Elevation);
	}

	public void SetTemperature(int x, int y, float temperature) {
		SetArrFloat(x, y, temperature, Temperature);
	}

	public float GetTemperature(int x, int y) {
		return SetArrFloat(x, y, Temperature);
	}

	public void SetSeaWind(int x, int y, float wind) {
		SetArrFloat(x, y, wind, SeaWind);
	}

	public float GetSeaWind(int x, int y) {
		return SetArrFloat(x, y, SeaWind, 1.0f);
	}

	public void SetHumidity(int x, int y, float humid) {
		SetArrFloat(x, y, humid, Humidity);
	}

	public float GetHumidity(int x, int y) {
		return SetArrFloat(x, y, Humidity, 1.0f);
	}

}