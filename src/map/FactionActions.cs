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

	public (uint, uint) GetFoodAndUsage() {
		return (
			(uint)Mathf.Round(faction.GetFood()),
			(uint)Mathf.Round(faction.GetFoodUsage())
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

	ProcessMarketJob _marketJobCached = null;
	public ProcessMarketJob GetProcessMarketJob() {
		if (_marketJobCached == null || !_marketJobCached.IsValid) {
			_marketJobCached = null;
			foreach (var m in GetMapObjects()) if (m is Building b) if (b.Type.GetSpecial() == IBuildingType.Special.Marketplace && b.IsConstructed) {
						_marketJobCached = GetMapObjectsJob(m) as ProcessMarketJob;
					}
		}
		return _marketJobCached;
	}

	Building _marketplaceCahced = null;
	public Building GetMarketplace() {
		if (_marketplaceCahced == null) {
			_marketplaceCahced = null;
			foreach (var m in GetMapObjects()) if (m is Building b) if (b.Type.GetSpecial() == IBuildingType.Special.Marketplace) {
						_marketplaceCahced = b;
					}
		}
		return _marketplaceCahced;
	}

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
			var job = GetMapObjectsJob(mp);
			if (job == null) continue;
			hs.Add(job);
		}
		return hs;
	}

	public Job GetMapObjectsJob(MapObject building) {
		var pos = building.GlobalPosition;
		var has = faction.GetJob(pos, out var job);
		if (!has) return null;
		return job;
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