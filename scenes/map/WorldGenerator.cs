using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using resources.game;
using scenes.ui;

namespace scenes.map {

	public partial class WorldGenerator : Node {

		

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
		[Export] FastNoiseLite drainageNoise;
		[Export] FastNoiseLite seawindGainNoise;

		readonly struct NoiseHomeParams(FastNoiseLite noise, float frequency, float frac) {
			public readonly FastNoiseLite Noise => noise;
			public readonly float Frequency => frequency;
			public readonly float FractalLacunarity => frac;
		}
		NoiseHomeParams[] noises;

		[Export] public int WorldWidth;
		[Export] public int WorldHeight;
		[Export] int LandRegionCount;
		[Export] int AggressiveFactionCount;
		[Export] Curve elevationCurve;
		[Export] Curve islandCurve;
		[Export] Curve seawindElevationDifferenceReductionCurve;
		[Export] Curve seawindTemperatureModCurve;
		[Export] Curve seawindHumidityReductionCurve;
		[Export] Curve elevationHumidityAdditionCurve;
		[Export] Curve humidityCurve;
		[Export] Curve drainageCurve;
		[Export] Curve temperaturePolarEquatorCurve;
		[Export] Curve elevationTemperatureReductionCurve;
		[Export] float temperatureNoiseCoef;
		[Export] Curve populationLandTileCurve;
		[Export] ResourceSiteGenerationParametersCollection resourceSiteGenerationParameters;


		public Region[] Regions;

		RandomNumberGenerator rng;

		public bool Generating { get; private set; }


		public override void _Ready() {
			noises = [
				new(continentNoise, continentNoise.Frequency, continentNoise.FractalLacunarity),
				new(temperatureNoise, temperatureNoise.Frequency, temperatureNoise.FractalLacunarity),
				new(humidityNoise, humidityNoise.Frequency, humidityNoise.FractalLacunarity),
				new(seawindGainNoise, seawindGainNoise.Frequency, seawindGainNoise.FractalLacunarity),
			];
		}

