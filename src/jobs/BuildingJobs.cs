using System.Collections.Generic;
using Godot;
using static Faction;

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

public interface IConstructBuildingJob {

	float GetProgressPerMinute();

}

public class ConstructBuildingJob : Job, IConstructBuildingJob {

	public override string Title => "Construct Building";
	public override ref Population Workers => ref workers;

	readonly List<ResourceBundle> requirements;

	Population workers;
	readonly Building building;


	public ConstructBuildingJob(List<ResourceBundle> requirements, Building building) {
		this.requirements = requirements;
		this.building = building;
		workers = new(35);
	}

	public override bool CanInitialise(RegionFaction ctxFaction) => ctxFaction.Resources.HasEnoughAll(GetRequirements());

	public override void Initialise(RegionFaction ctxFaction) {
		Debug.Assert(CanInitialise(ctxFaction), "Job cannot be initialised!!");
		ConsumeRequirements(requirements, ctxFaction);
	}

	public override void Deinitialise(RegionFaction ctxFaction) {
		if (building.IsConstructed) {
			RefundRequirements(requirements, ctxFaction);
			requirements.Clear();
		} else {
			// TODO remove building if not constructed

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

	public override Job Copy() {
		return new ConstructBuildingJob(requirements, building);
	}

	public float GetProgressPerMinute() {
		return workers.Amount;
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