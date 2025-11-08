using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static Building;

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

public partial class Faction {

	public class RegionFaction {

		readonly Faction faction; public Faction Faction { get => faction; }
		readonly Region region; public Region Region { get => region; }

		readonly HashSet<Vector2I> ownedTiles = new();

		readonly Dictionary<Vector2I, Building> buildings = new();
		readonly List<Job> jobs = new();
		readonly Dictionary<Vector2I, HashSet<Job>> jobsByPosition = new();

		readonly ResourceStorage resourceStorage = new(); public ResourceStorage Resources { get => resourceStorage; }

		Population homelessPopulation; public ref Population HomelessPopulation => ref homelessPopulation;
		Population unemployedPopulation; public ref Population UnemployedPopulation => ref unemployedPopulation;


		public RegionFaction(Region region, Faction faction) {
			this.region = region;
			this.faction = faction;
			homelessPopulation = new(100) { Amount = 10 };
			unemployedPopulation = new(100) { Amount = 10 };

			// ASSUMING these are wood rock and ... a third thing initially... TODO make sensical
			resourceStorage.IncreaseCapacity(Registry.Resources.GetAsset("rock"), 30);
			resourceStorage.IncreaseCapacity(Registry.Resources.GetAsset("wood"), 30);
			resourceStorage.IncreaseCapacity(Registry.Resources.GetAsset("fish"), 30);
			resourceStorage.AddResource(new(Registry.Resources.GetAsset("rock"), 4));
			resourceStorage.AddResource(new(Registry.Resources.GetAsset("wood"), 25));
			resourceStorage.AddResource(new(Registry.Resources.GetAsset("fish"), 25));

			var housing = Registry.Buildings.GetAsset("housing");
			PlacePrebuiltBuilding(housing, new(0, 0));
		}

		public void PassTime(TimeT minutes) {
			for (int i = jobs.Count - 1; i >= 0; i--) {
				var job = jobs[i];
				job.PassTime(minutes);
				job.CheckDone(this);
			}
			// building time is passed in Region
		}

		public int GetPopulationCount() {
			int count = homelessPopulation.Amount;
			foreach (var b in buildings.Values) {
				count += b.Population.Amount;
			}
			return count;
		}

		public void AddJob(Vector2I position, Job job) {
			Debug.Assert(job is not JobBox, "Debox the job before adding it! Can't add boxed job");
			Debug.Assert(!(jobsByPosition.ContainsKey(position) && jobsByPosition[position].Contains(job)), $"Job object ({job}) at {position} exists ");
			if (!jobsByPosition.ContainsKey(position)) jobsByPosition[position] = new();
			jobsByPosition[position].Add(job);
			AddJobWithoutPosition(job);

			Debug.Assert(job.CanInitialise(this), "Job cannot be initialised!");
			job.Initialise(this);
		}

		void AddJobWithoutPosition(Job job) {
			jobs.Add(job);
		}

		public void RemoveJob(Vector2I position, Job job) {
			Debug.Assert(jobsByPosition.ContainsKey(position) && jobsByPosition[position].Contains(job), $"Can't remove job ({job}) that doesn't exist here ({position})?? Hello?");

			UnemployWorkers(job);
			job.Deinitialise(this);
			jobsByPosition[position].Remove(job);
			jobs.Remove(job);
		}

		public IEnumerable<Job> GetJobs(Vector2I position) {
			jobsByPosition.TryGetValue(position, out HashSet<Job> gottenJobs);
			return gottenJobs.Where<Job>((j) => !j.IsInternal);
		}

		public int GetFreeWorkers() => unemployedPopulation.Amount;

		public bool CanEmployWorkers(Job job, int amount) {
			Debug.Assert(jobs.Contains(job), "This isn't my job...");
			return UnemployedPopulation.CanTransfer(ref job.Workers, amount);
		}

		public void EmployWorkers(Job job, int amount) {
			Debug.Assert(jobs.Contains(job), "This isn't my job...");
			Debug.Assert(CanEmployWorkers(job, amount), "Can't employ these workers!");

			UnemployedPopulation.Transfer(ref job.Workers, amount);
		}

		public void UnemployWorkers(Job job) {
			Debug.Assert(jobs.Contains(job), "This isn't my job...");
			Debug.Assert(CanEmployWorkers(job, -job.Workers.Amount), "Can't unemploy these workers!");

			UnemployedPopulation.Transfer(ref job.Workers, -job.Workers.Amount);
		}

		public Building PlaceBuildingConstructionSite(IBuildingType type, Vector2I position) {
			Debug.Assert(!buildings.ContainsKey(position), "There's a lreayd a building here (known by faction)");
			Debug.Assert(region.CanPlaceBuilding(position), "There's a lreayd a building here (known by region)");
			Debug.Assert(CanPlaceBuilding(type, position), "Cannot place the building for whatever reason");
			var building = PlaceBuilding(type, position);
			if (type.TakesTimeToConstruct() || type.HasResourceRequirements()) {
				var job = new ConstructBuildingJob(type.GetResourceRequirements().ToList(), building);
				AddJob(position, job);
				building.ConstructionJob = job;
			}
			return building;
		}

		Building PlacePrebuiltBuilding(IBuildingType type, Vector2I position) {
			Debug.Assert(!buildings.ContainsKey(position), "There's a lreayd a building here");
			var building = PlaceBuilding(type, position);
			building.ProgressBuild((int)(type.GetHoursToConstruct() * 60), new AnonBuilderJob());
			return building;
		}

		Building PlaceBuilding(IBuildingType type, Vector2I position) {
			var building = region.CreateBuildingSpotAndPlace(type, position);
			if (type.GetPopulationCapacity() > 0) AddJob(position, new AbsorbFromHomelessPopulationJob(building));
			buildings[position] = building;
			return building;
		}

		public bool CanPlaceBuilding(IBuildingType type, Vector2I tilepos) {
			return CanBuild(type) && region.CanPlaceBuilding(tilepos);
		}

		public bool CanBuild(IBuildingType type) {
			return resourceStorage.HasEnoughAll(type.GetResourceRequirements());
		}

		public ICollection<Building> GetBuildings() => buildings.Values;

		public bool HasBuilding(Vector2I at) => buildings.ContainsKey(at);

		public Building GetBuilding(Vector2I at) => buildings.GetValueOrDefault(at, null);

		private class AnonBuilderJob : IConstructBuildingJob { public float GetProgressPerMinute() => 1f; }

	}

}