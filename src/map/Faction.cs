using System.Collections.Generic;

public partial class Faction {

	//readonly List<Region> ownedRegions; public List<Region> OwnedRegions { get => ownedRegions; }
	readonly List<RegionFaction> ownedFactions = new();


	public RegionFaction CreateOwnedFaction(Region region) {
		var fac = new RegionFaction(region, this);
		ownedFactions.Add(fac);
		return fac;
	}

	public RegionFaction GetOwnedRegionFaction(int ix) {
		return ownedFactions[ix];
	}

	public void PassTime(TimeT minutes) {
		// RegionFaction time passes in Map.PassTime
	}

}
