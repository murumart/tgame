using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using resouces.game;
using resources.game;
using static ResourceSite;

namespace scenes.map {

	public partial class WorldGenerator : Node {

		readonly (Vector2I, byte)[] GrowDirs = { (Vector2I.Right, 0b1), (Vector2I.Left, 0b10), (Vector2I.Down, 0b100), (Vector2I.Up, 0b1000) };

		readonly struct KernelVal(int x, int y, float coef) {
			public readonly int X => x;
			public readonly int Y => y;
			public readonly float Coef => coef;
		}
		readonly KernelVal[] kernel = {
			new( 0,  0, 0.30f),
			new( 1,  0, 0.15f),
			new(-1,  0, 0.15f),
			new( 0,  1, 0.15f),
			new( 0, -1, 0.15f),
			new( 2,  0, 0.25f),
			new( -2, 0, 0.25f),
			new( 0,  2, 0.25f),
			new( 0, -2, 0.25f),
		};

		[Export] FastNoiseLite continentNoise;
		[Export] FastNoiseLite temperatureNoise;
		[Export] FastNoiseLite humidityNoise;
		[Export] FastNoiseLite seawindGainNoise;

		[Export] public int WorldWidth;
		[Export] public int WorldHeight;
		[Export] int LandRegionCount;
		[Export] int AggressiveFactionCount;
		[Export] Curve islandCurve;
		[Export] Curve seawindElevationDifferenceReductionCurve;
		[Export] Curve seawindTemperatureModCurve;
		[Export] Curve seawindHumidityReductionCurve;
		[Export] Curve elevationHumidityAdditionCurve;
		[Export] Curve humidityCurve;
		[Export] Curve temperaturePolarEquatorCurve;
		[Export] Curve elevationTemperatureReductionCurve;
		[Export] float temperatureNoiseCoef;
		[Export] Curve populationLandTileCurve;
		[Export] Godot.Collections.Array<ResourceSiteGenerationParameters> resourceSiteGenerationParameters;

		public Region[] Regions;

		RandomNumberGenerator rng;

		public bool Generating { get; private set; }


		public override void _Ready() {
			rng = new();
		}

		public void GenerateContinents(World world) {
			Generating = true;

			continentNoise.Seed = (int)rng.Randi();
			temperatureNoise.Seed = (int)rng.Randi();
			humidityNoise.Seed = (int)rng.Randi();
			seawindGainNoise.Seed = (int)rng.Randi();

			var centre = new Vector2(world.Longitude / 2, world.Latitude / 2);
			var divBySidelen = 1.0f / (float)(Math.Pow(world.Longitude / 2.0, 2) + Math.Pow(world.Latitude / 2.0, 2));
			for (int x = 0; x < world.Longitude; x++) {
				for (int y = 0; y < world.Latitude; y++) {
					var vec = new Vector2(x, y);

					float continentSample = continentNoise.GetNoise2D(x, y);

					float distanceSqFromCentre = centre.DistanceSquaredTo(vec) * divBySidelen;
					continentSample -= islandCurve.SampleBaked(distanceSqFromCentre);
					continentSample = Mathf.Clamp(continentSample, -1f, 1f);

					world.SetElevation(x, y, continentSample);
				}
			}
			// creating initial seawind
			for (int xinc = 0; xinc < world.Longitude; xinc++) {
				for (int yinc = 0; yinc < world.Latitude; yinc++) {
					int x = xinc;
					int y = yinc;
					if (world.SeaWindDirection.X == -1) x = world.Longitude - xinc - 1;
					if (world.SeaWindDirection.Y == -1) y = world.Latitude - yinc - 1;

					float continentSample = world.GetElevation(x, y);
					float previousContinentSample = world.GetElevation(x - world.SeaWindDirection.X, y - world.SeaWindDirection.Y);
					float aboveSeaSample = Mathf.Clamp(continentSample, 0f, 1f);
					float previousAboveSeaSample = Mathf.Clamp(previousContinentSample, 0f, 1f);

					float previousSeawindSample = world.GetSeaWind(x - world.SeaWindDirection.X, y - world.SeaWindDirection.Y);
					float seawindGain = Math.Abs(seawindGainNoise.GetNoise2D(x, y)) * 0.03f;
					float seawind = (previousSeawindSample - seawindElevationDifferenceReductionCurve.SampleBaked(aboveSeaSample - previousAboveSeaSample)) + seawindGain;
					seawind = Mathf.Clamp(seawind, 0f, 1f);
					world.SetSeaWind(x, y, seawind);
				}
			}
			// blurring the seawind
			float kernelSum = kernel.Sum(k => k.Coef);
			for (int x = 0; x < world.Longitude; x++) {
				for (int y = 0; y < world.Latitude; y++) {
					float val = 0;
					foreach (var p in kernel) {
						val += world.GetSeaWind(x + p.X, y + p.Y) * p.Coef;
					}
					val /= kernelSum;
					world.SetSeaWind(x, y, val);
				}
			}
			for (int xinc = 0; xinc < world.Longitude; xinc++) {
				for (int yinc = 0; yinc < world.Latitude; yinc++) {
					int x = xinc;
					int y = yinc;
					if (world.SeaWindDirection.X == -1) x = world.Longitude - xinc - 1;
					if (world.SeaWindDirection.Y == -1) y = world.Latitude - yinc - 1;

					float seawind = world.GetSeaWind(x, y);
					float continentSample = world.GetElevation(x, y);

					float distanceFromEquator = Math.Abs(y - world.Latitude * 0.5f) / (world.Latitude * 0.5f);
					float temperatureSample = Mathf.Lerp(
						temperatureNoise.GetNoise2D(x, y),
						temperaturePolarEquatorCurve.SampleBaked(distanceFromEquator),
						seawindTemperatureModCurve.SampleBaked(seawind)
					);
					temperatureSample += rng.Randf() * temperatureNoiseCoef; // randomizing temp a bit
					temperatureSample -= elevationTemperatureReductionCurve.SampleBaked(continentSample);
					temperatureSample = Mathf.Clamp(temperatureSample, -1f, 1f);
					world.SetTemperature(x, y, temperatureSample);

					float humiditySample = humidityCurve.SampleBaked(humidityNoise.GetNoise2D(x, y));
					humiditySample += elevationHumidityAdditionCurve.SampleBaked(continentSample);
					humiditySample -= seawindHumidityReductionCurve.SampleBaked(seawind);
					humiditySample = Mathf.Clamp(humiditySample, 0.0f, 1.0f);
					world.SetHumidity(x, y, humiditySample);
				}
			}

			for (int x = 0; x < world.Longitude; x++) {
				for (int y = 0; y < world.Latitude; y++) {
					var ele = world.GetElevation(x, y);
					var humi = world.GetHumidity(x, y);
					var temp = world.GetTemperature(x, y);
					var tile = GroundTileType.Sea;
					if (ele >= 0) {
						tile = GroundTileType.HasLand;
						if (ele <= 0.012f) tile |= GroundTileType.HasSand;
						else if (humi > 0.15f) {
							if (temp < 0) tile |= GroundTileType.HasSnow;
							else tile |= GroundTileType.HasVeg;
						} else tile |= GroundTileType.HasSand;
					}
					world.SetTile(x, y, tile);
				}
			}
			Generating = false;
		}

