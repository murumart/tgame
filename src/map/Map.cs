using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Map {

	public readonly World World;
	protected readonly List<Region> regions;

	public readonly int TotalSilver;
	public readonly int TotalLandTiles;
	public readonly int TotalSeaTiles;

	public Dictionary<Vector2I, Region> TileOwners { get; init; }
	

	public Map(ICollection<Region> regions, World world) {
		this.regions = regions.ToList();
		World = world;

		TileOwners = new();
		int totalSilver = 0;
		int totalLand = 0;
		int totalSea = 0;
		foreach (Region region in regions) {
			foreach (var (pos, tile) in region.GetGroundTiles()) {
				var wpos = pos + region.WorldPosition;
				Debug.Assert(!TileOwners.ContainsKey(wpos), $"The tile {wpos} is contested and wrong please behave");
				TileOwners[wpos] = region;
				if ((tile & GroundTileType.HasLand) == 0) totalSea++;
				else totalLand++;
			}
			totalSilver += region.LocalFaction.Silver;
		}
		TotalSilver = totalSilver;
		TotalLandTiles = totalLand;
		TotalSeaTiles = totalSea;
		GD.Print($"Map::Map : TotalSilver = {TotalSilver}, TotalLandTiles = {TotalLandTiles}, TotalSeaTiles = {TotalSeaTiles}");
	}

	public Region GetRegion(int ix) {
		return regions[ix];
	}

	public Region[] GetRegions() => regions.ToArray();

	public void PassTime(TimeT minutes) {
		foreach (Region region in regions) {
			region.PassTime(minutes);
		}
	}

	public static Map GetDebugMap() {

		List<Region> regions = new();
		List<Faction> regionFactions = new();
		for (int i = 10; i < 20; i++) {
			var region = Region.GetTestCircleRegion(i - 10, 12, new(i * 18, i * 15));
			regions.Add(region);
			var regionFaction = new Faction(region);
			regionFactions.Add(regionFaction);
			// test mandate
			//var mandate = faction.Briefcase.CreateExportMandate(
			//	new() { new(Registry.Resources.GetAsset("logs"), 9) },
			//	new(),
			//	faction,
			//	regionFaction,
			//	60 * 7 + 120 //GameTime.DAYS_PER_WEEK * GameTime.HOURS_PER_DAY * GameTime.MINUTES_PER_HOUR
			//);
			//regionFaction.Briefcase.AddDocument(mandate);
		}
		foreach (var region in regions) {
			foreach (var otherregion in regions) {
				if (region == otherregion) continue;
				region.AddNeighbor(otherregion);
				region.LocalFaction.AddTradePartner(otherregion.LocalFaction);
			}
		}
		var world = new World(1000, 1000, 1);
		for (int x = 0; x < world.Width; x++) for (int y = 0; y < world.Height; y++) {
				world.SetElevation(x, y, Mathf.Clamp(Mathf.Sin(x * 0.04f) * Mathf.Cos(y * 0.04f), -1f, 1f));
				world.SetTile(x, y, GroundTileType.HasLand | GroundTileType.HasVeg);
			}
		return new Map(regions, world);
	}

}