		public async Task GenerateContinents(World world, float noiseScale = 1f, float baseDepth = 0f) {
			rng = new() {
				Seed = world.Seed
			};

			foreach (var noise in noises) {
				noise.Noise.Frequency = noise.Frequency * noiseScale;
				noise.Noise.FractalLacunarity = noise.FractalLacunarity / Mathf.Pow(noiseScale, 0.25f);
			}

			Generating = true;

			continentNoise.Seed = (int)rng.Randi();
			temperatureNoise.Seed = (int)rng.Randi();
			humidityNoise.Seed = (int)rng.Randi();
			drainageNoise.Seed = (int)rng.Randi();
			seawindGainNoise.Seed = (int)rng.Randi();

			var centre = new Vector2(world.Width / 2, world.Height / 2);
			var wsize = new Vector2(world.Width, world.Height);
			int longerAxis = (int)(world.Width > world.Height ? Vector2.Axis.X : Vector2.Axis.Y);
			int shorterAxis = (int)(world.Width > world.Height ? Vector2.Axis.X : Vector2.Axis.Y);
			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					// pretend that the world is circle shaped
					var vec = new Vector2(x, y);
					vec /= wsize;

					float distanceSqFromCentre = (Vector2.One * 0.5f).DistanceSquaredTo(vec) * 1.7f;

					float continentSample = continentNoise.GetNoise2D(x, y);
					continentSample = Mathf.Clamp(
						baseDepth
						+ elevationCurve.SampleBaked(continentSample)
						- islandCurve.SampleBaked(distanceSqFromCentre),
					-1f, 1f);

					world.SetElevation(x, y, continentSample);
				}
			}
			await Task.Delay(1);
			// creating initial seawind
			for (int xinc = 0; xinc < world.Width; xinc++) {
				for (int yinc = 0; yinc < world.Height; yinc++) {
					int x = xinc;
					int y = yinc;
					if (world.SeaWindDirection.X == -1) x = world.Width - xinc - 1;
					if (world.SeaWindDirection.Y == -1) y = world.Height - yinc - 1;

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
			await Task.Delay(1);
			// blurring the seawind
			float kernelSum = kernel.Sum(k => k.Coef);
			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					float val = 0;
					foreach (var p in kernel) {
						val += world.GetSeaWind(x + p.X, y + p.Y) * p.Coef;
					}
					val /= kernelSum;
					world.SetSeaWind(x, y, val);
				}
			}
			for (int xinc = 0; xinc < world.Width; xinc++) {
				for (int yinc = 0; yinc < world.Height; yinc++) {
					int x = xinc;
					int y = yinc;
					if (world.SeaWindDirection.X == -1) x = world.Width - xinc - 1;
					if (world.SeaWindDirection.Y == -1) y = world.Height - yinc - 1;

					float seawind = world.GetSeaWind(x, y);
					float continentSample = world.GetElevation(x, y);

					float distanceFromEquator = Math.Abs(y - world.Height * 0.5f) / (world.Height * 0.5f);
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

					float drainageSample = drainageCurve.SampleBaked(drainageNoise.GetNoise2D(x, y));
					drainageSample = Mathf.Clamp(drainageSample + continentSample * 0.01f, 0f, 1f);
					world.SetDrainage(x, y, drainageSample);
				}
			}
			await Task.Delay(1);

			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					var ele = world.GetElevation(x, y);
					var humi = world.GetHumidity(x, y);
					var drain = world.GetDrainage(x, y);
					var temp = world.GetTemperature(x, y);
					var tile = GroundTileType.Sea;
					if (ele >= 0) {
						tile = GroundTileType.HasLand;
						if (ele <= 0.012f || humi < 0.02f || temp > 0.9f || drain > 0.95f) tile |= GroundTileType.HasSand;
						else if (humi > 0.15f) {
							if (temp < 0) tile |= GroundTileType.HasSnow;
							else {
								tile |= GroundTileType.HasVeg;
								if (temp > 0.55f && humi < 0.45f || drain > 0.8f) tile |= GroundTileType.HasSand;
							}

							if (humi > 0.45f && drain < 0.2f) {
								tile &= (~GroundTileType.HasLand);
							}
						}
					}
					if (humi > 0.45 && rng.Randf() < 0.02 * humi && temp > 0f) tile |= GroundTileType.HasVeg;
					world.SetTile(x, y, tile);
				}
			}
			await Task.Delay(1);
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

			await GrowRegions(world, regionsLand, landOccupied, freeEdgeTiles);

			freeEdgeTiles.Clear();

			foreach (Region region in regionsLand) {
				foreach (Vector2I pos in region.GroundTilePositions) {
					var wpos = pos + region.WorldPosition;
					// starter house position..
					if (pos == Vector2I.Zero) continue;
					var ele = world.GetElevation(wpos.X, wpos.Y);
					var humi = world.GetHumidity(wpos.X, wpos.Y);
					var temp = world.GetTemperature(wpos.X, wpos.Y);
					var drain = world.GetDrainage(wpos.X, wpos.Y);
					ResourceSiteGenerationParameters siteType = resourceSiteGenerationParameters[rng.RandiRange(0, resourceSiteGenerationParameters.Count - 1)];
					if (siteType == null) continue;
					Debug.Assert(Registry.ResourceSites.GetAsset((siteType.Target as IAssetType).GetIdString()) is not null, "Resource site type not registred");
					if (ele < siteType.MinElevation || ele > siteType.MaxElevation) continue;
					if (humi < siteType.MinHumidity || humi > siteType.MaxHumidity) continue;
					if (temp < siteType.MinTemperature || temp > siteType.MaxTemperature) continue;
					if (drain < siteType.MinDrainage || drain > siteType.MaxDrainage) continue;
					var elesfinal = Mathf.Clamp(1f - ResourceSiteGenerationParameters.ParamDistance(siteType.MinElevation, siteType.MaxElevation, ele), 0f, 1f);
					var humisfinal = Mathf.Clamp(1f - ResourceSiteGenerationParameters.ParamDistance(siteType.MinHumidity, siteType.MaxHumidity, humi), 0f, 1f);
					var tempsfinal = Mathf.Clamp(1f - ResourceSiteGenerationParameters.ParamDistance(siteType.MinTemperature, siteType.MaxTemperature, temp), 0f, 1f);
					var drainsfinal = Mathf.Clamp(1f - ResourceSiteGenerationParameters.ParamDistance(siteType.MinDrainage, siteType.MaxDrainage, drain), 0f, 1f);
					if (rng.Randf() > elesfinal * humisfinal * tempsfinal * drainsfinal * siteType.Rarity) continue;
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
				var tile = new Vector2I(rng.RandiRange(0, world.Width - 1), rng.RandiRange(0, world.Height - 1));
				while (
					(world.GetTile(tile.X, tile.Y) & GroundTileType.HasVeg) == 0
					|| (world.GetTile(tile.X, tile.Y) & GroundTileType.HasLand) == 0
					|| startPoses.ContainsKey(tile)
				) {
					tile = new Vector2I(rng.RandiRange(0, world.Width - 1), rng.RandiRange(0, world.Height - 1));
				}
				Debug.Assert(!startPoses.ContainsKey(tile), "Two regions can't start on the same tile!");
				var gtiles = new Dictionary<Vector2I, GroundTileType>();
				var region = new Region(regionsMade, tile, gtiles);
				startPoses.Add(tile, region);

				gtiles[Vector2I.Zero] = world.GetTile(tile.X, tile.Y);

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
				var grew = Region.GenerationAccessor.GrowAllRegionsOneStep(regions, occupied, freeEdgeTiles, world, rng, sea: sea, iterations: 1);
				Regions = regions;
				if (!grew) {
					tw.EmitSignal(Tween.SignalName.Finished);
					tw.Stop();
				}
			});

			tw.TweenCallback(growCallback);
			tw.TweenInterval(0.06);

			await ToSignal(tw, Tween.SignalName.Finished);

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

		enum Behavior {
			Occupy,
			Annex,
			Max
		}

		async Task War(Region[] regions, int aggressiveRegionCount) {
			// all regions get initial populations
			foreach (Region region in regions) {
				int initPop = (int)populationLandTileCurve.SampleBaked(region.LandTileCount);
				int sustainsForOneMonth = (int)(region.GetPotentialFoodFirstMonth() / (GameTime.WEEKS_PER_MONTH * GameTime.DAYS_PER_WEEK));
				initPop = Math.Min(initPop, sustainsForOneMonth) * 3;
				// taper it out ... 300 people is much for the game when starting out
				initPop = (int)(Mathf.Ease(initPop / 300f, 0.3f) * 75f);

				var faction = new Faction(
					region,
					initialPopulation: (uint)initPop,
					initialSilver:(uint)(region.LandTileCount * 0.1 + 1)
				);
			}

			foreach (Region region in regions) {
				foreach (var neighbor in region.Neighbors) {
					if (neighbor.LocalFaction.IsWild) continue;
					region.LocalFaction.AddTradePartner(neighbor.LocalFaction);
				}
			}

#if false // won't have time to make this useful for the game or make sense
			var aggressors = regions.Where(r => r.LocalFaction.GetPopulationCount() > 20).OrderByDescending(r => r.LocalFaction.GetPopulationCount()).Take(aggressiveRegionCount);
			var subs = new Dictionary<Region, HashSet<Region>>();
			var taken = new HashSet<Region>();
			foreach (var ag in aggressors) {
				subs[ag] = new();
				taken.Add(ag);
			}

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
#endif // false
		}

		void WarlikeExpand(HashSet<Region> subs, HashSet<Region> taken, Region attacker, uint attackingPop, Region owner) {
			foreach (var n in attacker.Neighbors) {
				if (subs.Contains(n) || taken.Contains(n) || n.LocalFaction.IsWild) continue;
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
