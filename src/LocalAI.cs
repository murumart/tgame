using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.game.resource_site_types;
using static Building;
using static ResourceSite;

public partial class LocalAI {

	readonly FactionActions factionActions;
	readonly Action[] mainActions;
	readonly List<Action> ephemeralActions;

	readonly Dictionary<IResourceType, DecisionFactor> resourceWants;

	TimeT time;


	public LocalAI(FactionActions actions) {
		this.factionActions = actions;
		this.resourceWants = new() {
			{Resources.Logs, DecisionFactors.ResourceWant(actions, Resources.Logs, 30)},
			{Resources.Rocks, DecisionFactors.ResourceWant(actions, Resources.Rocks, 30)},
		};
		this.mainActions = [
			Actions.CreateGatherJob([
					resourceWants[Resources.Logs],
					DecisionFactors.HasFreeResourceSite(actions, Resources.Trees),
				], factionActions, Resources.Trees),
			Actions.CreateGatherJob([
					resourceWants[Resources.Rocks],
					DecisionFactors.HasFreeResourceSite(actions, Resources.Boulder),
				], factionActions, Resources.Boulder),
		];
		foreach (var building in Resources.Buildings) {
			mainActions = mainActions.Append(
				Actions.PlaceBuildingJob(
					new DecisionFactor[] { DecisionFactors.HomelessnessRate(factionActions) }
						.Concat(
							building.GetResourceRequirements()
								.Select((rb) => DecisionFactors.ResourceNeed(factionActions, rb.Type, rb.Amount))).ToArray(),
					factionActions, building)
				).ToArray();
		}
		this.ephemeralActions = new();
	}

	public void Update(TimeT minute) {
		var delta = minute - time;

		Console.WriteLine($"LocalAI::Update : (of {factionActions}) doing update");
		var mstime = Time.GetTicksMsec();

		for (int i = 0; i < 5; i++) {
			ephemeralActions.Clear();
			foreach (var job in factionActions.GetMapObjectJobs()) {
				if (job is GatherResourceJob gjob) {
					foreach (var prod in gjob.GetProductions()) {
						ephemeralActions.Add(Actions.AssignWorkersToJob([
								DecisionFactors.HasFreeWorkers(factionActions),
								resourceWants.GetValueOrDefault(prod.ResourceType, DecisionFactors.Null),
								DecisionFactors.JobHasEmploymentSpots(factionActions, gjob),
							], factionActions, job));
						ephemeralActions.Add(Actions.RemoveJob([
								DecisionFactors.OneMinus(resourceWants.GetValueOrDefault(prod.ResourceType, DecisionFactors.Null)),
								DecisionFactors.Mult(DecisionFactors.JobEmploymentRate(gjob), 0.025f),
							], factionActions, job));
					}
				} else if (job is ConstructBuildingJob bjob) {
					ephemeralActions.Add(Actions.AssignWorkersToJob([
						DecisionFactors.HasFreeWorkers(factionActions),
						DecisionFactors.OneMinus(DecisionFactors.JobEmploymentRate(bjob)),
					], factionActions, bjob));
				}
			}

			Action chosenAction = Actions.Idle;
			float chosenScore = 0f;
			foreach (var action in mainActions.Concat(ephemeralActions)) {
				var s = action.Score();
				Debug.Assert(!Mathf.IsNaN(s), $"Got NOT A NUMBER from scoring action {action}");
				if (s > chosenScore) {
					chosenScore = s;
					chosenAction = action;
				}
			}
			mstime = Time.GetTicksMsec() - mstime;
			Console.WriteLine($"LocalAI::Update : (of {factionActions}) chose action {chosenAction}!");
			Console.WriteLine($"LocalAI::Update : choosing took {mstime} ms!\n");
			var mstime2 = Time.GetTicksMsec();
			chosenAction.Do();
			mstime2 = Time.GetTicksMsec() - mstime2;
			Console.WriteLine($"LocalAI::Update : (of {factionActions}) did action {chosenAction}!");
			Console.WriteLine($"LocalAI::Update : doing took {mstime2} ms!\n");
		}

		time = minute;
	}

	static class Resources {
		public static readonly IResourceType Logs = Registry.Resources.GetAsset("logs");
		public static readonly IResourceType Rocks = Registry.Resources.GetAsset("rock");

		public static readonly IResourceSiteType Trees = Registry.ResourceSites.GetAsset("trees");
		public static readonly IResourceSiteType Boulder = Registry.ResourceSites.GetAsset("boulder");

		public static readonly IBuildingType LogCabin = Registry.Buildings.GetAsset("log_cabin");
		public static readonly IBuildingType Housing = Registry.Buildings.GetAsset("housing");
		public static readonly IBuildingType BrickHousing = Registry.Buildings.GetAsset("brick_housing");
		public static readonly IBuildingType[] Buildings = [Resources.LogCabin, Resources.Housing, Resources.BrickHousing];
	}

}

public partial class LocalAI {

	public struct Action(DecisionFactor[] factors, System.Action act, string name) {

		public System.Action Do = act;

		public readonly float Score() {
			Console.WriteLine($"LocalAI::Action::Score : \tscoring {this}");
			var mstime = Time.GetTicksMsec();
			var score = 1f;
			foreach (var factor in factors) {
				var s = factor.Call();
				Console.WriteLine($"LocalAI::Action::Score : \t\tutility of {factor} is {s}");
				score *= s;
				if (s <= 0) {
					break;
				}
			}
			Console.WriteLine($"LocalAI::Action::Score : \tscored *{score}* (took {Time.GetTicksMsec() - mstime})\n");
			return score;
		}

