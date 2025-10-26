using System;
using Godot;

public partial class JobType : Resource, IWorkerJob {

	public bool CanCreateJob(Faction.RegionFaction ctxFaction) {
		throw new NotImplementedException();
	}

	public ref Population GetWorkers() {
		throw new NotImplementedException();
	}

	public void PassTime(float hours) {
		throw new NotImplementedException();
	}
	
}
