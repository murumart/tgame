using Godot;
using static Faction;


// abstract jobs

public interface IJob : ITimePassing {

	bool CanCreateJob(RegionFaction ctxFaction);

}

public interface IWorkerJob : IJob {

	ref Population GetWorkers();

}


// concrete (hard-coded) jobs

public class AbsorbFromHomelessPopulationJob : IJob {

	Building building;
	RegionFaction faction;

	public AbsorbFromHomelessPopulationJob(Building building, RegionFaction fac) {
		this.building = building;
		this.faction = fac;
	}

	public void Finish() { }

	float remainderPeopleTransferTime;
	public void PassTime(float hours) {
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

    public bool CanCreateJob(RegionFaction ctxFaction) {
	throw new System.NotImplementedException();
    }
}
