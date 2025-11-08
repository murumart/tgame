using System;
using System.Collections.Generic;
using Godot;

public class AbsorbFromHomelessPopulationJob : Job {

	public override bool IsInternal => true;
	public override bool NeedsWorkers => false;
	public bool Paused { get; set; }

	readonly Building building;
	RegionFaction regionFaction;


	public AbsorbFromHomelessPopulationJob(Building building) {
		this.building = building;
	}

	public void Finish() { }

	TimeT remainderPeopleTransferTime;
	public override void PassTime(TimeT minutes) {
		if (!Paused && building.IsConstructed && regionFaction.HomelessPopulation.Amount > 0) {
			/* if (homelessPopulation.CanTransfer(ref building.Population, 1)) {
				homelessPopulation.Transfer(ref building.Population, 1);
			} */
			TimeT peopleTransferTime = minutes + remainderPeopleTransferTime;
			while (peopleTransferTime > 6 && regionFaction.HomelessPopulation.CanTransfer(ref building.Population, 1)) {
				regionFaction.HomelessPopulation.Transfer(ref building.Population, 1);
				peopleTransferTime -= 6;
			}
			remainderPeopleTransferTime = peopleTransferTime;
		} else {
			remainderPeopleTransferTime = 0;
		}
	}

	public override bool CanInitialise(RegionFaction ctxFaction) => building != null && ctxFaction != null;

	public override void Initialise(RegionFaction ctxFaction) {
		this.regionFaction = ctxFaction;
	}

	public override void Deinitialise(RegionFaction ctxFaction) => throw new System.NotImplementedException("no..? pause this instead");

	public override Job Copy() => throw new System.NotImplementedException("You shouldn't have to copy this...");

}

public class ConstructBuildingJob : MapObjectJob {

	public override string Title => "Construct Building";
	public override ref Population Workers => ref workers;

	readonly List<ResourceBundle> requirements;

	Population workers;
	Building building;


	public ConstructBuildingJob(List<ResourceBundle> requirements) {
		this.requirements = requirements;
		workers = new(35);
	}

	public override bool CanInitialise(RegionFaction ctxFaction, MapObject building) => ctxFaction.Resources.HasEnoughAll(GetRequirements());

	public override void Initialise(RegionFaction ctxFaction, MapObject mapObject) {
		Debug.Assert(CanInitialise(ctxFaction, mapObject), "Job cannot be initialised!!");
		building = (Building)mapObject;
		ConsumeRequirements(requirements, ctxFaction.Resources);
	}

	public override void Deinitialise(RegionFaction ctxFaction) {
		if (building.IsConstructed) {
			requirements.Clear();
		} else {
			RefundRequirements(requirements, ctxFaction.Resources);
			ctxFaction.RemoveBuilding(building.Position);
			building = null;
		}
	}

	public override List<ResourceBundle> GetRequirements() => requirements;

	public override void PassTime(TimeT minutes) {
		if (building.IsConstructed) {
			return;
		}
		building.ProgressBuild(minutes, this);
	}

	public override void CheckDone(RegionFaction regionFaction) {
		if (building.IsConstructed) {
			regionFaction.RemoveJob(building.Position, this);
		}
	}

	public override float GetProgressEstimate() => building.GetBuildProgress();

	public override Job Copy() {
		return new ConstructBuildingJob(requirements);
	}

	public override float GetWorkTime(TimeT minutes) {
		return minutes * Mathf.Pow(workers.Amount, 0.7f);
	}

	public override string GetStatusDescription() {
		float hoursLeft = ((1 - GetProgressEstimate()) * building.Type.GetHoursToConstruct());
		float speed = GetWorkTime(1);
		hoursLeft /= speed;
		float mins = hoursLeft - (int)hoursLeft;
		hoursLeft -= mins;
		GD.Print($"{GetProgressEstimate()} {building.Type.GetHoursToConstruct()} {hoursLeft} {speed}");
		var str2 = "The construction will take ";
		if (hoursLeft > 0) str2 += $"{hoursLeft:0} more hours.";
		else str2 += $"{mins * 60:0} more minutes.";
		return str2;
	}

	public override string GetProductionDescription() {
		if (building != null) {
			return $"A {building.Type.Name} will be constructed.";
		}
		return "";
	}

}

public class FishByHandJob : Job {

	public override string Title => "Fish by Hand";
	public override ref Population Workers => ref workers;

	ResourceStorage storage;
	Population workers;


	public FishByHandJob() {
		workers = new(5);
	}

	public override void Deinitialise(RegionFaction ctxFaction) { }

	public override bool CanInitialise(RegionFaction ctxFaction) => true;

