// game master
using System;
using System.Linq;
using Godot;
using scenes.autoload;
using static ResourceSite;

public class TroubleMaker {

	Map map;
	Region PlayerRegion { get; set; }
	Faction PlayerFaction {
		get {
			return PlayerRegion.LocalFaction;
		}
	}

	float delayUntilPlayerAction = 0.33f;
	const float changeCoef = 0.005f;

	readonly (Func<Faction, bool>, float)[] problemCreators;
	public float ProblemWeightsSum { get; private set; }


	public TroubleMaker(Map map) {
		this.map = map;

		UILayer.DebugDisplay(() => map is not null && PlayerRegion is not null ? "troublemaker: " + delayUntilPlayerAction : throw new Exception("invalid object"), "trouble");

		problemCreators = [
			(CreateHoodooMiningAccident, 0.7f),
			(CreateRockMiningAccident, 0.7f),
			(CreateFishingAccident, 0.5f),
			(HaveMercy, 0.34f),
			(CreateBroadleafGatheringAccident, 0.01f),
		];
		ProblemWeightsSum = problemCreators.Sum((a) => a.Item2);
	}

	public void SetPlayRegion(Region region) {
		PlayerRegion = region;
	}

	public void Deinit() {
		PlayerRegion = null;
		map = null;
	}

	public void Update() {
		delayUntilPlayerAction -= changeCoef;

		if (PlayerFaction.Population.ArePeopleStarving) delayUntilPlayerAction = Mathf.Max(delayUntilPlayerAction, 0.45f);
		if (PlayerFaction.Population.GetApprovalMonthlyChange() < 0f) delayUntilPlayerAction += 0.1f * changeCoef;
		delayUntilPlayerAction -= PlayerFaction.Population.Approval * 0.1f * changeCoef;

		if (delayUntilPlayerAction <= 0) {
			float rand = GD.Randf() * ProblemWeightsSum;
			foreach (var (p, w) in problemCreators) {
				rand -= w;
				if (rand >= 0) continue;
				bool did = p(PlayerFaction);
				if (did) GD.Print($"TroubleMaker::Update : made trouble with {p}");
				break;
			}
			CreateFishingAccident(PlayerFaction);
		}
	}

	static Vector2I[] GetFittingGatherJobPositions(Faction faction, IResourceSiteType resourceSiteType) {
		var fittingJobPositions = faction.GetJobs()
			.Where(j => j is GatherResourceJob gj && gj.Site.Type == resourceSiteType && gj.Workers >= 1)
			.Cast<MapObjectJob>()
			.Select(j => j.GlobalPosition - faction.Region.WorldPosition)
			.Where(p => !faction.HasProblem(p))
			.ToArray();
		return fittingJobPositions;
	}

	bool CreateGatherSiteAccident(Faction faction, IResourceSiteType resourceSiteType, string title, string jobtitle, string notiftitle, uint hourstosolve, float tensionAdd) {
		var poses = GetFittingGatherJobPositions(faction, resourceSiteType);
		if (poses.Length == 0) return false;
		int randomix = GD.RandRange(0, poses.Length - 1);
		var chosenpos = poses[randomix];
		var hasJob = faction.GetJob(chosenpos + faction.Region.WorldPosition, out var job);
		Debug.Assert(hasJob, "Doesn't have job");
		var problem = new WorkplaceAccidentProblem(chosenpos, job, title, jobtitle, notiftitle, hourstosolve);
		faction.AddProblem(problem, chosenpos);
		if (faction == PlayerFaction) delayUntilPlayerAction += tensionAdd;
		return true;
	}

	bool CreateFishingAccident(Faction faction) {
		return CreateGatherSiteAccident(faction, Registry.ResourceSitesS.FishingSpot, "Tipped Boat", "Save Boatmen", "A boat tipped over! Send people to save them before they drown!", 3, 0.33f);
	}

	bool CreateRockMiningAccident(Faction faction) {
		return CreateGatherSiteAccident(faction, Registry.ResourceSitesS.Rock, "Rockslide", "Save Miners", "Rock miners got stuck after a rockslide blocked their path. Send rescuers to bring them back home before they starve!", 5, 0.11f);
	}

	bool CreateHoodooMiningAccident(Faction faction) {
		return CreateGatherSiteAccident(faction, Registry.ResourceSitesS.Hoodoo, "Collapse", "Save Miners", "Miners got stuck after a hoodoo collapsed. Send rescuers to bring them back home before they starve!", 5, 0.11f);
	}

	bool CreateBroadleafGatheringAccident(Faction faction) {
		return CreateGatherSiteAccident(faction, Registry.ResourceSitesS.BroadleafWoods, "Tree Fell", "Save Foresters", "A tree fell, pinning down the legs of all the people currently working in the woods. Yeah, they were standing in a line parallel to the tree when it fell. Really unfortunate. Please save them?", 6, 0.4f);
	}

	bool HaveMercy(Faction faction) {
		if (faction == PlayerFaction) delayUntilPlayerAction += 0.33f;
		return true;
	}
}
