using System.Collections.Generic;
using Godot;
using resources.game.resource_site_types;
using static ResourceSite;

public partial class LocalAI {

	FactionActions factionActions;
	List<Action> actions;

	TimeT time;


	public LocalAI(FactionActions actions) {
		this.factionActions = actions;
		this.actions = new() {
			new GatherFromResourceSiteJobAction([new DecisionFactor.ResourceWant(Registry.Resources.GetAsset("logs"), 30)], Registry.ResourceSites.GetAsset("trees")),
			new GatherFromResourceSiteJobAction([new DecisionFactor.ResourceWant(Registry.Resources.GetAsset("rock"), 30)], Registry.ResourceSites.GetAsset("boulder")),
		};
	}

	public void Update(TimeT minute) {
		var delta = minute - time;

		GD.Print($"LocalAI::Update : (of {factionActions}) doing update");

		for (int i = 0; i < 5; i++) {
			Action chosenAction = null;
			float chosenScore = -1f;
			foreach (var action in actions) {
				var s = action.Score(factionActions);
				if (s > chosenScore) {
					chosenScore = s;
					chosenAction = action;
				}
			}
			GD.Print($"LocalAI::Update : (of {factionActions}) chose action {chosenAction}");
			chosenAction.Do(factionActions);
			GD.Print($"LocalAI::Update : (of {factionActions}) did action {chosenAction}");
		}


		time = minute;
	}



}

public partial class LocalAI {

	abstract class Action {

		DecisionFactor[] factors;


		public Action(DecisionFactor[] factors) {
			this.factors = factors;
		}

		public float Score(FactionActions factionActions) {
			GD.Print($"LocalAI::Action::Score : \tscoring {this}");
			var score = 1f;
			foreach (var factor in factors) {
				var s = factor.Score(factionActions);
				GD.Print($"LocalAI::Action::Score : \t\tconsidering utility of {factor} ({s})");
				score *= s;
			}
			GD.Print($"LocalAI::Action::Score : \tscored {score}");
			return score;
		}

		public abstract void Do(FactionActions ac);

	}

	class AssignWorkersToJobAction : Action {


		public AssignWorkersToJobAction(DecisionFactor[] factors) : base(factors) { }

		public override void Do(FactionActions ac) {
			foreach (var mop in ac.GetMapObjects()) {
				foreach (var job in ac.GetMapObjectJobs(mop)) {
					if (job.IsInternal) continue;
					if (!job.NeedsWorkers) continue;
				}
			}
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
		}

	}

}

public partial class LocalAI {

	abstract class DecisionFactor {

		public abstract float Score(FactionActions ac);


		public class HomelessnessRate : DecisionFactor {

			public override float Score(FactionActions ac) {
				return Mathf.Clamp(ac.GetHomelessPopulationCount() / 7f, 0f, 1f);
			}

		}

		public class ResourceWant(IResourceType resourceType, int want) : DecisionFactor {

			public override float Score(FactionActions ac) {
				var res = ac.GetResourceStorage();
				var count = res.GetCount(resourceType);
				var a = Mathf.Max(0f, (float)(want - count) / want);
				a *= a;
				return Mathf.Clamp(a, 0f, 1f);
			}

		}

		public class ResourceNeed(IResourceType resourceType, int need) : DecisionFactor {

			public override float Score(FactionActions ac) {
				var res = ac.GetResourceStorage();
				return res.HasEnough(new(resourceType, need)) ? 1f : 0f;
			}

		}

	}

}
