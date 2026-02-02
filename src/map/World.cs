using System;
using Godot;

[Flags]
public enum GroundTileType : byte {
	Void  = 0b00000000,
	Grass = 0b00000001,
	Sand  = 0b00000010,
	Snow  = 0b00000100,
	Land  = 0b01111111,
	Ocean = 0b10000000,
}

public static class GroundTileTypeEx {
	public static string UIString(this GroundTileType t) {
		return t switch {
			GroundTileType.Void => "...",
			GroundTileType.Grass => "grass",
			GroundTileType.Sand => "sand",
			GroundTileType.Snow => "snow",
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
	readonly byte[] SeaWind;
	public readonly Vector2I SeaWindDirection;
	readonly byte[] Temperature;
	readonly byte[] Humidity;


	public World(int width, int height) {
		Longitude = width;
		Latitude = height;
		Ground = new GroundTileType[Longitude * Latitude];
		Elevation = new byte[Longitude * Latitude];
		Temperature = new byte[Longitude * Latitude];
		SeaWind = new byte[Longitude * Latitude];
		for (int i = 0; i < Longitude * Latitude; i++) SeaWind[i] = byte.MaxValue;
		Humidity = new byte[Longitude * Latitude];
		SeaWindDirection = new(((int)GD.Randi() * 2 - 1) % 2, ((int)GD.Randi() % 2 * 2 - 1)); // -1 or 1
	}

	public void SetTile(int x, int y, GroundTileType tile) {
		Ground[x + y * Longitude] = tile;
	}

	public GroundTileType GetTile(int x, int y) {
		if (x < 0 || x >= Longitude) return GroundTileType.Void;
		if (y < 0 || y >= Latitude) return GroundTileType.Void;
		return Ground[x + y * Longitude];
	}

	public void SetArrFloat(int x, int y, float value, byte[] where) {
		if (x < 0 || x >= Longitude) return;
		if (y < 0 || y >= Latitude) return;
		where[x + y * Longitude] = (byte)((value * 0.5f + 0.5f) * byte.MaxValue);
	}

	public float SetArrFloat(int x, int y, byte[] where, float defauld = -1f) {
		if (x < 0 || x >= Longitude) return defauld;
		if (y < 0 || y >= Latitude) return defauld;
		return ((float)where[x + y * Longitude] / byte.MaxValue) * 2.0f - 1.0f;
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