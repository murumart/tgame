using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.game.resource_site_types;
using static ResourceSite;

using DecisionFactor = System.Func<float>;

public partial class LocalAI {

	readonly FactionActions factionActions;
	readonly Action[] mainActions;
	readonly List<Action> ephemeralActions;

	readonly Dictionary<IResourceType, DecisionFactor> resourceWants;

	TimeT time;


	public LocalAI(FactionActions actions) {
		this.factionActions = actions;
		this.resourceWants = new() {
			{Registry.Resources.GetAsset("logs"), DecisionFactors.GetResourceWant(actions, Registry.Resources.GetAsset("logs"), 30)},
			{Registry.Resources.GetAsset("rock"), DecisionFactors.GetResourceWant(actions, Registry.Resources.GetAsset("rock"), 30)},
		};
		this.mainActions = [
			new GatherFromResourceSiteJobAction(
				[
					resourceWants[Registry.Resources.GetAsset("logs")],
					DecisionFactors.GetHasFreeResourceSite(actions, Registry.ResourceSites.GetAsset("trees")),
				],
				Registry.ResourceSites.GetAsset("trees")),
			new GatherFromResourceSiteJobAction(
				[
					resourceWants[Registry.Resources.GetAsset("rock")],
					DecisionFactors.GetHasFreeResourceSite(actions, Registry.ResourceSites.GetAsset("boulder")),
				],
				Registry.ResourceSites.GetAsset("boulder")),
		];
		this.ephemeralActions = new();
	}

	public void Update(TimeT minute) {
		var delta = minute - time;

		GD.Print($"LocalAI::Update : (of {factionActions}) doing update");

		for (int i = 0; i < 5; i++) {
			ephemeralActions.Clear();
			foreach (var job in factionActions.GetMapObjectJobs()) {
				if (job is GatherResourceJob gjob) {
					foreach (var prod in gjob.GetProductions()) {
						var decFacs = new List<DecisionFactor>();
						ephemeralActions.Add(new AssignWorkersToJobAction(
							[
								resourceWants.GetValueOrDefault(prod.ResourceType, DecisionFactors.Null),
								DecisionFactors.GetHasFreeWorkers(factionActions),
							],
							job
						));
						ephemeralActions.Add(new RemoveWorkersFromJobAction(
							[
								DecisionFactors.OneMinus(resourceWants.GetValueOrDefault(prod.ResourceType, DecisionFactors.Null)),
								//DecisionFactors.OneMinus(DecisionFactors.GetHasFreeWorkers(factionActions)),
							],
							job
						));
					}
				}
			}

			Action chosenAction = null;
			float chosenScore = -1f;
			foreach (var action in mainActions.Concat(ephemeralActions)) {
				var s = action.Score();
				if (s > chosenScore) {
					chosenScore = s;
					chosenAction = action;
				}
			}
			GD.Print($"LocalAI::Update : (of {factionActions}) chose action {chosenAction}!");
			chosenAction.Do(factionActions);
			GD.Print($"LocalAI::Update : (of {factionActions}) did action {chosenAction}!\n");
		}

		time = minute;
	}

}

public partial class LocalAI {

	abstract class Action {

		readonly DecisionFactor[] factors;


		public Action(DecisionFactor[] factors) {
			this.factors = factors;
		}

		public float Score() {
			GD.Print($"LocalAI::Action::Score : \tscoring {this}");
			var score = 1f;
			foreach (var factor in factors) {
				var s = factor();
				GD.Print($"LocalAI::Action::Score : \t\tutility of {factor} ({s})");
				score *= s;
			}
			GD.Print($"LocalAI::Action::Score : \tscored *{score}*\n");
			return score;
		}

		public abstract void Do(FactionActions ac);

	}

	class AssignWorkersToJobAction : Action {

		Job job;


		public AssignWorkersToJobAction(DecisionFactor[] factors, Job job) : base(factors) {
			this.job = job;
		}

		public override void Do(FactionActions ac) {
			if (!job.NeedsWorkers) return;
			int maxChange = Math.Min(ac.GetFreeWorkers() + job.Workers.Count, job.Workers.Capacity);
			ac.ChangeJobWorkerCount(job, maxChange);
		}

	}

	class RemoveWorkersFromJobAction : Action {

		Job job;


		public RemoveWorkersFromJobAction(DecisionFactor[] factors, Job job) : base(factors) {
			this.job = job;
		}

		public override void Do(FactionActions ac) {
			if (!job.NeedsWorkers) return;
			int maxChange = job.Workers.Count;
			ac.ChangeJobWorkerCount(job, -maxChange);
		}

	}

	class GatherFromResourceSiteJobAction : Action {

		public IResourceSiteType SiteType;


		public GatherFromResourceSiteJobAction(DecisionFactor[] factors, IResourceSiteType siteType) : base(factors) {
			SiteType = siteType;
		}

		public override void Do(FactionActions ac) {
			foreach (var mop in ac.GetMapObjects()) {
				if (mop is ResourceSite rs) {
					if (rs.Type == SiteType && ac.GetMapObjectJobs(rs).Count == 0) {
						var jobdesc = (rs.Type as ResourceSiteType).resourceTypeDescription; // this will breka when not using resources
						ac.AddJob(rs, new GatherResourceJob(jobdesc));
						return;
					}
				}
			}
			Debug.Assert(false, "Didn't find resource site to add job to");

		}

	}

}

public partial class LocalAI {

	static class DecisionFactors {

		public static readonly DecisionFactor Null = () => 0.0f;
		public static readonly DecisionFactor One = () => 1.0f;

		public static DecisionFactor OneMinus(DecisionFactor fac) {
			return () => 1.0f - fac();
		}

		public static DecisionFactor GetHomelessnessRate(FactionActions ac) {
			return () => Mathf.Clamp(ac.GetHomelessPopulationCount() / 7f, 0f, 1f);
		}

		public static DecisionFactor GetHasFreeWorkers(FactionActions ac) {
			return () => ac.GetFreeWorkers() > 0 ? 1.0f : 0.0f;
		}

		public static DecisionFactor GetHasFreeResourceSite(FactionActions ac, IResourceSiteType siteType) {
			return () => {
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
				return (float)matchingFreeSites / (float)matchingSites;
			};
		}

		public static DecisionFactor GetResourceWant(FactionActions ac, IResourceType resourceType, int want) {
			return () => {
				var res = ac.GetResourceStorage();
				var count = res.GetCount(resourceType);
				var a = Mathf.Max(0f, (float)(want - count) / want);
				a *= a;
				return Mathf.Clamp(a, 0f, 1f);
			};
		}

		public static DecisionFactor GetResourceNeed(FactionActions ac, IResourceType resourceType, int need) {
			return () => {
				var res = ac.GetResourceStorage();
				return res.HasEnough(new(resourceType, need)) ? 1f : 0f;
			};
		}

	}

}
