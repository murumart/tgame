using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Godot;
using static Faction;


// abstract jobs

public abstract class Job : ITimePassing {

	private Population dummyPop;

	public virtual string Title => "Some kind of job???";
	public virtual bool NeedsWorkers => true;
	public virtual bool Internal => false;


	public abstract Job Copy();

	public abstract bool CanCreateJob(RegionFaction ctxFaction);

	/// <summary>
	/// Call before adding job. Do things like consume resources here.
	/// </summary>
	/// <param name="ctxFaction"></param>
	public abstract void Initialise(RegionFaction ctxFaction);

	/// <summary>
	/// Call before removing job. Can uninitialise things here.
	/// </summary>
	/// <param name="ctxFaction"></param>
	public abstract void Cancel(RegionFaction ctxFaction);

	// sandbox methods vv

	public virtual ref Population GetWorkers() => ref dummyPop;

	public virtual List<ResourceBundle> GetRequirements() => null;
	public virtual List<ResourceBundle> GetRewards() => null;

	protected static void ConsumeRequirements(List<ResourceBundle> requirements, RegionFaction ctxFaction) {
		foreach (var r in requirements) {
			ctxFaction.Resources.SubtractResource(r);
		}
	}

	protected static void RefundRequirements(List<ResourceBundle> requirements, RegionFaction ctxFaction) {
		foreach (var r in requirements) {
			ctxFaction.Resources.AddResource(r);
		}
	}

	protected static void ProvideRewards(List<ResourceBundle> rewards, RegionFaction ctxFaction) {
		foreach (var r in rewards) {
			ctxFaction.Resources.AddResource(r);
		}
	}

	public virtual string GetResourceRequirementDescription() {
		StringBuilder sb = new();
		var resourceReqs = GetRequirements();
		if (resourceReqs != null) {
			sb.Append("Required Resources:\n");
			foreach (ResourceBundle res in resourceReqs) {
				sb.Append("  ").Append(res.Type.Name).Append(" x ").Append(res.Amount).Append('\n');
			}
		}
		return sb.ToString();
	}

	public abstract void PassTime(TimeT minutes);
}

// concrete (hard-coded) jobs

public class AbsorbFromHomelessPopulationJob : Job {

	public override bool Internal => true;
	public override bool NeedsWorkers => false;

	readonly Building building;
	readonly RegionFaction faction;


	public AbsorbFromHomelessPopulationJob(Building building, RegionFaction fac) {
		this.building = building;
		this.faction = fac;
	}

	public void Finish() { }

	TimeT remainderPeopleTransferTime;
	public override void PassTime(TimeT minutes) {
		if (building.IsConstructed && faction.HomelessPopulation.Amount > 0) {
			/* if (homelessPopulation.CanTransfer(ref building.Population, 1)) {
				homelessPopulation.Transfer(ref building.Population, 1);
			} */
			TimeT peopleTransferTime = minutes + remainderPeopleTransferTime;
			while (peopleTransferTime > 6 && faction.HomelessPopulation.CanTransfer(ref building.Population, 1)) {
				faction.HomelessPopulation.Transfer(ref building.Population, 1);
				peopleTransferTime -= 6;
			}
			remainderPeopleTransferTime = peopleTransferTime;
		} else {
			remainderPeopleTransferTime = 0;
		}
	}

	public override bool CanCreateJob(RegionFaction ctxFaction) {
		throw new System.NotImplementedException("You can make this job anytime you want ;-))");
	}

	public override void Initialise(RegionFaction ctxFaction) {
		throw new System.NotImplementedException("You can make this job anytime you want ;-))");
	}

	public override void Cancel(RegionFaction ctxFaction) {
		throw new System.NotImplementedException("This doesn't do anything on this job class");
	}

	public override Job Copy() {
		throw new System.NotImplementedException("You shouldn't have to copy this...");
	}

}

public interface IConstructBuildingJob {

	float GetProgressPerMinute();

}

public class ConstructBuildingJob : Job, IConstructBuildingJob {

	public override string Title => "Construct Building";

	readonly List<ResourceBundle> requirements;

	Population workers;
	readonly Building building;


	public ConstructBuildingJob(List<ResourceBundle> requirements, Building building) {
		this.requirements = requirements;
		this.building = building;
		workers = new(35);
	}

	public override bool CanCreateJob(RegionFaction ctxFaction) => ctxFaction.Resources.HasEnoughAll(GetRequirements());

	public override void Initialise(RegionFaction ctxFaction) {
		Debug.Assert(CanCreateJob(ctxFaction), "Job cannot be created!!");
		ConsumeRequirements(requirements, ctxFaction);
	}

	public override void Cancel(RegionFaction ctxFaction) {
		RefundRequirements(requirements, ctxFaction);
		requirements.Clear();
	}

	public override List<ResourceBundle> GetRequirements() => requirements;

	public override ref Population GetWorkers() => ref workers;


	public override void PassTime(TimeT minutes) {
		if (building.IsConstructed) return;
		building.ProgressBuild(minutes, this);
	}

	public override Job Copy() {
		return new ConstructBuildingJob(requirements, building);
	}

	public float GetProgressPerMinute() {
		return workers.Amount;
	}
}

public class FishByHandJob : Job {

	public override string Title => "Fish by Hand";

	ResourceStorage storage;


	public override void Cancel(RegionFaction ctxFaction) {
		throw new System.NotImplementedException();
	}

	public override bool CanCreateJob(RegionFaction ctxFaction) => true;

	public override Job Copy() => new FishByHandJob();

	public override void Initialise(RegionFaction ctxFaction) {
		storage = ctxFaction.Resources;
	}

	public override void PassTime(TimeT minutes) {
		GD.Print("We fish x", minutes);
	}

}
