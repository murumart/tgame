using System.Collections.Generic;

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
			// test mandate
			var mandate = faction.Briefcase.CreateExportMandate(
				new() { new(Registry.Resources.GetAsset("logs"), 9) },
				new(),
				faction,
				regionFaction,
				60 * 7 + 120 //GameTime.DAYS_PER_WEEK * GameTime.HOURS_PER_DAY * GameTime.MINUTES_PER_HOUR
			);
			regionFaction.Briefcase.AddDocument(mandate);
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


