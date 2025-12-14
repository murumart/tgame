using System;
using Godot;

namespace scenes.map {

	public partial class WorldGenerator : Node {

		[Export] FastNoiseLite continentNoise;

		[Export] int worldWidth;
		[Export] int worldHeight;
		[Export] Curve islandCurve;


		public World GenerateWorld() {
			var world = new World(worldWidth, worldHeight);
			var centre = new Vector2(worldWidth / 2, worldHeight / 2);
			for (int x = 0; x < worldWidth; x++) {
				for (int y = 0; y < worldHeight; y++) {
					var vec = new Vector2(x, y);

					float sample = continentNoise.GetNoise2D(x, y);

					float distanceSqFromCentre = centre.DistanceSquaredTo(vec) / (float)(Math.Pow(worldWidth / 2, 2) + Math.Pow(worldHeight / 2, 2));
					sample -= islandCurve.SampleBaked(distanceSqFromCentre);

					if (sample > 0) world.SetTile(x, y, GroundTileType.GRASS);
					else world.SetTile(x, y, GroundTileType.WATER);
				}
			}
			return world;
		}
	}

}
