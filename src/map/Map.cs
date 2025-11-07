using System.Collections.Generic;
using Godot;
using static Faction;

public class Map : ITimePassing {

	readonly List<Region> regions = new();
	readonly List<Faction> factions = new();
	readonly List<RegionFaction> regionFactions = new();


	public Map() {
		// debug
		for (int i = 0; i < 10; i++) {
			var region = Region.GetTestCircleRegion(12);
			regions.Add(region);
			var faction = new Faction();
			factions.Add(faction);
			var regionFaction = faction.CreateOwnedFaction(region);
			regionFactions.Add(regionFaction);
		}
	}

	public Region GetRegion(int ix) {
		return regions[ix];
	}

	public Faction GetFaction(int ix) {
		return factions[ix];
	}

	public void PassTime(TimeT minutes) {
		foreach (Region region in regions) {
			region.PassTime(minutes);
		}
		foreach (var faction in factions) {
			faction.PassTime(minutes);
		}
		foreach (var regionFaction in regionFactions) {
			regionFaction.PassTime(minutes);
		}
	}

}