	public override Job Copy() => new FishByHandJob();

	public override void Initialise(RegionFaction ctxFaction) {
		storage = ctxFaction.Resources;
	}

	public override void PassTime(TimeT minutes) {
		GD.Print("We fish x", minutes);
	}

}

public class GatherResourceJob : MapObjectJob {

	public override string Title =>
		site == null ?
			(resourceTypeDescription == null ?
				"Gather Resources"
			: "Gather " + resourceTypeDescription.Capitalize())
		: "Gather from " + site.Type.Name;

	public override ref Population Workers => ref workers;

	Population workers;
	ResourceStorage storage;
	ResourceSite site;
	readonly string resourceTypeDescription;

	float[] timeSpent; // storing as float due to workers
	readonly List<ResourceBundle> grant = new();


	public GatherResourceJob() {
		workers = new(5);
	}

	public GatherResourceJob(string resourceTypeDescription) : this() {
		this.resourceTypeDescription = resourceTypeDescription;
	}

	public override Job Copy() => new GatherResourceJob(resourceTypeDescription);

	public override void Initialise(RegionFaction ctxFaction, MapObject mapObject) {
		storage = ctxFaction.Resources;
		site = (ResourceSite)mapObject;
		timeSpent = new float[site.MineWells.Count];
	}

	public override void Deinitialise(RegionFaction ctxFaction) {

	}

	public override float GetWorkTime(TimeT minutes) => minutes * MathF.Pow(workers.Amount, 0.7f);

	public override void PassTime(TimeT minutes) {
		float ts = GetWorkTime(minutes);

		for (int i = 0; i < timeSpent.Length; i++) {
			timeSpent[i] += ts;

			var well = site.MineWells[i];

			while (timeSpent[i] >= well.MinutesPerBunch && storage.CanAdd(well.BunchSize) && well.HasBunches) {
				timeSpent[i] -= well.MinutesPerBunch;
				well.Deplete();
				grant.Add(new(well.ResourceType, well.BunchSize));
			}
		}

		if (grant.Count != 0) {
			ProvideProduction(grant, storage);
			grant.Clear();
		}
	}

	public override void CheckDone(RegionFaction regionFaction) {
		foreach (var item in site.MineWells) {
			if (item.HasBunches) return;
		}

		regionFaction.RemoveJob(site.Position, this);
		regionFaction.Uproot(site.Position);
		site = null;
	}

	// display

	public override float GetProgressEstimate() {
		float estimate = 0f;
		for (int i = 0; i < timeSpent.Length; i++) {
			var well = site.MineWells[i];
			estimate = Math.Max(estimate, timeSpent[i] / well.MinutesPerBunch);
		}
		return estimate;
	}

	int GetClosestWell() {
		float time = -1f;
		int ix = -1;
		for (int i = 0; i < timeSpent.Length; i++) {
			var well = site.MineWells[i];
			if (timeSpent[i] / well.MinutesPerBunch > time) {
				time = timeSpent[i];
				ix = i;
			}
		}
		Debug.Assert(ix != -1, "Couldn't get closest well? What the issue");
		return ix;
	}

	public override string GetProductionDescription() {
		if (site == null) {
			return Title;
		}
		var str = $"The {site.Type.Name} produces:\n";
		if (workers.Amount <= 0) str += "Nothing, as long as there's no workers. But it could produce:\n";
		bool reproduce = false;
		foreach (var well in site.MineWells) {
			float time = well.MinutesPerBunch / MathF.Max(GetWorkTime(1), 1);
			str += $" * {well.BunchSize} x {well.ResourceType.Name} every {GameTime.FancyTimeString((TimeT)time)}.\n";
			str += $"   This is {100 - ((float)well.Bunches / well.InitialBunches) * 100:0}% depleted.\n";
			reproduce = reproduce || well.MinutesPerBunchRegen > 0;
		}

		if (reproduce) {
			str += $"\nSome {(resourceTypeDescription ?? "resources")} can regrow:\n";
			foreach (var well in site.MineWells) {
				if (well.MinutesPerBunchRegen > 0) {
					str += $" * {well.ResourceType.Name.Capitalize()} grows every {GameTime.FancyTimeString(well.MinutesPerBunchRegen)}.";
				}
			}
		}

		return str;
	}

	public override string GetStatusDescription() {
		if (workers.Amount <= 0) return "";
		int wellIx = GetClosestWell();
		var well = site.MineWells[wellIx];
		float timeLeft = well.MinutesPerBunch - timeSpent[wellIx];
		timeLeft /= GetWorkTime(1);
		return GameTime.FancyTimeString((TimeT)timeLeft) + " until more " + well.ResourceType.Name + ".";
	}

}