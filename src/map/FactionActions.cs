using System.Collections.Generic;
using Godot;
using static Building;

public class FactionActions {

	Region region;
	Faction faction;


	public FactionActions(Region region, Faction faction) {
		this.region = region;
		this.faction = faction;
	}

	// resources

	public ResourceStorage GetResourceStorage() {
		return faction.Resources;
	}

	// building

	public bool CanBuild(IBuildingType type) {
		return faction.CanBuild(type);
	}

	public bool CanPlaceBuilding(IBuildingType type, Vector2I tilepos) {
		Debug.Assert(type != null, "Cant place NULL building type!!");
		return faction.CanPlaceBuilding(type, tilepos);
	}

	public Building PlaceBuilding(IBuildingType type, Vector2I tilepos) {
		Debug.Assert(type != null, "Cant place NULL building type!!");
		return faction.PlaceBuildingConstructionSite(type, tilepos);
	}

	public IEnumerable<MapObject> GetMapObjects() => region.GetMapObjects();

	// jobs

	public void AddJob(MapObject place, MapObjectJob job) {
		faction.AddMapObjectJob(job, place);
	}

	public void RemoveJob(Job job) {
		faction.RemoveJob(job);
	}

	public HashSet<Job> GetMapObjectJobs(MapObject building) {
		var pos = building.GlobalPosition;
		var jobs = new HashSet<Job>();
		foreach (var job in faction.GetJobs(pos)) {
			jobs.Add(job);
		}
		return jobs;
	}

	public int GetHomelessPopulationCount() => faction.HomelessPopulation;

	public int GetUnemployedPopulationCount() => faction.UnemployedPopulation;

	// notifications

	void HourlyUpdate(TimeT timeInMinutes) {
	}

	void OnRegionMapObjectUpdated(Vector2I tile) { }

	void OnRegionMandateFailed(Document doc) {
		GD.Print("FactionAction::OnRegionMandateFailed : MY MANDATE FAILED:::::: DAMN");
	}


	public override string ToString() {
		return $"FactionActions({region}, {faction})";
	}

}