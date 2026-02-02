using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static Building;

public class FactionActions {

	readonly Region region;
	public Region Region { get => region; }
	readonly Faction faction;
	public Faction Faction { get => faction; }


	public FactionActions(Region region, Faction faction) {
		this.region = region;
		this.faction = faction;
	}

	// resources

	public ResourceStorage GetResourceStorage() {
		return faction.Resources;
	}

	public IEnumerable<Vector2I> GetTiles() {
		return region.GroundTiles.Keys;
	}

	public static float GetFoodUsageS(uint population, uint employed) {
		return employed + (population - employed) * 0.5f;
	}

	public float GetFoodUsage() => GetFoodUsageS(Faction.GetPopulationCount(), Faction.Population.EmployedCount);

	public float GetFood() {
		float f = 0;
		foreach (var rb in Faction.Resources) {
			if (Registry.ResourcesS.FoodValues.GroupValues.TryGetValue(rb.Key, out int foodValue)) {
				f += foodValue * rb.Value.Amount;
			}
		}
		f += Faction.Population.Food;
		return f;
	}

	public (uint, uint) GetFoodAndUsage() {
		return (
			(uint)Mathf.Round(GetFood()),
			(uint)Mathf.Round(GetFoodUsage())
		);
	}

	// building

	public bool CanBuild(IBuildingType type) {
		return faction.HasBuildingMaterials(type);
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

	public void ChangeJobWorkerCount(Job job, int by) {
		faction.EmployWorkers(job, by);
	}

	public HashSet<Job> GetMapObjectJobs() {
		var hs = new HashSet<Job>();
		foreach (var mp in region.GetMapObjects()) {
			foreach (var job in GetMapObjectJobs(mp)) {
				hs.Add(job);
			}
		}
		return hs;
	}

	public HashSet<Job> GetMapObjectJobs(MapObject building) {
		var pos = building.GlobalPosition;
		var jobs = new HashSet<Job>();
		foreach (var job in faction.GetJobs(pos)) {
			jobs.Add(job);
		}
		return jobs;
	}

	public uint GetHomelessPopulationCount() => faction.HomelessPopulation;

	public uint GetFreeWorkers() => GetUnemployedPopulationCount();

	public uint GetUnemployedPopulationCount() => faction.UnemployedPopulation;

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