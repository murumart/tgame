using System.Collections.Generic;
using System.Linq;

public class Map : ITimePassing {

	protected readonly List<Region> regions = new();
	protected readonly List<Faction> factions = new();
	protected readonly List<RegionFaction> regionFactions = new();


	public Map(ICollection<Region> regions, ICollection<Faction> factions, ICollection<RegionFaction> regionFactions) {
		this.regions = regions.ToList();
		this.factions = factions.ToList();
		this.regionFactions = regionFactions.ToList();
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

	public static Map GetDebugMap() {

		List<Region> regions = new();
		List<Faction> factions = new();
		List<RegionFaction> regionFactions = new();
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

		return new Map(regions, factions, regionFactions);
	}

}


