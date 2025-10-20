using System.Collections.Generic;

public class Map : ITimePassing {
	readonly List<Region> regions = new();

	public Map() {
		// debug
		for (int i = 0; i < 10; i++) {
			regions.Add(Region.GetTestCircleRegion(12));
		}
	}

	public Region GetRegion(int ix) {
		return regions[ix];
	}

	public void PassTime(float hours) {
		foreach (Region region in regions) {
			region.PassTime(hours);
		}
	}
}