		public async Task<Map> GenerateRegions(World world) {
			return await GenerateRegions(world, LandRegionCount);
		}

		public async Task<Map> GenerateRegions(World world, int regionCountLand) {
			Generating = true;
			Dictionary<Vector2I, Region> landOccupied; // coordinates in global space
			landOccupied = GenerateRegionStarts(world, regionCountLand);

			Region[] regionsLand = landOccupied.Values.ToArray();

			Dictionary<Region, List<(Vector2I, byte)>> freeEdgeTiles = new();

			foreach (var reg in regionsLand) {
				freeEdgeTiles[reg] = new() { (Vector2I.Zero, 0b1111) };
			}

			GD.Print("WorldGenerator::GenerateRegions : Growing regions");
			await GrowRegions(world, regionsLand, landOccupied, freeEdgeTiles);

			freeEdgeTiles.Clear();

			foreach (Region region in regionsLand) {
				foreach (Vector2I pos in region.GroundTiles.Keys) {
					var wpos = pos + region.WorldPosition;
					// starter house position..
					if (pos == Vector2I.Zero) continue;
					var ele = world.GetElevation(wpos.X, wpos.Y);
					var humi = world.GetHumidity(wpos.X, wpos.Y);
					var temp = world.GetTemperature(wpos.X, wpos.Y);
					ResourceSiteGenerationParameters siteType = null;
					//foreach (var genpara in resourceSiteGenerationParameters) {
					//	if (ele < genpara.MinElevation || ele > genpara.MaxElevation) continue;
					//	if (humi < genpara.MinHumidity || humi > genpara.MaxHumidity) continue;
					//	if (temp < genpara.MinTemperature || temp > genpara.MaxTemperature) continue;
					//	var eles =  ResourceSiteGenerationParameters.ParamDistance(genpara.MinElevation, genpara.MaxElevation, ele);
					//	var humis = ResourceSiteGenerationParameters.ParamDistance(genpara.MinHumidity, genpara.MaxHumidity, humi);
					//	var temps = ResourceSiteGenerationParameters.ParamDistance(genpara.MinTemperature, genpara.MaxTemperature, temp);
					//	if (eles + humis + temps < distance) {
					//		distance = eles + humis + temps;
					//		siteType = genpara;
					//	}
					//}
					siteType = resourceSiteGenerationParameters[rng.RandiRange(0, resourceSiteGenerationParameters.Count - 1)];
					if (siteType == null) continue;
					if (ele < siteType.MinElevation || ele > siteType.MaxElevation) continue;
					if (humi < siteType.MinHumidity || humi > siteType.MaxHumidity) continue;
					if (temp < siteType.MinTemperature || temp > siteType.MaxTemperature) continue;
					var elesfinal = Mathf.Clamp(1f - ResourceSiteGenerationParameters.ParamDistance(siteType.MinElevation, siteType.MaxElevation, ele), 0f, 1f);
					var humisfinal = Mathf.Clamp(1f - ResourceSiteGenerationParameters.ParamDistance(siteType.MinHumidity, siteType.MaxHumidity, humi), 0f, 1f);
					var tempsfinal = Mathf.Clamp(1f - ResourceSiteGenerationParameters.ParamDistance(siteType.MinTemperature, siteType.MaxTemperature, temp), 0f, 1f);
					if (rng.Randf() > elesfinal * humisfinal * tempsfinal * siteType.Rarity) continue;
					region.CreateResourceSiteAndPlace(siteType.Target, pos);
				}
			}

			await War(regionsLand, AggressiveFactionCount);

			Map map = new(regionsLand, world);

			Generating = false;
			return map;
		}

