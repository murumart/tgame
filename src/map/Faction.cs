using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using scenes.autoload;
using static Building;
using static Document;

public interface IEntity {

	Briefcase Briefcase { get; }
	string DocName { get; }
	TimeT GetTime();
	void ContractFailure(Document doc, Point fulfillFailure);
	void ContractSuccess(Document doc);

	ResourceStorage Resources { get; }

}

public class Faction : IEntity {

	public event Action<Document> ContractFailedEvent;

	public Region Region { get; init; }

	readonly Dictionary<Vector2I, Building> buildings = new();
	readonly List<Job> jobs = new();
	readonly Dictionary<Vector2I, HashSet<Job>> jobsByPosition = new();

	readonly ResourceStorage resourceStorage = new();
	public ResourceStorage Resources { get => resourceStorage; }

	Population homelessPopulation;
	public ref Population HomelessPopulation => ref homelessPopulation;

	Population unemployedPopulation;
	public ref Population UnemployedPopulation => ref unemployedPopulation;

	public string DocName => ToString();
	public Briefcase Briefcase { get; init; }

	TimeT time;


	public Faction(Region region, int initialPopulation = 10, int maxPop = 100, int storageCapacity = 300) {
		Region = region;
		Briefcase = new();

		region.MapObjectUpdatedAtEvent += OnMapObjectUpdated;

		homelessPopulation = new(maxPop) { Amount = initialPopulation };
		unemployedPopulation = new(maxPop) { Amount = initialPopulation };

		Region.SetLocalFaction(this);

		resourceStorage.IncreaseCapacity(storageCapacity);
		var housing = Registry.Buildings.GetAsset("log_cabin");
		PlacePrebuiltBuilding(housing, new(0, 0));
	}

	// *** MANAGING WORKERS AND JOBS ***

	public int GetPopulationCount() {
		int count = homelessPopulation.Amount;
		foreach (var b in buildings.Values) {
			count += b.Population.Amount;
		}
		return count;
	}

	void RegisterJob(Vector2I position, Job job) {
		Debug.Assert(job is not JobBox, "Debox the job before adding it! Can't add boxed job");
		Debug.Assert(!(jobsByPosition.ContainsKey(position) && jobsByPosition[position].Contains(job)), $"Job object ({job}) at {position} exists ");
		if (!jobsByPosition.ContainsKey(position)) jobsByPosition[position] = new();
		jobsByPosition[position].Add(job);
		AddJobWithoutPosition(job);
	}

	public void AddJob(Vector2I position, Job job) {
		RegisterJob(position, job);

		Debug.Assert(job.CanInitialise(this), "Job cannot be initialised!");
		job.Initialise(this);
	}

