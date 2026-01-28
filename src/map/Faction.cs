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
	public event Action<Job> JobAddedEvent;
	public event Action<Job> JobRemovedEvent;

	public Region Region { get; init; }

	readonly List<Job> jobs = new();
	readonly Dictionary<Vector2I, HashSet<Job>> jobsByPosition = new();

	readonly ResourceStorage resourceStorage = new();
	public ResourceStorage Resources { get => resourceStorage; }

	public readonly Population Population;

	public int HomelessPopulation => Population.HomelessCount;
	public int UnemployedPopulation => Population.UnemployedCount;

	public string DocName => ToString();
	public Briefcase Briefcase { get; init; }

	TimeT time;


	public Faction(Region region, int initialPopulation = 30, int maxPop = 100) {
		maxPop = -1; // UNUSED

		Region = region;
		Briefcase = new();

		region.MapObjectUpdatedAtEvent += OnMapObjectUpdated;

		Population = new();
		Population.Manifest(initialPopulation);

		Region.SetLocalFaction(this);

		var housing = Registry.Buildings.GetAsset("log_cabin");
		PlacePrebuiltBuilding(housing, new(0, 0));
	}

	// *** MANAGING WORKERS AND JOBS ***

	public int GetPopulationCount() => Population.Count;

	public void AddMapObjectJob(MapObjectJob job, MapObject mapObject) {
		RegisterJob(mapObject.GlobalPosition, job);

		Debug.Assert(job.CanInitialise(this, mapObject), "Job cannot be initialised!");
		job.Initialise(this, mapObject);
		JobAddedEvent?.Invoke(job);
	}

	void RegisterJob(Vector2I position, Job job) {
		Debug.Assert(!(jobsByPosition.ContainsKey(position) && jobsByPosition[position].Contains(job)), $"Job object ({job}) at {position} exists ");
		if (!jobsByPosition.ContainsKey(position)) jobsByPosition[position] = new();
		jobsByPosition[position].Add(job);
		jobs.Add(job);
	}

	public void RemoveJob(Job job) {
		if (job is MapObjectJob mopjob) {
			Debug.Assert(jobsByPosition.ContainsKey(mopjob.GlobalPosition) && jobsByPosition[mopjob.GlobalPosition].Contains(job), $"Can't remove job ({job}) that doesn't exist here ({mopjob.GlobalPosition})?? Hello?");
			jobsByPosition[mopjob.GlobalPosition].Remove(job);
		}
		Debug.Assert(jobs.Contains(job), "Dont have this job, can't remove it");
		if (job.NeedsWorkers) UnemployAllWorkers(job);
		jobs.Remove(job);
		JobRemovedEvent?.Invoke(job);
		job.Deinitialise(this);
	}

	public IEnumerable<Job> GetJobs(Vector2I position) {
		jobsByPosition.TryGetValue(position, out HashSet<Job> gottenJobs);
		return gottenJobs?.ToList() ?? new List<Job>();
	}

	public int GetFreeWorkers() => Population.UnemployedCount;

	public bool CanEmployWorkers(Job job, int amount) {
		Debug.Assert(jobs.Contains(job), "This isn't my job...");
		return UnemployedPopulation > 0 && job.Workers.CanAdd(amount);
	}

	public bool CanUnemployWorkers(Job job, int amount) {
		Debug.Assert(jobs.Contains(job), "This isnt ,y job...");
		return job.Workers.Count >= amount;
	}

	public void EmployWorkers(Job job, int amount) {
		Debug.Assert(jobs.Contains(job), "This isn't my job...");
		if (amount < 0) {
			Debug.Assert(CanUnemployWorkers(job, -amount), $"Can't employ these workers! (amount {amount}, workers {job.Workers})");
			Population.Unemploy(job, -amount);
			return;
		}
		Debug.Assert(CanEmployWorkers(job, amount), $"Can't employ these workers! (amount {amount}, workers {job.Workers})");

		Population.Employ(job, amount);
	}

	public void UnemployAllWorkers(Job job) {
		if (job.Workers.Count == 0) return; // noone was ever assigned
		Debug.Assert(jobs.Contains(job), "This isn't my job...");
		Debug.Assert(CanUnemployWorkers(job, job.Workers.Count), $"Can't unemploy these workers! (workers {job.Workers})");

		Population.Unemploy(job, job.Workers.Count);
	}

	public void DecreasePopulation(int by) {
		throw new NotImplementedException();
	}

	// *** MANAGING BUILDINGS ***

	public Building PlaceBuildingConstructionSite(IBuildingType type, Vector2I position) {
		Debug.Assert(Region.CanPlaceBuilding(position), $"Region says can't place building at {position}");
		Debug.Assert(CanPlaceBuilding(type, position), "Cannot place the building for whatever reason");
		var building = PlaceBuilding(type, position);
		if (type.TakesTimeToConstruct() || type.HasResourceRequirements()) {
			var job = new ConstructBuildingJob(type.GetResourceRequirements().ToList());
			AddMapObjectJob(job, building);
			building.ConstructionJob = job;
		}
		return building;
	}

	// for initialising the world and such
	Building PlacePrebuiltBuilding(IBuildingType type, Vector2I position) {
		Debug.Assert(!Region.HasMapObject(position), $"There's a lreayd a building here (at {position})");
		var building = PlaceBuilding(type, position);
		building.ProgressBuild((int)(type.GetHoursToConstruct() * 60), new AnonBuilderJob());
		return building;
	}

	Building PlaceBuilding(IBuildingType type, Vector2I position) {
		var building = Region.CreateBuildingSpotAndPlace(type, position);
		if (type.GetPopulationCapacity() > 0) AddMapObjectJob(new AbsorbFromHomelessPopulationJob(), building);
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
		foreach (var job in GetJobs(at)) {
			RemoveJob(job);
		}
		Region.RemoveMapObject(at);
	}

	public void Uproot(Vector2I at) {
		bool has = Region.HasMapObject(at, out var obj);
		Debug.Assert(has, $"No map object to uproot at {at}");
		Region.RemoveMapObject(at);
	}

	public bool HasBuilding(Vector2I at) => Region.HasMapObject(at) && Region.GetMapObject(at) is Building;

	public Building GetBuilding(Vector2I at) {
		Debug.Assert(HasBuilding(at), $"Don't have a building at {at}");
		return Region.GetMapObject(at) as Building;
	}

	private class AnonBuilderJob : ConstructBuildingJob {

		public AnonBuilderJob() : base(null) { }
		public override float GetWorkTime(TimeT minutes) => minutes;

	}

	void OnMapObjectUpdated(Vector2I at) { }

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

	public void MakeFactionSubservient(Faction faction) {
		var doc = Briefcase.CreateOwningRelationship(this, faction);
		Debug.Assert(!faction.HasOwningFaction(), "This faction is already owned by another faction");
		faction.Briefcase.AddDocument(Point.Type.HasColony, doc);
	}

	public bool HasOwningFaction() {
		return Briefcase.ContainsPointType(Point.Type.HasColony) && Briefcase.GetOwnerDocument().SideB == this;
	}

	// this is meant to be called by the subservient faction
	// check first with HasOwningFaction
	public Faction GetOwningFaction() {
		return Briefcase.GetOwnerDocument().SideA;
	}

	public void ContractFailure(Document doc, Point fulfillFailure) {
		ContractFailedEvent?.Invoke(doc);
	}

	public void ContractSuccess(Document doc) {
		var other = doc.SideA == this ? doc.SideB : doc.SideA;

		// placeholder!! TODO hold place with something better
		const float MULTIPLY_RESOURCE_COSTS_EVERY_SUCCESS_BY = 1.1f;
		if (doc.ContainsPointType(Document.Point.Type.ProvidesResourcesTo) && this == doc.SideA) {
			var newdoc = Briefcase.CreateExportMandate(
				doc.Points[0].Resources.Select((j) => new ResourceBundle(j.Type, (int)Math.Round(j.Amount * MULTIPLY_RESOURCE_COSTS_EVERY_SUCCESS_BY))).ToList(),
				doc.Points[1].Resources,
				this,
				other,
				GetTime() + GameTime.DAYS_PER_WEEK * GameTime.HOURS_PER_DAY * GameTime.MINUTES_PER_HOUR
			);
			(other).Briefcase.AddDocument(Document.Point.Type.ProvidesResourcesTo, newdoc);
		}
	}

}

