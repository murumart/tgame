using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace scenes.map {

	public partial class WorldGenerator : Node {

		[Export] FastNoiseLite continentNoise;

		[Export] public int WorldWidth;
		[Export] public int WorldHeight;
		[Export] int RegionCount;
		[Export] Curve islandCurve;

		Region[] regions;
		RandomNumberGenerator rng;


		public override void _Ready() {
			rng = new();
		}

		public override void _Process(double delta) {
			if (!growingRegions) return;
			GrowingRegions();
			GrowingRegions();
			GrowingRegions();
			GrowingRegions();
			GrowingRegions();
			GrowingRegions();
			GrowingRegions();
			GrowingRegions();
			GrowingRegions();
		}

		public void GenerateContinents(World world) {
			var centre = new Vector2(WorldWidth / 2, WorldHeight / 2);
			for (int x = 0; x < WorldWidth; x++) {
				for (int y = 0; y < WorldHeight; y++) {
					var vec = new Vector2(x, y);

					float sample = continentNoise.GetNoise2D(x, y);

					float distanceSqFromCentre = centre.DistanceSquaredTo(vec) / (float)(Math.Pow(WorldWidth / 2, 2) + Math.Pow(WorldHeight / 2, 2));
					sample -= islandCurve.SampleBaked(distanceSqFromCentre);

					if (sample > 0) world.SetTile(x, y, GroundTileType.GRASS);
					else world.SetTile(x, y, GroundTileType.WATER);
				}
			}
		}

		public void GenerateRegionStarts(World world) {
			regions = new Region[RegionCount];
			int regionsMade = 0;

			while (regionsMade < RegionCount) {
				var region = new Region();
				var tile = new Vector2I(rng.RandiRange(0, WorldWidth - 1), rng.RandiRange(0, WorldHeight - 1));
				while (world.GetTile(tile.X, tile.Y) != GroundTileType.GRASS) {
					tile = new Vector2I(rng.RandiRange(0, WorldWidth - 1), rng.RandiRange(0, WorldHeight - 1));
				}
				region.GroundTiles[tile] = GroundTileType.GRASS;
				regions[regionsMade++] = region;
			}
		}

		public Region[] Regions => regions;

		bool growingRegions = false;
		World gWorld;
		public void GrowRegions(World world) {
			growingRegions = true;
			gWorld = world;
		}

		private void GrowingRegions() {
			var occupied = new HashSet<Vector2I>();
			var growthOccurred = false;
			var sizes = new short[RegionCount];
			for (int i = 0; i < RegionCount; i++) {
				var region = regions[i];
				foreach (var xy in region.GroundTiles.Keys) {
					occupied.Add(xy);
					sizes[i] += 1;
				}
			}
			var dirs = new Vector2I[] { Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up };
			var addKeys = new HashSet<Vector2I>();
			for (int i = 0; i < RegionCount; i++) {
				addKeys.Clear();
				var region = regions[i];
				foreach (var dir in dirs) {
					growthOccurred = GrowRegionInDirectionRandom(occupied, addKeys, region, dir) || growthOccurred;
				}
				foreach (var k in addKeys) {
					region.GroundTiles.Add(k, GroundTileType.GRASS);
				}
			}
			if (!growthOccurred) {
				growingRegions = false;
				GD.Print("region growth filled up all space attainable");
			}
		}

		private bool TryGrowRegionTo(Vector2I where, HashSet<Vector2I> occupied, HashSet<Vector2I> addKeys) {
			if (gWorld.GetTile(where.X, where.Y) == GroundTileType.GRASS && !occupied.Contains(where)) {
				addKeys.Add(where);
				return true;
			}
			return false;
		}

		private bool GrowRegionInDirectionDeterm(HashSet<Vector2I> occupied, HashSet<Vector2I> addKeys, Region region, Vector2I dir) {
			bool growthOccurred = false;
			foreach (var xy in region.GroundTiles.Keys) {
				var move = xy + dir;
				growthOccurred = TryGrowRegionTo(move, occupied, addKeys) || growthOccurred;
			}
			return growthOccurred;
		}


		private bool GrowRegionInDirectionRandom(HashSet<Vector2I> occupied, HashSet<Vector2I> addKeys, Region region, Vector2I dir) {
			var karr = region.GroundTiles.Keys.ToArray();
			bool growthOccurred = false;
			for (int i = 0; i < 200; i++) {
				var rtile = karr[rng.RandiRange(0, karr.Length - 1)];
				var move = rtile + dir;
				bool grew = TryGrowRegionTo(move, occupied, addKeys);
				if (grew) {
					growthOccurred = true;
					break;
				}
			}
			return growthOccurred;
		}

	}

}