	public void AddMapObjectJob(Vector2I position, MapObjectJob job, MapObject mapObject) {
		RegisterJob(position, job);

		Debug.Assert(job.CanInitialise(this, mapObject), "Job cannot be initialised!");
		job.Initialise(this, mapObject);
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
		var had = jobsByPosition.TryGetValue(position, out HashSet<Job> gottenJobs);
		return gottenJobs?.Where<Job>((j) => !j.IsInternal) ?? new List<Job>();
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

	public void DecreasePopulation(int by) {
		Debug.Assert(by > 0, "I need to subtract this value.");
		var reduction = Mathf.Min(homelessPopulation.Amount, by);
		homelessPopulation.Amount -= reduction;
		by -= reduction;

		reduction = Mathf.Min(unemployedPopulation.Amount, by);
		unemployedPopulation.Amount -= reduction;
		by -= reduction;

		foreach (var b in buildings.Values) {
			reduction = Mathf.Min(b.Population.Amount, by);
			b.Population.Amount -= by;
			by -= reduction;
		}
	}

	// *** MANAGING BUILDINGS ***

	public Building PlaceBuildingConstructionSite(IBuildingType type, Vector2I position) {
		Debug.Assert(!buildings.ContainsKey(position), "There's a lreayd a building here (known by faction)");
		Debug.Assert(Region.CanPlaceBuilding(position), "There's a lreayd a building here (known by region)");
		Debug.Assert(CanPlaceBuilding(type, position), "Cannot place the building for whatever reason");
		var building = PlaceBuilding(type, position);
		if (type.TakesTimeToConstruct() || type.HasResourceRequirements()) {
			var job = new ConstructBuildingJob(type.GetResourceRequirements().ToList());
			AddMapObjectJob(position, job, building);
			building.ConstructionJob = job;
		}
		return building;
	}

	// for initialising the world and such
	Building PlacePrebuiltBuilding(IBuildingType type, Vector2I position) {
		Debug.Assert(!buildings.ContainsKey(position), "There's a lreayd a building here");
		var building = PlaceBuilding(type, position);
		building.ProgressBuild((int)(type.GetHoursToConstruct() * 60), new AnonBuilderJob());
		return building;
	}

	Building PlaceBuilding(IBuildingType type, Vector2I position) {
		var building = Region.CreateBuildingSpotAndPlace(type, position);
		if (type.GetPopulationCapacity() > 0) AddJob(position, new AbsorbFromHomelessPopulationJob(building));
		buildings[position] = building;
		return building;
	}

	public bool CanPlaceBuilding(IBuildingType type, Vector2I tilepos) {
		return CanBuild(type) && Region.CanPlaceBuilding(tilepos);
	}

	public bool CanBuild(IBuildingType type) {
		return resourceStorage.HasEnoughAll(type.GetResourceRequirements());
	}

	public void RemoveBuilding(Vector2I at) {
		Debug.Assert(HasBuilding(at), $"There's no building to remove at {at}...");
		buildings.Remove(at);
		Region.RemoveMapObject(at);
	}

	public void Uproot(Vector2I at) {
		bool has = Region.HasMapObject(at, out var obj);
		Debug.Assert(has, $"No map object to uproot at {at}");
		Region.RemoveMapObject(at);
	}

	public ICollection<Building> GetBuildings() => buildings.Values;

	public bool HasBuilding(Vector2I at) => buildings.ContainsKey(at);

	public Building GetBuilding(Vector2I at) => buildings.GetValueOrDefault(at, null);

	private class AnonBuilderJob : ConstructBuildingJob {

		public AnonBuilderJob() : base(null) { }
		public override float GetWorkTime(TimeT minutes) => minutes;

	}

	void OnMapObjectUpdated(Vector2I at) {
		if (!Region.HasMapObject(at) && HasBuilding(at)) {
			buildings.Remove(at);
		}
	}

	// *** TIMING AND CONTRACTS ***

	public TimeT GetTime() => time;

	public void PassTime(TimeT minutes) {
		for (int i = jobs.Count - 1; i >= 0; i--) {
			var job = jobs[i];
			job.PassTime(minutes);
			job.CheckDone(this);
		}
		time += minutes;
		// building time is passed in Region
	}

	public void UpdateBriefcaseTime(TimeT minutes) {
		var prevHour = time / 60;
		var nextHour = (time + minutes) / 60;
		var diff = nextHour - prevHour;

		for (ulong i = 1; i <= diff; i++) {
			Briefcase.Check(prevHour * 60 + i * 60);
		}
	}

	public void ContractFailure(Document doc, Point fulfillFailure) {
		ContractFailedEvent?.Invoke(doc);
	}

	public void ContractSuccess(Document doc) {
		var other = doc.SideA == this ? doc.SideB : doc.SideA;

		// placeholder!! TODO hold place with something better
		const float MULTIPLY_RESOURCE_COSTS_EVERY_SUCCESS_BY = 1.1f;
		if (doc.type == Document.Type.MANDATE_CONTRACT && this == doc.SideA) {
			var newdoc = Briefcase.CreateExportMandate(
				doc.Points[0].Resources.Select((j) => new ResourceBundle(j.Type, (int)Math.Round(j.Amount * MULTIPLY_RESOURCE_COSTS_EVERY_SUCCESS_BY))).ToList(),
				doc.Points[1].Resources,
				this,
				(Faction)other, // trust in the cast
				GetTime() + GameTime.DAYS_PER_WEEK * GameTime.HOURS_PER_DAY * GameTime.MINUTES_PER_HOUR
			);
			((Faction)other).Briefcase.AddDocument(newdoc);
		}
	}

}

