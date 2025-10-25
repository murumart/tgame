using System;
using System.Collections.Generic;
using Godot;
using Jobs;
using static Building;
using static ResourceType;

public partial class Faction : ITimePassing {

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

	public void PassTime(float hours) {
		// RegionFaction time passes in Map.PassTime
	}
}

public partial class Faction {

	public class RegionFaction : ITimePassing {

		readonly Faction faction; public Faction Faction { get => faction; }
		readonly Region region; public Region Region { get => region; }

		readonly HashSet<Vector2I> ownedTiles = new();

		readonly Dictionary<Vector2I, Building> buildings = new();
		readonly List<Job> jobs = new();
		readonly Dictionary<Vector2I, HashSet<Job>> jobsByPosition = new();

		readonly ResourceStorage resourceStorage = new();

		Population homelessPopulation; public ref Population HomelessPopulation { get => ref homelessPopulation; }


		public RegionFaction(Region region, Faction faction) {
			this.region = region;
			this.faction = faction;
			homelessPopulation = new Population(100);
			homelessPopulation.Pop = 10;
			// ASSUMING these are wood rock and ... a third thing initially...
			resourceStorage.IncreaseCapacity(ResourceRegistry.GetResourceType(0), 30);
			resourceStorage.IncreaseCapacity(ResourceRegistry.GetResourceType(1), 30);
			resourceStorage.IncreaseCapacity(ResourceRegistry.GetResourceType(2), 30);
		}

		public void PassTime(float hours) {
			foreach (var job in jobs) {
				job.PassTime(hours);
			}
			// building time is passed in Region
		}

		public int GetPopulationCount() {
			int count = homelessPopulation.Pop;
			foreach (var b in buildings.Values) {
				count += b.Population.Pop;
			}
			return count;
		}

		public void AddJob(Vector2I pos, Job job) {
			Debug.Assert(!(jobsByPosition.ContainsKey(pos) && jobsByPosition[pos].Contains(job)), $"Job at place {pos} exists ({job})");
			if (!jobsByPosition.ContainsKey(pos)) jobsByPosition[pos] = new();
			jobsByPosition[pos].Add(job);
			AddJob(job);
		}

		public void AddJob(Job job) {
			jobs.Add(job);
		}

		public Building PlaceBuilding(IBuildingType type, Vector2I position) {
			var building = region.CreateBuildingSpotAndPlace(type, position);
			if (type.GetPopulationCapacity() > 0) AddJob(position, new AbsorbFromHomelessPopulationJob(building, this));
			return building;
		}

		public bool CanPlaceBuilding(IBuildingType type, Vector2I tilepos) {
			return region.CanPlaceBuilding(tilepos);
		}
	}
}