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
		RegionFaction[] regionFactions;
		Faction[] factions;
		RandomNumberGenerator rng;


		public override void _Ready() {
			rng = new();
		}

		bool doneGenerating = false;
		public override void _Process(double delta) {
			if (!growingRegions) {
				if (!doneGenerating) {
					doneGenerating = true;
					finishGenCallback(regions, factions, regionFactions);
				}
				return;
			}
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
			regionFactions = new RegionFaction[RegionCount];
			factions = new Faction[RegionCount];
			int regionsMade = 0;

			while (regionsMade < RegionCount) {
				var tile = new Vector2I(rng.RandiRange(0, WorldWidth - 1), rng.RandiRange(0, WorldHeight - 1));
				while (world.GetTile(tile.X, tile.Y) != GroundTileType.GRASS) {
					tile = new Vector2I(rng.RandiRange(0, WorldWidth - 1), rng.RandiRange(0, WorldHeight - 1));
				}
				var region = new Region(tile, new());
				var fac = new Faction();
				var rfac = fac.CreateOwnedFaction(region);
				region.GroundTiles[Vector2I.Zero] = GroundTileType.GRASS;

				regions[regionsMade] = region;
				regionFactions[regionsMade] = rfac;
				factions[regionsMade] = fac;

				regionsMade++;

			}
		}

		public Region[] Regions => regions;
		Action<Region[], Faction[], RegionFaction[]> finishGenCallback;
		bool growingRegions = false;
		World gWorld;
		public void GrowRegions(World world, Action<Region[], Faction[], RegionFaction[]> callback) {
			growingRegions = true;
			doneGenerating = false;
			finishGenCallback = callback;
			gWorld = world;
		}

		private void GrowingRegions() {
			var occupied = new HashSet<Vector2I>(); // coordinates in global space
			var growthOccurred = false;
			var sizes = new short[RegionCount];
			for (int i = 0; i < RegionCount; i++) {
				var region = regions[i];
				foreach (var xy in region.GroundTiles.Keys) {
					occupied.Add(xy + region.WorldPosition);
					sizes[i] += 1;
				}
			}
			var dirs = new Vector2I[] { Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up };
			var addKeys = new HashSet<Vector2I>(); // coordinates in region local space
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

				foreach (var region in regions) {

				}
			}
		}

		private bool TryGrowRegionTo(
			Vector2I where, // world space
			HashSet<Vector2I> occupied, // world space
			HashSet<Vector2I> addKeys, // region space
			Vector2I regionPosition
		) {
			if (gWorld.GetTile(where.X, where.Y) == GroundTileType.GRASS && !occupied.Contains(where)) {
				addKeys.Add(where - regionPosition);
				return true;
			}
			return false;
		}

		private bool GrowRegionInDirectionDeterm(HashSet<Vector2I> occupied, HashSet<Vector2I> addKeys, Region region, Vector2I dir) {
			bool growthOccurred = false;
			foreach (var xy in region.GroundTiles.Keys) {
				var move = xy + dir + region.WorldPosition;
				growthOccurred = TryGrowRegionTo(move, occupied, addKeys, region.WorldPosition) || growthOccurred;
			}
			return growthOccurred;
		}


		private bool GrowRegionInDirectionRandom(
			HashSet<Vector2I> occupied, // global spcae
			HashSet<Vector2I> addKeys, // region space
			Region region,
			Vector2I dir
		) {
			var karr = region.GroundTiles.Keys.ToArray();
			bool growthOccurred = false;
			for (int i = 0; i < 200; i++) {
				var rtile = karr[rng.RandiRange(0, karr.Length - 1)];
				var move = rtile + dir + region.WorldPosition;
				bool grew = TryGrowRegionTo(move, occupied, addKeys, region.WorldPosition);
				if (grew) {
					growthOccurred = true;
					break;
				}
			}
			return growthOccurred;
		}

	}

}
