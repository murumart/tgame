using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace scenes.map {

	public partial class WorldGenerator : Node {

		readonly (Vector2I, byte)[] GrowDirs = { (Vector2I.Right, 0b1), (Vector2I.Left, 0b10), (Vector2I.Down, 0b100), (Vector2I.Up, 0b1000) };

		[Export] FastNoiseLite continentNoise;

		[Export] public int WorldWidth;
		[Export] public int WorldHeight;
		[Export] int LandRegionCount;
		[Export] int SeaRegionCount;
		[Export] int AggressiveFactionCount;
		[Export] Curve islandCurve;
		[Export] Curve populationLandTileCurve;

		public Region[] Regions;

		RandomNumberGenerator rng;

		public bool Generating { get; private set; }


		public override void _Ready() {
			rng = new();
		}

		public void GenerateContinents(World world) {
			Generating = true;

			continentNoise.Seed = (int)rng.Randi();

			var centre = new Vector2(world.Longitude / 2, world.Latitude / 2);
			var divBySidelen = 1.0f / (float)(Math.Pow(world.Longitude / 2.0, 2) + Math.Pow(world.Latitude / 2.0, 2));
			for (int x = 0; x < world.Longitude; x++) {
				for (int y = 0; y < world.Latitude; y++) {
					var vec = new Vector2(x, y);

					float sample = continentNoise.GetNoise2D(x, y);

					float distanceSqFromCentre = centre.DistanceSquaredTo(vec) * divBySidelen;
					sample -= islandCurve.SampleBaked(distanceSqFromCentre);
					sample = Mathf.Clamp(sample, -1f, 1f);

					world.SetElevation(x, y, sample);
				}
			}

			for (int x = 0; x < world.Longitude; x++) {
				for (int y = 0; y < world.Latitude; y++) {
					var ele = world.GetElevation(x, y);
					if (ele < 0) world.SetTile(x, y, GroundTileType.Ocean);
					else if (ele < 0.02) world.SetTile(x, y, GroundTileType.Sand);
					else world.SetTile(x, y, GroundTileType.Grass);
				}
			}
			Generating = false;
		}

		public async Task<Map> GenerateRegions(World world) {
			return await GenerateRegions(world, LandRegionCount, SeaRegionCount);
		}

		public async Task<Map> GenerateRegions(World world, int regionCountLand, int regionCountSea) {
			Generating = true;
			Dictionary<Vector2I, Region> landOccupied; // coordinates in global space
			landOccupied = GenerateRegionStarts(world, regionCountLand);

			Region[] regionsLand = landOccupied.Values.ToArray();

			Dictionary<Region, List<(Vector2I, byte)>> freeEdgeTiles = new();

			foreach (var reg in regionsLand) {
				freeEdgeTiles[reg] = new() { (Vector2I.Zero, 0b1111) };
			}

			GD.Print("Growing land regions");
			await GrowRegions(world, regionsLand, landOccupied, freeEdgeTiles);

			freeEdgeTiles.Clear();

			foreach (Region region in regionsLand) {
				foreach (Vector2I pos in region.GroundTiles.Keys) {
					if (pos == Vector2I.Zero) continue; // starter house position..
					if ((region.GroundTiles[pos] & GroundTileType.Land) == 0) continue;
					if (GD.Randf() < 0.01f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("boulder"), pos);
					else if (GD.Randf() < 0.07f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("trees"), pos);
					else if (GD.Randf() < 0.003f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("clay_pit"), pos);
				}
			}

			War(regionsLand, AggressiveFactionCount);

			Map map = new(regionsLand);

			Generating = false;
			return map;
		}

		public Dictionary<Vector2I, Region> GenerateRegionStarts(World world, int regionCount, bool sea = false) {
			var startPoses = new Dictionary<Vector2I, Region>();
			int regionsMade = 0;

			var tileType = !sea ? GroundTileType.Grass : GroundTileType.Ocean;

			while (regionsMade < regionCount) {
				var tile = new Vector2I(rng.RandiRange(0, world.Longitude - 1), rng.RandiRange(0, world.Latitude - 1));
				while (
					world.GetTile(tile.X, tile.Y) != tileType
					|| startPoses.ContainsKey(tile)
				) {
					tile = new Vector2I(rng.RandiRange(0, world.Longitude - 1), rng.RandiRange(0, world.Latitude - 1));
				}
				Debug.Assert(!startPoses.ContainsKey(tile), "Two regions can't start on the same tile!");
				var region = new Region(regionsMade, tile, new());
				startPoses.Add(tile, region);

				region.GroundTiles[Vector2I.Zero] = world.GetTile(tile.X, tile.Y);

				regionsMade++;
			}
			return startPoses;
		}


		public async Task GrowRegions(
			World world,
			Region[] regions,
			Dictionary<Vector2I, Region> occupied,
			Dictionary<Region, List<(Vector2I, byte)>> freeEdgeTiles,
			bool sea = false
		) {

			var tw = CreateTween().SetLoops(0);
			var growCallback = Callable.From(() => {
				var grew = GrowAllRegionsOneStep(regions, occupied, freeEdgeTiles, world, sea: sea, iterations: 17);
				Regions = regions;
				if (!grew) {
					tw.EmitSignal("finished");
					tw.Stop();
				}
			});

			tw.TweenCallback(growCallback);
			tw.TweenInterval(0.06);

			await ToSignal(tw, "finished");

			// fill regionless tiles
			//for (int x = 0; x < world.Longitude; x++) {
			//	for (int y = 0; y< world.Latitude; y++) {
			//		var pos = new  Vector2I(x, y);
			//		if (occupied.ContainsKey(pos)) continue;
			//		Region closest = regions[0];
			//		foreach (var reg in regions) {
			//			if (pos.DistanceSquaredTo(reg.WorldPosition) < pos.DistanceSquaredTo(closest.WorldPosition)) {
			//				closest = reg;
			//			}
			//		}
			//		closest.GroundTiles.Add(pos - closest.WorldPosition, world.GetTile(x, y));
			//	}
			//	if (x % 36 == 0) await ToSignal(GetTree(), "process_frame");
			//}

		}

		private bool GrowAllRegionsOneStep(
			Region[] regions, Dictionary<Vector2I, Region> occupied,
			Dictionary<Region, List<(Vector2I, byte)>> freeEdgeTiles,
			World world,
			int iterations = 10,
			bool sea = false
		) {
			var growthOccurred = false;
			var tileType = !sea ? GroundTileType.Grass : GroundTileType.Ocean;

			for (int i = 0; i < regions.Length; i++) {
				var region = regions[i];
				var freeEdges = freeEdgeTiles[region];
				var c = freeEdges.Count;
				for (int x = 0; x < c; x++) {
					var addKeys = new HashSet<Vector2I>(); // coordinates in region local space
					addKeys.Clear();
					for (int dirIx = 0; dirIx < 4; dirIx++) {
						var ix = rng.RandiRange(0, freeEdges.Count - 1);
						growthOccurred = GrowRegionInDirection(occupied, addKeys, freeEdges, ix, region, dirIx, world, tileType) || growthOccurred;
						if (freeEdges.Count == 0) break;
					}
					foreach (var k in addKeys) {
						Debug.Assert(!region.GroundTiles.ContainsKey(k), $"region {region} already owns the local tile {k}");
						region.GroundTiles.Add(k, world.GetTile(k.X + region.WorldPosition.X, k.Y + region.WorldPosition.Y));
					}
					if (freeEdges.Count == 0) break;
				}
			}
			if (!growthOccurred) {
				GD.Print("region growth filled up all space attainable");
			}
			return growthOccurred;
		}

		private bool GrowRegionInDirection(
			Dictionary<Vector2I, Region> occupied, // global spcae
			HashSet<Vector2I> addKeys, // region space
			List<(Vector2I, byte)> freeEdgeTiles, // local space
			int tileIndex,
			Region region,
			int dirIx,
			World world,
			GroundTileType allowedTile
		) {
			var (vectorDirectionTryingToGrowIn, directionTryingToGrowIn) = GrowDirs[dirIx];

			int i = tileIndex;
			{
				var (localPos, directionsThatAreFree) = freeEdgeTiles[i];
				if ((directionTryingToGrowIn & directionsThatAreFree) == 0) return false;

				var moveLocal = localPos + vectorDirectionTryingToGrowIn;
				var moveGlobal = region.WorldPosition + moveLocal;
				var (neighbor, grew) = TryGrowRegionTo(region, moveGlobal, occupied, addKeys, world, allowedTile);
				if (neighbor != null && neighbor != region) {
					region.AddNeighbor(neighbor);
					neighbor.AddNeighbor(region);
				}

				directionsThatAreFree &= (byte)~directionTryingToGrowIn;
				if (grew) {
					byte opposite = directionTryingToGrowIn switch { 0b10 => 0b1, 0b01 => 0b10, 0b100 => 0b1000, 0b1000 => 0b100, _ => throw new NotImplementedException() };
					freeEdgeTiles.Add((moveLocal, (byte)(0b1111 & (byte)~opposite)));
				}

				if (directionsThatAreFree == 0) freeEdgeTiles.RemoveAt(i);
				else {
					freeEdgeTiles[i] = (localPos, directionsThatAreFree);
				}

				return grew;
			}
		}

		private (Region, bool) TryGrowRegionTo(
			Region region,
			Vector2I where, // world space
			Dictionary<Vector2I, Region> occupied, // world space
			HashSet<Vector2I> addKeys, // region space
			World world,
			GroundTileType allowedTile
		) {
			occupied.TryGetValue(where, out Region there);
			var local = where - region.WorldPosition;
			var tileAt = world.GetTile(where.X, where.Y);
			if (there == null && /*tileAt == allowedTile && */ tileAt != GroundTileType.Void && !addKeys.Contains(local)) {
				Debug.Assert(!occupied.ContainsKey(where), "Tile I thought was good to grow onto is already planned to be used!!");
				addKeys.Add(local);
				Debug.Assert(!occupied.ContainsKey(where), "Tile I thought was good to grow onto is already occupied!!");
				occupied.Add(where, region);
				return (null, true);
			}
			return (there, false);
		}

		enum Behavior {
			Occupy,
			Annex,
			Max
		}

		private void War(Region[] regions, int aggressiveRegionCount) {
			const int MAX_POP = 1000;
			// all regions get initial populations
			foreach (Region region in regions) {
				var faction = new Faction(
					region,
					maxPop: MAX_POP,
					initialPopulation: (int)populationLandTileCurve.SampleBaked(region.LandTileCount)
				);
			}
			// TODO implement while not tired
		}

	}

}
