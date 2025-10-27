using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using static Faction;


// abstract jobs

public abstract class Job : ITimePassing {

	private Population dummyPop;


	public abstract bool CanCreateJob(RegionFaction ctxFaction);

	public virtual ref Population GetWorkers() { return ref dummyPop; }

	public virtual List<ResourceBundle> GetRequirements() { return null; }
	public virtual List<ResourceBundle> GetRewards() { return null; }

	public abstract void PassTime(float hours);
}

// concrete (hard-coded) jobs

public class AbsorbFromHomelessPopulationJob : Job {

	Building building;
	RegionFaction faction;

	public AbsorbFromHomelessPopulationJob(Building building, RegionFaction fac) {
		this.building = building;
		this.faction = fac;
	}

	public void Finish() { }

	float remainderPeopleTransferTime;
	public override void PassTime(float hours) {
		if (building.IsConstructed && faction.HomelessPopulation.Pop > 0) {
			/* if (homelessPopulation.CanTransfer(ref building.Population, 1)) {
				homelessPopulation.Transfer(ref building.Population, 1);
			} */
			float peopleTransferTime = hours + remainderPeopleTransferTime;
			while (peopleTransferTime > 0.1 && faction.HomelessPopulation.CanTransfer(ref building.Population, 1)) {
				faction.HomelessPopulation.Transfer(ref building.Population, 1);
				peopleTransferTime -= 0.1f;
			}
			remainderPeopleTransferTime = peopleTransferTime;
		} else {
			remainderPeopleTransferTime = 0.0f;
		}
	}

	public override bool CanCreateJob(RegionFaction ctxFaction) {
		throw new System.NotImplementedException();
	}
}

public class ConstructBuildingJob : Job {

	List<ResourceBundle> requirements;
	Population workers;
	Building building;


	public ConstructBuildingJob(List<ResourceBundle> requirements, Building building) {
		this.requirements = requirements;
		this.building = building;
	}

	public override bool CanCreateJob(RegionFaction ctxFaction) {
		return ctxFaction.Resources.HasAll(GetRequirements());
	}

	public override List<ResourceBundle> GetRequirements() {
		return requirements;
	}

	public override ref Population GetWorkers() {
		return ref workers;
	}

	public override void PassTime(float hours) {
		if (building.IsConstructed) return;
		building.ProgressBuild(hours * workers.Pop);
	}
}
