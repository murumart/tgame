using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Map {

	public readonly World World;
	protected readonly List<Region> regions;

	public Dictionary<Vector2I, Region> TileOwners { get; init; }


	public Map(ICollection<Region> regions, World world) {
		this.regions = regions.ToList();
		World = world;

		TileOwners = new();
		foreach (Region region in regions) {
			foreach (Vector2I pos in region.GroundTiles.Keys) {
				var wpos = pos + region.WorldPosition;
				Debug.Assert(!TileOwners.ContainsKey(wpos), $"The tile {wpos} is contested and wrong please behave");
				TileOwners[wpos] = region;
			}
		}
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
		for (int i = 0; i < 10; i++) {
			var region = Region.GetTestCircleRegion(i, 12, new(i * 50, 0));
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

		return new Map(regions, null);
	}

}


