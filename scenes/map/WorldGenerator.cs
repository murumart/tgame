using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace scenes.map {

	public partial class WorldGenerator : Node {

		readonly Vector2I[] GrowDirs = { Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up };

		[Export] FastNoiseLite continentNoise;

		[Export] public int WorldWidth;
		[Export] public int WorldHeight;
		[Export] int RegionCount;
		[Export] Curve islandCurve;

		Region[] regions; public Region[] Regions => regions;
		RegionFaction[] regionFactions;
		Faction[] factions;

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

					if (sample > 0) world.SetTile(x, y, GroundTileType.GRASS);
					else world.SetTile(x, y, GroundTileType.WATER);
				}
			}
		}

		public async Task<Map> GenerateRegions(World world) {
			return await GenerateRegions(world, RegionCount);
		}

		public async Task<Map> GenerateRegions(World world, int regionCount) {
			var occupied = GenerateRegionStarts(world, regionCount); // coordinates in global space

			await GrowRegions(world, occupied);

			Map map = CreateMap();

			return map;
		}

		public Dictionary<Vector2I, Region> GenerateRegionStarts(World world, int regionCount) {
			regions = new Region[regionCount];
			regionFactions = new RegionFaction[regionCount];
			factions = new Faction[regionCount];
			var regionEdgeTiles = new List<Vector2I>[regionCount];
			var startPoses = new Dictionary<Vector2I, Region>();
			int regionsMade = 0;

			while (regionsMade < regionCount) {
				var tile = new Vector2I(rng.RandiRange(0, WorldWidth - 1), rng.RandiRange(0, WorldHeight - 1));
				while (
					world.GetTile(tile.X, tile.Y) != GroundTileType.GRASS
					|| startPoses.ContainsKey(tile)
				) {
					tile = new Vector2I(rng.RandiRange(0, WorldWidth - 1), rng.RandiRange(0, WorldHeight - 1));
				}
				Debug.Assert(!startPoses.ContainsKey(tile), "Two regions can't start on the same tile!");
				var region = new Region(tile, new());
				startPoses.Add(tile, region);
				var fac = new Faction();
				var rfac = fac.CreateOwnedFaction(region);
				region.GroundTiles[Vector2I.Zero] = GroundTileType.GRASS;

				regions[regionsMade] = region;
				regionFactions[regionsMade] = rfac;
				factions[regionsMade] = fac;

				regionsMade++;
			}
			return startPoses;
		}


		public async Task GrowRegions(World world, Dictionary<Vector2I, Region> occupied) {

			var tw = CreateTween().SetLoops(0);
			var growCallback = Callable.From(() => {
				var grew = GrowAllRegionsOneStep(occupied, world);
				if (!grew) {
					tw.EmitSignal("finished");
					tw.Stop();
				}
			});

			tw.TweenInterval(0.06);
			tw.TweenCallback(growCallback);

			await ToSignal(tw, "finished");

		}

		private bool GrowAllRegionsOneStep(Dictionary<Vector2I, Region> occupied, World world) {
			var growthOccurred = false;
			//for (int i = 0; i < RegionCount; i++) {
			//	var region = regions[i];
			//	foreach (var xy in region.GroundTiles.Keys) {
			//		var wpos = xy + region.WorldPosition;
			//		Debug.Assert(!occupied.ContainsKey(wpos), $"Collecting occupied tiles failed: {wpos} is already occupied");
			//		occupied.Add(wpos, region);
			//	}
			//}

			for (int i = 0; i < RegionCount; i++) {
				var addKeys = new HashSet<Vector2I>(); // coordinates in region local space
				addKeys.Clear();
				var region = regions[i];
				foreach (var dir in GrowDirs) {
					growthOccurred = GrowRegionInDirectionRandom(occupied, addKeys, region, dir, world) || growthOccurred;
				}
				foreach (var k in addKeys) {
					Debug.Assert(!region.GroundTiles.ContainsKey(k), $"region {region} already owns the local tile {k}");
					region.GroundTiles.Add(k, GroundTileType.GRASS);
				}
			}
			if (!growthOccurred) {
				GD.Print("region growth filled up all space attainable");
			}
			return growthOccurred;
		}

		private bool GrowRegionInDirectionDeterm(Dictionary<Vector2I, Region> occupied, HashSet<Vector2I> addKeys, Region region, Vector2I dir, World world) {
			bool growthOccurred = false;
			foreach (var xy in region.GroundTiles.Keys) {
				var move = xy + dir + region.WorldPosition;
				var (neighbor, grew) = TryGrowRegionTo(region, move, occupied, addKeys, world);
				growthOccurred = growthOccurred || grew;
				if (!growthOccurred && neighbor != region) region.AddNeighbor(neighbor);
			}
			return growthOccurred;
		}

		private bool GrowRegionInDirectionRandom(
			Dictionary<Vector2I, Region> occupied, // global spcae
			HashSet<Vector2I> addKeys, // region space
			Region region,
			Vector2I dir,
			World world
		) {
			var karr = region.GroundTiles.Keys.ToArray();
			for (int i = 0; i < 100; i++) {
				var rtile = karr[rng.RandiRange(0, karr.Length - 1)];
				var move = rtile + region.WorldPosition + dir;
				var (neighbor, grew) = TryGrowRegionTo(region, move, occupied, addKeys, world);
				if (grew) region.AddNeighbor(neighbor);
				if (grew) {
					return true;
				}
			}
			return false;
		}

		private (Region, bool) TryGrowRegionTo(
			Region region,
			Vector2I where, // world space
			Dictionary<Vector2I, Region> occupied, // world space
			HashSet<Vector2I> addKeys, // region space
			World world
		) {
			occupied.TryGetValue(where, out Region there);
			var local = where - region.WorldPosition;
			if (there == null && world.GetTile(where.X, where.Y) == GroundTileType.GRASS && !addKeys.Contains(local)) {
				addKeys.Add(local);
				Debug.Assert(!occupied.ContainsKey(where), "Tile I thought was good to grow onto is already occupied!!");
				occupied.Add(where, region);
				return (null, true);
			}
			return (there, false);
		}

		Map CreateMap() {
			Map map = new(regions, factions, regionFactions);

			foreach (Region region in regions) {
				foreach (Vector2I pos in region.GroundTiles.Keys) {
					if (pos == Vector2I.Zero) continue; // starter house position
					if (GD.Randf() < 0.01f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("boulder"), pos);
					else if (GD.Randf() < 0.07f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("trees"), pos);
					else if (GD.Randf() < 0.003f) region.CreateResourceSiteAndPlace(Registry.ResourceSites.GetAsset("clay_pit"), pos);
				}
			}
			return map;
		}

	}

}
