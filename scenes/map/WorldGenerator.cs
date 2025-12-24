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
		[Export] int FactionCount;
		[Export] Curve islandCurve;

		public Region[] Regions;

		RandomNumberGenerator rng;


		public override void _Ready() {
			rng = new();
		}

		public override void _Process(double delta) {

		}

		public void GenerateContinents(World world) {
			var centre = new Vector2(WorldWidth / 2, WorldHeight / 2);
			for (int x = 0; x < WorldWidth; x++) {
				for (int y = 0; y < WorldHeight; y++) {
					var vec = new Vector2(x, y);

					float sample = continentNoise.GetNoise2D(x, y);

					float distanceSqFromCentre = centre.DistanceSquaredTo(vec) / (float)(Math.Pow(WorldWidth / 2, 2) + Math.Pow(WorldHeight / 2, 2));
					sample -= islandCurve.SampleBaked(distanceSqFromCentre);
					sample = Mathf.Clamp(sample, -1f, 1f);

					world.SetElevation(x, y, sample);
					if (sample > 0) world.SetTile(x, y, GroundTileType.Grass);
					else world.SetTile(x, y, GroundTileType.Ocean);
				}
			}
		}

		public async Task<Map> GenerateRegions(World world) {
			return await GenerateRegions(world, LandRegionCount, SeaRegionCount);
		}

		public async Task<Map> GenerateRegions(World world, int regionCountLand, int regionCountSea) {
			Dictionary<Vector2I, Region> landOccupied; // coordinates in global space
			landOccupied = GenerateRegionStarts(world, regionCountLand);
			Dictionary<Vector2I, Region> seaOccupied; // coordinates in global space
			seaOccupied = GenerateRegionStarts(world, regionCountSea, sea: true);

			Region[] regionsLand = landOccupied.Values.ToArray();
			Region[] regionsSea = seaOccupied.Values.ToArray();

			Dictionary<Region, List<(Vector2I, byte)>> freeEdgeTiles = new();

			foreach (var reg in regionsLand) {
				freeEdgeTiles[reg] = new() { (Vector2I.Zero, 0b1111) };
			}

			GD.Print("Growing land regions");
			await GrowRegions(world, regionsLand, landOccupied, freeEdgeTiles);

			freeEdgeTiles.Clear();
			foreach (var reg in regionsSea) {
				freeEdgeTiles[reg] = new() { (Vector2I.Zero, 0b1111) };
			}

			GD.Print("Growing sea regions");
			await GrowRegions(world, regionsSea, seaOccupied, freeEdgeTiles, sea: true);

			foreach (Region region in regionsLand) {
				foreach (Vector2I pos in region.GroundTiles.Keys) {
					if (pos == Vector2I.Zero) continue; // starter house position
					if (GD.Randf() < 0.01f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("boulder"), pos);
					else if (GD.Randf() < 0.07f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("trees"), pos);
					else if (GD.Randf() < 0.003f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("clay_pit"), pos);
				}
			}

			Map map = CreateMap(regionsLand.Concat(regionsSea).ToArray(), [], []);

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
				var region = new Region(tile, new());
				startPoses.Add(tile, region);

				region.GroundTiles[Vector2I.Zero] = tileType;

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
				var grew = GrowAllRegionsOneStep(regions, occupied, freeEdgeTiles, world, sea: sea, iterations: sea ? 300 : 7);
				Regions = regions;
				if (!grew) {
					tw.EmitSignal("finished");
					tw.Stop();
				}
			});

			tw.TweenCallback(growCallback);
			tw.TweenInterval(0.06);

			await ToSignal(tw, "finished");

		}

		private bool GrowAllRegionsOneStep(
			Region[] regions, Dictionary<Vector2I, Region> occupied,
			Dictionary<Region, List<(Vector2I, byte)>> freeEdgeTiles,
			World world,
			int iterations = 10,
			bool sea = false
		) {
			var growthOccurred = false;
			//for (int i = 0; i < RegionCount; i++) {
			//	var region = regions[i];
			//	foreach (var xy in region.GroundTiles.Keys) {
			//		var wpos = xy + region.WorldPosition;
			//		Debug.Assert(!occupied.ContainsKey(wpos), $"Collecting occupied tiles failed: {wpos} is already occupied");
			//		occupied.Add(wpos, region);
			//	}
			//}

			for (int x = 0; x < iterations; x++) for (int i = 0; i < regions.Length; i++) {
					var addKeys = new HashSet<Vector2I>(); // coordinates in region local space
					addKeys.Clear();
					var region = regions[i];
					var tileType = !sea ? GroundTileType.Grass : GroundTileType.Ocean;
					var freeEdges = freeEdgeTiles[region];
					if (freeEdges.Count == 0) continue;
					for (int dirIx = 0; dirIx < 4; dirIx++) {
						growthOccurred = GrowRegionInDirectionRandomly(occupied, addKeys, freeEdges, region, dirIx, world, tileType) || growthOccurred;
						if (freeEdges.Count == 0) break;
					}
					foreach (var k in addKeys) {
						Debug.Assert(!region.GroundTiles.ContainsKey(k), $"region {region} already owns the local tile {k}");
						region.GroundTiles.Add(k, tileType);
					}
				}
			if (!growthOccurred) {
				GD.Print("region growth filled up all space attainable");
			}
			return growthOccurred;
		}

		private bool GrowRegionInDirectionRandomly(
			Dictionary<Vector2I, Region> occupied, // global spcae
			HashSet<Vector2I> addKeys, // region space
			List<(Vector2I, byte)> freeEdgeTiles, // local space
			Region region,
			int dirIx,
			World world,
			GroundTileType allowedTile
		) {
			var (vectorDirectionTryingToGrowIn, directionTryingToGrowIn) = GrowDirs[dirIx];

			// choose weighted randomly based on index. Not Doing This Right Now. The sorting part is hideously slow (so should cache and carry more things).
			//freeEdgeTiles.Sort((a, b) => {
			//var (localpos1, _) = a;
			//var (localpos2, _) = b;
			//var global1 = localpos1 + region.WorldPosition;
			//var global2 = localpos1 + region.WorldPosition;
			//var moveGlobal1 = global1 + vectorDirectionTryingToGrowIn;
			//var moveGlobal2 = global2 + vectorDirectionTryingToGrowIn;
			//var grad1 = world.GetElevation(moveGlobal1.X, moveGlobal1.Y) - world.GetElevation(global1.X, global1.Y);
			//var grad2 = world.GetElevation(moveGlobal2.X, moveGlobal2.Y) - world.GetElevation(global2.X, global2.Y);
			//return (grad1).CompareTo(grad2);
			//});

			//var sumOfProbabilities = 0.5f * freeEdgeTiles.Count * (1 + freeEdgeTiles.Count);
			//var random = rng.Randf() * sumOfProbabilities;
			//var at = 0f;
			//int i = 0;
			//for (; i < freeEdgeTiles.Count; i++) {
			//	at += freeEdgeTiles.Count - i;
			//	if (random < at) break;
			//}

			int i = rng.RandiRange(0, freeEdgeTiles.Count - 1); // completely random choice
			{
				var (localPos, directionsThatAreFree) = freeEdgeTiles[i];
				if ((directionTryingToGrowIn & directionsThatAreFree) == 0) return false;

				var moveLocal = localPos + vectorDirectionTryingToGrowIn;
				var moveGlobal = region.WorldPosition + moveLocal;
				var (neighbor, grew) = TryGrowRegionTo(region, moveGlobal, occupied, addKeys, world, allowedTile);
				region.AddNeighbor(neighbor);
				neighbor?.AddNeighbor(region);

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
			if (there == null && world.GetTile(where.X, where.Y) == allowedTile && !addKeys.Contains(local)) {
				Debug.Assert(!occupied.ContainsKey(where), "Tile I thought was good to grow onto is already planned to be used!!");
				addKeys.Add(local);
				Debug.Assert(!occupied.ContainsKey(where), "Tile I thought was good to grow onto is already occupied!!");
				occupied.Add(where, region);
				return (null, true);
			}
			return (there, false);
		}

		Map CreateMap(Region[] regions, Faction[] factions, RegionFaction[] regionFactions) {
			Map map = new(regions, factions, regionFactions);

			return map;
		}

	}

}
