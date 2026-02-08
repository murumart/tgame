using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
	public event Action<Job, int> JobChangedEvent;
	public event Action<Job> JobRemovedEvent;

	public Region Region { get; init; }

	readonly List<Job> jobs = new();
	readonly Dictionary<Vector2I, Job> jobsByPosition = new();

	readonly ResourceStorage resourceStorage = new();
	public ResourceStorage Resources { get => resourceStorage; }

	public readonly Population Population;

	public uint HomelessPopulation => Population.Count - Population.HousedCount;
	public uint UnemployedPopulation => Population.Count - Population.EmployedCount;

	public readonly string Name;

	public string DocName => ToString();
	public Briefcase Briefcase { get; init; }

	TimeT time;


	public Faction(Region region, uint initialPopulation = 30) {

		Region = region;
		Briefcase = new();
		Name = Naming.GenRandomName();

		region.MapObjectUpdatedAtEvent += OnMapObjectUpdated;

		Population = new();
		Population.FoodRequested += OnGetMoreFoodRequested;
		Population.JobEmploymentChanged += (j, a) => JobChangedEvent?.Invoke(j, a);
		Population.Manifest(initialPopulation);

		Region.SetLocalFaction(this);

		Resources.AddResource(new(Registry.ResourcesS.Bread, 10)); // initial buffer (DEBUG probably)
		PlacePrebuiltBuilding(Registry.BuildingsS.LogCabin, new(0, 0));
	}

	// *** MANAGING WORKERS AND JOBS ***

	public uint GetPopulationCount() => Population.Count;

	public void AddMapObjectJob(MapObjectJob job, MapObject mapObject) {
		RegisterJob(mapObject.GlobalPosition, job);

		Debug.Assert(job.CanInitialise(this, mapObject), "Job cannot be initialised!");
		job.Initialise(this, mapObject);
		JobAddedEvent?.Invoke(job);
	}

	void RegisterJob(Vector2I position, Job job) {
		Debug.Assert(!jobsByPosition.ContainsKey(position), $"There's already a job registred at {position}");
		jobsByPosition[position] = job;
		jobs.Add(job);
	}

	public void RemoveJob(Job job) {
		if (job is MapObjectJob mopjob) {
			Debug.Assert(jobsByPosition.ContainsKey(mopjob.GlobalPosition), $"Can't remove job ({job}) that doesn't exist here ({mopjob.GlobalPosition})?? Hello?");
			jobsByPosition.Remove(mopjob.GlobalPosition);
		}
		Debug.Assert(jobs.Contains(job), "Dont have this job, can't remove it");
		if (job.NeedsWorkers) UnemployAllWorkers(job);
		jobs.Remove(job);
		JobRemovedEvent?.Invoke(job);
		job.Deinitialise(this);
	}

	public bool GetJob(Vector2I position, out Job job) {
		return jobsByPosition.TryGetValue(position, out job);
	}

	public uint GetFreeWorkers() => UnemployedPopulation;

	public bool CanEmployWorkers(Job job, int amount) {
		Debug.Assert(jobs.Contains(job), "This isn't my job...");
		return UnemployedPopulation > 0 && job.Workers + amount <= job.MaxWorkers;
	}

	public bool CanUnemployWorkers(Job job, int amount) {
		Debug.Assert(jobs.Contains(job), "This isnt ,y job...");
		return job.Workers >= amount;
	}

	public void EmployWorkers(Job job, int amount) {
		Debug.Assert(jobs.Contains(job), "This isn't my job...");
		if (amount < 0) {
			Debug.Assert(CanUnemployWorkers(job, -amount), $"Can't employ these workers! (amount {amount}, workers {job.Workers})");
			Population.Unemploy(job, (uint)-amount);
			return;
		}
		Debug.Assert(CanEmployWorkers(job, amount), $"Can't employ these workers! (amount {amount}, workers {job.Workers})");

		Population.Employ(job, (uint)amount);
	}

	public void UnemployAllWorkers(Job job) {
		if (job.Workers == 0) return; // noone was ever assigned
		Debug.Assert(jobs.Contains(job), "This isn't my job...");
		Debug.Assert(CanUnemployWorkers(job, job.Workers), $"Can't unemploy these workers! (workers {job.Workers})");

		Population.Unemploy(job, (uint)job.Workers);
	}

	public void DecreasePopulation(uint by) {
		Population.Reduce(by);
	}

	uint OnGetMoreFoodRequested(uint amount) {
		Debug.Assert(amount > 0, "Why request 0 food?");
		uint food = 0;
		foreach (var (type, value) in Registry.ResourcesS.FoodValues) {
			Debug.Assert(value >= 0, $"Food value is negative ({value})");
			if (food >= amount) break;
			int fcount = Resources.GetCount(type);
			if (fcount == 0) continue;
			int fulfillingvalue = (((int)amount - (int)food) / value + 1);
			int amountchange = Math.Min(fulfillingvalue, fcount);
			int gotvalue = amountchange * value;
			Debug.Assert(gotvalue > 0, $"Gotvalue was {gotvalue} <= 0");
			Resources.SubtractResource(type, amountchange);
			food += (uint)gotvalue;
		}
		return food;
	}

	public static float GetFoodUsageS(uint population, uint employed) {
		return employed + (population - employed) * 0.5f;
	}

	public float GetFoodUsage() => GetFoodUsageS(GetPopulationCount(), Population.EmployedCount);

	public float GetFood() {
		float f = 0;
		foreach (var rb in Resources) {
			if (Registry.ResourcesS.FoodValues.TryGetValue(rb.Key, out int foodValue)) {
				f += foodValue * rb.Value.Amount;
			}
		}
		f += Population.Food;
		return f;
	}

	// *** MANAGING BUILDINGS ***

	// does what the method name says: a construction site is created and placed in the world
	public Building PlaceBuildingConstructionSite(IBuildingType type, Vector2I position) {
		Debug.Assert(Region.CanPlaceBuilding(position), $"Region says can't place building at {position}");
		Debug.Assert(CanPlaceBuilding(type, position), "Cannot place the building for whatever reason");
		var building = CreateBuilding(type, position);
		if (type.TakesTimeToConstruct() || type.HasResourceRequirements()) {
			var job = new ConstructBuildingJob(type.GetResourceRequirements().ToList());
			AddMapObjectJob(job, building);
			building.ConstructionJob = job;
		}
		return building;
	}

	// creates a building object and then completes its construction. for initialising the world and such
	Building PlacePrebuiltBuilding(IBuildingType type, Vector2I position) {
		Debug.Assert(!Region.HasMapObject(position), $"There's a lreayd a building here (at {position})");
		var building = CreateBuilding(type, position);
		building.ProgressBuild((int)(type.GetHoursToConstruct() * 60), new AnonBuilderJob());
		return building;
	}

	// asks the region to create a building spot and gets a building from it.
	Building CreateBuilding(IBuildingType type, Vector2I position) {
		var building = Region.CreateBuildingSpotAndPlace(type, position);
		building.BuildingConstructed += OnBuildingConstructed;
		return building;
	}

	public bool CanPlaceBuilding(IBuildingType type, Vector2I tilepos) {
		return HasBuildingMaterials(type) && Region.CanPlaceBuilding(tilepos);
	}

	public bool HasBuildingMaterials(IBuildingType type) {
		return resourceStorage.HasEnoughAll(type.GetResourceRequirements());
	}

	void OnBuildingConstructed(Building building) {
		Population.ChangeHousingCapacity((int)building.GetHousingCapacity());
	}

	// deletes a building from the faction's records and has the region also delete it
	public void RemoveBuilding(Vector2I at) {
		Debug.Assert(HasBuilding(at), $"There's no building to remove at {at}...");
		var building = GetBuilding(at);
		if (GetJob(at, out var job)) {
			RemoveJob(job);
		}
		if (building.IsConstructed) {
			Population.ChangeHousingCapacity(-(int)building.GetHousingCapacity());
		}
		Region.RemoveMapObject(at);
	}

	// just removes a map object, intended for resource sites etc
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
		Population.PassTime(minutes);
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

	public override string ToString() {
		return Name;
	}

	public static class Naming {

		static readonly Dictionary<string, float> syllableCounts = new();
		static float maxSylProb = 0;

		public static string GenRandomName() {
			if (syllableCounts.Count == 0) {
				var f = FileAccess.Open("res://tools/silbitus/syls.txt", FileAccess.ModeFlags.Read);
				Debug.Assert(FileAccess.GetOpenError() == Error.Ok, "Failed opening syllables file");
				while (!f.EofReached()) {
					string line = f.GetLine();
					if (line.Length <= 1) break;
					string[] split = line.Split(" ");
					Debug.Assert(split.Length == 2, $"Invalid line in file? ({line}) length {split.Length})");
					syllableCounts.Add(split[0], split[1].ToFloat());
				}
				f.Close();
				maxSylProb = syllableCounts.Values.Sum();
			}

			var sb = new StringBuilder();
			var maxlen = 3 + GD.Randi() % 6;
			while (sb.Length < maxlen) {
				var r = GD.Randf() * maxSylProb;
				float sum = 0;
				foreach (var (syl, prob) in syllableCounts) {
					sum += prob;
					if (r <= sum) {
						sb.Append(syl);
						break;
					}
				}
			}

			sb[0] = char.ToUpper(sb[0]);
			return sb.ToString();
		}

	}

}

