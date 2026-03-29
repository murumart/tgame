using System;
using System.Collections.Generic;
using Godot;
using scenes.autoload;
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

	public Span<KeyValuePair<Vector2I, GroundTileType>> GetTiles() {
		return region.GetGroundTiles();
	}

	public (float Food, float Usage) GetFoodAndUsage() {
		return (
			faction.GetFood(),
			faction.GetFoodUsage()
		);
	}

	// building

	public bool CanPlaceBuilding(IBuildingType type, Vector2I tilepos) {
		Debug.Assert(type != null, "Cant place NULL building type!!");
		return faction.CanPlaceBuilding(type, tilepos);
	}

	public Building PlaceBuilding(IBuildingType type, Vector2I tilepos) {
		Debug.Assert(type != null, "Cant place NULL building type!!");
		return faction.PlaceBuildingConstructionSite(type, tilepos);
	}

	public IEnumerable<MapObject> GetMapObjects() => region.GetMapObjects();


	public int GetBuildingCount(IBuildingType buildingType) {
		return faction.GetBuildingCount(buildingType);
	}

	ProcessMarketJob _marketJobCached = null;
	public ProcessMarketJob GetProcessMarketJob() {
		if (_marketJobCached == null || !_marketJobCached.IsValid) {
			_marketJobCached = null;
			foreach (var m in GetMapObjects()) if (m is Building b) if (b.Type.GetSpecial() == IBuildingType.Special.Marketplace && b.IsConstructed) {
						var job = GetMapObjectsJob(m);
						if (job != null && job is ProcessMarketJob) _marketJobCached = job as ProcessMarketJob;
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

	public void AddJob(Problem problem, SolveProblemJob job) {
		faction.AddProblemJob(job, problem);
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

	public void Surrender(Faction to) {
		to.Region.AnnexAll(region, GameMan.Game.Map.TileOwners);
	}

	public static bool CanAttack(Region who, Region whom, Vector2I worldpos) {
		if (whom.GetEdge(worldpos - whom.WorldPosition, out var edge)) {
			if (edge.Above == who || edge.Below == who || edge.Left == who || edge.Right == who) {
				return true;
			}
		}
		return false;
	}

	public static TileAttackJob GetAttackJob(Faction from, Faction to, Vector2I globalPosition) {
		var job = new TileAttackJob(to.Region, globalPosition);
		return job;
	}

	public static void ApplyAttackJob(Faction from, TileAttackJob job) {
		from.AddAttackJob(job);
	}

	public static bool IsAttacking(Faction who, Faction whom, Vector2I globalPosition, out TileAttackJob tajob) {
		tajob = null;
		if (!who.GetJob(globalPosition, out var job)) return false;
		if (job is not TileAttackJob tj) return false;
		tajob = tj;
		if (tajob.Target.LocalFaction != whom) return false;
		return true;
	}

	public static void RemoveAttackingJob(Faction who, TileAttackJob job) {
		who.RemoveJob(job);
	}

	// notifications

	void HourlyUpdate(TimeT timeInMinutes) {
	}

	void OnRegionMandateFailed(Document doc) {
		GD.Print("FactionAction::OnRegionMandateFailed : MY MANDATE FAILED:::::: DAMN");
	}


	public override string ToString() {
		return $"FactionActions({region}, {faction})";
	}

}