		public override readonly string ToString() => name;

	}

	public static class Actions {

		private readonly static DecisionFactor[] NoFactors = [];

		public readonly static Action Idle = new(NoFactors, () => { }, "Idle");

		public static Action AssignWorkersToJob(DecisionFactor[] factors, FactionActions ac, Job job) {
			return new(factors, () => {
				if (!job.NeedsWorkers) return;
				int maxChange = Math.Min(ac.GetFreeWorkers() + job.Workers.Count, job.Workers.Capacity);
				maxChange = Math.Min(maxChange, job.Workers.Capacity - job.Workers.Count);
				ac.ChangeJobWorkerCount(job, maxChange);
			}, $"AssignWorkersToJob({job})");
		}

		public static Action RemoveJob(DecisionFactor[] factors, FactionActions ac, Job job) {
			return new(factors, () => {
				ac.RemoveJob(job);
			}, $"RemoveJob({job})");
		}

		public static Action CreateGatherJob(DecisionFactor[] factors, FactionActions ac, IResourceSiteType siteType) {
			return new(factors, () => {
				foreach (var mop in ac.GetMapObjects()) {
					if (mop is ResourceSite rs) {
						if (rs.Type == siteType && ac.GetMapObjectJobs(rs).Count == 0) {
							var jobdesc = (rs.Type as ResourceSiteType).resourceTypeDescription; // this will breka when not using resources
							ac.AddJob(rs, new GatherResourceJob(jobdesc));
							return;
						}
					}
				}
				Debug.Assert(false, "Didn't find resource site to add job to");
			}, $"GatherFromResourceSite({siteType.AssetName})");
		}

		public static Action PlaceBuildingJob(DecisionFactor[] factors, FactionActions ac, IBuildingType buildingType) {
			return new(factors, () => {
				foreach (var pos in ac.GetTiles()) {
					if (!ac.CanPlaceBuilding(buildingType, pos)) continue;
					ac.PlaceBuilding(buildingType, pos);
					return;
				}
				Debug.Assert(false, "No free tiles left to place building");
			}, $"CreateBuildingJob({buildingType.AssetName})");
		}

	}

}

public partial class LocalAI {

	public readonly struct DecisionFactor(Func<float> call, string name) {

		public readonly Func<float> Call = call;


		public override readonly string ToString() => name;

	}

	static class DecisionFactors {


		public static readonly DecisionFactor Null = new(() => 0.0f, "Null");
		public static readonly DecisionFactor One = new(() => 1.0f, "One");

		public static DecisionFactor OneMinus(DecisionFactor fac) {
			return new(() => 1.0f - fac.Call(), $"OneMinus({fac})");
		}

		public static DecisionFactor Mult(DecisionFactor fac, float with) {
			return new(() => fac.Call() * with, $"({fac} * {with})");
		}

		public static DecisionFactor HomelessnessRate(FactionActions ac) {
			return new(() => Mathf.Clamp(ac.GetHomelessPopulationCount() / 7f, 0f, 1f), "HomelessnessRate");
		}

		public static DecisionFactor HasFreeWorkers(FactionActions ac) {
			return new(() => ac.GetFreeWorkers() > 0 ? 1.0f : 0.0f, "HasFreeWorkers");
		}

		public static DecisionFactor JobEmploymentRate(Job job) {
			return new(() => (float)job.Workers.Count / (float)job.Workers.Capacity, "JobEmploymentRate");
		}

		public static DecisionFactor JobHasEmploymentSpots(FactionActions ac, Job job) {
			return new(() => job.NeedsWorkers && job.Workers.Capacity > job.Workers.Count ? 1.0f : 0.0f, "HasEmploymentSpots");
		}

		public static DecisionFactor JobHasEmployees(Job job) {
			return new(() => job.NeedsWorkers && job.Workers.Count > 0 ? 1.0f : 0.0f, "HasEmployees");
		}

		public static DecisionFactor HasFreeResourceSite(FactionActions ac, IResourceSiteType siteType) {
			return new(() => {
				int matchingSites = 0;
				int matchingFreeSites = 0;
				foreach (var mop in ac.GetMapObjects()) {
					if (mop is ResourceSite rs) {
						if (rs.Type == siteType) {
							matchingSites += 1;
							if (ac.GetMapObjectJobs(rs).Count == 0) {
								matchingFreeSites += 1;
							}
						}
					}
				}
				if (matchingSites == 0) return 0f;
				return (float)matchingFreeSites / (float)matchingSites;
			}, $"HasFreeResourceSite({siteType.AssetName})");
		}

		public static DecisionFactor ResourceWant(FactionActions ac, IResourceType resourceType, int want) {
			return new(() => {
				var res = ac.GetResourceStorage();
				var count = res.GetCount(resourceType);
				var a = Mathf.Max(0f, (float)(want - count) / want);
				a *= a;
				return Mathf.Clamp(a, 0f, 1f);
			}, $"ResourceWant({resourceType.AssetName}, {want})");
		}

		public static DecisionFactor ResourceNeed(FactionActions ac, IResourceType resourceType, int need) {
			return new(() => {
				var res = ac.GetResourceStorage();
				return res.HasEnough(new(resourceType, need)) ? 1f : 0f;
			}, $"ResourceNeed({resourceType.AssetName}, {need})");
		}

	}

}
