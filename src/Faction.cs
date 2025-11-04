using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static Building;

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

	public void PassTime(TimeT minutes) {
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

		readonly ResourceStorage resourceStorage = new(); public ResourceStorage Resources { get => resourceStorage; }

		Population homelessPopulation; public ref Population HomelessPopulation => ref homelessPopulation;
		Population unemployedPopulation; public ref Population UnemployedPopulation => ref unemployedPopulation;


		public RegionFaction(Region region, Faction faction) {
			this.region = region;
			this.faction = faction;
			homelessPopulation = new(100) { Pop = 10 };
			unemployedPopulation = new(100) { Pop = 10 };

			// ASSUMING these are wood rock and ... a third thing initially... TODO make sensical
			resourceStorage.IncreaseCapacity(Registry.Resources.GetAsset(0), 30);
			resourceStorage.IncreaseCapacity(Registry.Resources.GetAsset(1), 30);
			resourceStorage.IncreaseCapacity(Registry.Resources.GetAsset(2), 30);
			resourceStorage.AddResource(new(Registry.Resources.GetAsset(0), 4));
			resourceStorage.AddResource(new(Registry.Resources.GetAsset(1), 25));
			resourceStorage.AddResource(new(Registry.Resources.GetAsset(2), 25));

			var housing = Registry.Buildings.GetAsset(0);
			var starterHouse = PlacePrebuiltBuilding(housing, new(0, 0));
			starterHouse.ProgressBuild((int)(housing.GetHoursToConstruct() * 60));
		}

		public void PassTime(TimeT minutes) {
			foreach (var job in jobs) {
				job.PassTime(minutes);
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

		public void AddJob(Vector2I position, Job job) {
			Debug.Assert(!(jobsByPosition.ContainsKey(position) && jobsByPosition[position].Contains(job)), $"Job at place {position} exists ({job})");
			if (!jobsByPosition.ContainsKey(position)) jobsByPosition[position] = new();
			jobsByPosition[position].Add(job);
			AddJobWithoutPosition(job);
		}

		void AddJobWithoutPosition(Job job) {
			jobs.Add(job);
		}

		public IEnumerable<Job> GetJobs(Vector2I position) {
			jobsByPosition.TryGetValue(position, out HashSet<Job> gottenJobs);
			return gottenJobs.Where<Job>((j) => !j.Internal);
		}

		public int GetFreeWorkers() => unemployedPopulation.Pop;

		public bool CanEmployWorkers(Job job, int amount) {
			Debug.Assert(jobs.Contains(job), "This isn't my job...");
			return UnemployedPopulation.CanTransfer(ref job.GetWorkers(), amount);
		}

		public void EmployWorkers(Job job, int amount) {
			Debug.Assert(jobs.Contains(job), "This isn't my job...");
			Debug.Assert(CanEmployWorkers(job, amount), "Can't employ these workers!");

			UnemployedPopulation.Transfer(ref job.GetWorkers(), amount);
		}

		public Building PlaceBuilding(IBuildingType type, Vector2I position) {
			var building = region.CreateBuildingSpotAndPlace(type, position);
			if (type.TakesTimeToConstruct() || type.HasResourceRequirements()) {
				var job = new ConstructBuildingJob(type.GetResourceRequirements().ToList(), building);
				Debug.Assert(job.CanCreateJob(this), "Job cannot be created!");
				job.Initialise(this);
				AddJob(position, job);
				building.ConstructionJob = job;
			}
			if (type.GetPopulationCapacity() > 0) AddJob(position, new AbsorbFromHomelessPopulationJob(building, this));
			Debug.Assert(!buildings.ContainsKey(position), "There's a lreayd a building here");
			buildings[position] = building;
			return building;
		}

		public Building PlacePrebuiltBuilding(IBuildingType type, Vector2I position) {
			var building = region.CreateBuildingSpotAndPlace(type, position);
			if (type.GetPopulationCapacity() > 0) AddJob(position, new AbsorbFromHomelessPopulationJob(building, this));
			Debug.Assert(!buildings.ContainsKey(position), "There's a lreayd a building here");
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

	}

}