		public Dictionary<Vector2I, Region> GenerateRegionStarts(World world, int regionCount) {
			var startPoses = new Dictionary<Vector2I, Region>();
			int regionsMade = 0;

			while (regionsMade < regionCount) {
				var tile = new Vector2I(rng.RandiRange(0, world.Longitude - 1), rng.RandiRange(0, world.Latitude - 1));
				while (
					(world.GetTile(tile.X, tile.Y) & GroundTileType.HasVeg) == 0
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
				var grew = GrowAllRegionsOneStep(regions, occupied, freeEdgeTiles, world, sea: sea, iterations: 128);
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

			for (int xxx = 0; xxx < iterations; xxx++) for (int i = 0; i < regions.Length; i++) {
					var region = regions[i];
					var freeEdges = freeEdgeTiles[region];
					var c = freeEdges.Count;
					for (int x = 0; x < c; x++) {
						var addKeys = new HashSet<Vector2I>(); // coordinates in region local space
						addKeys.Clear();
						for (int dirIx = 0; dirIx < 4; dirIx++) {
							var ix = rng.RandiRange(0, freeEdges.Count - 1);
							growthOccurred = GrowRegionInDirection(occupied, addKeys, freeEdges, ix, region, dirIx, world, GroundTileType.All) || growthOccurred;
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
				GD.Print("WorldGenerator::GrowAllRegionsOneStep : region growth filled up all space attainable");
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
			//var tileAt = world.GetTile(where.X, where.Y);
			if (there == null && /*tileAt == allowedTile && */ !addKeys.Contains(local) && where.X >= 0 && where.X < world.Longitude && where.Y >= 0 && where.Y < world.Latitude) {
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

		async Task War(Region[] regions, int aggressiveRegionCount) {
			// all regions get initial populations
			foreach (Region region in regions) {
				int initPop = (int)populationLandTileCurve.SampleBaked(region.LandTileCount);
				int sustainsForOneMonth = (int)(region.GetPotentialFood() * 1.75f / (GameTime.WEEKS_PER_MONTH * GameTime.DAYS_PER_WEEK));
				initPop = Math.Min(initPop, sustainsForOneMonth);

				var faction = new Faction(
					region,
					initialPopulation: (uint)initPop
				);
			}

			var aggressors = regions.Where(r => r.LocalFaction.GetPopulationCount() > 20).OrderByDescending(r => r.LocalFaction.GetPopulationCount()).Take(aggressiveRegionCount);
			GD.Print($"WorldGenerator::War : aggressors {string.Join(", ", aggressors)}");
			var subs = new Dictionary<Region, HashSet<Region>>();
			var taken = new HashSet<Region>();
			foreach (var ag in aggressors) subs[ag] = new();

			int actions = 10;
			while (actions-- > 0) {
				foreach (var ag in aggressors) {
					uint attackingPop = ag.LocalFaction.GetPopulationCount() + (uint)subs[ag].Select(s => (float)s.LocalFaction.GetPopulationCount()).Sum();
					foreach (var sub in subs[ag]) {
						WarlikeExpand(subs[ag].ToList().ToHashSet(), taken, sub, attackingPop, ag);
					}
					WarlikeExpand(subs[ag], taken, ag, attackingPop, ag);
				}
				await ToSignal(GetTree(), "process_frame");
			}
		}

		void WarlikeExpand(HashSet<Region> subs, HashSet<Region> taken, Region attacker, uint attackingPop, Region owner) {
			foreach (var n in attacker.Neighbors) {
				if (subs.Contains(n)) continue;
				if (taken.Contains(n)) continue;
				if (n.LocalFaction.GetPopulationCount() > attackingPop / 2) {

				} else {
					subs.Add(n);
					taken.Add(n);

					owner.LocalFaction.MakeFactionSubservient(n.LocalFaction);
				}
			}
		}

	}

}
