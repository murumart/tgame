//#define PROFILING

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using resources.game.building_types;
using scenes.autoload;
using static Building;
using static ResourceSite;

public abstract partial class LocalAI {

	protected readonly FactionActions factionActions;

	public LocalAI(FactionActions actions) {
		this.factionActions = actions;
	}

	public abstract void PreUpdate(TimeT moment);
	public abstract void Update(TimeT moment);

	public readonly Profiling profiling = new();

	readonly Dictionary<string, TimeT> lastUsed = new();


	// actions should be shuffled when input
	protected void ChooseAction(out Action chosenAction, out float chosenScore, Span<Action> actions, TimeT currentTime, float idleScore = 0f) {
		chosenAction = Actions.Idle;
		chosenScore = idleScore;
		foreach (ref readonly var action in actions) {
			float delta = currentTime - lastUsed.GetValueOrDefault(action.ToString(), 0);
			float debounce = Mathf.Pow(Mathf.Clamp(delta / (4 * GameTime.MINUTES_PER_HOUR), 0f, 1f), 4);
			float s = action.Score(profiling) * debounce;
			//if (GameMan.Game.PlayRegion == factionActions.Region) Console.WriteLine($"LocalAI::ChooseAction : {action} scored {s}");
			if (s > chosenScore) {
				chosenScore = s;
				chosenAction = action;
			}
		}
		lastUsed[chosenAction.ToString()] = currentTime;
	}

}

// actions
public partial class LocalAI {

	public struct Action(DecisionFactor[] factors, System.Action act, string name) {

		public readonly void Do() {
			act();
		}

		public readonly float Score(Profiling profiling) {
			//Console.WriteLine($"LocalAI::Action::Score : \tscoring {this}");
			//ulong ustime = Time.GetTicksUsec();
			float score = 1f;
			foreach (ref readonly DecisionFactor factor in factors.AsSpan()) {
				//if (factor.Equals(Factors.One)) continue;
				//ulong sustime = Time.GetTicksUsec();
				float s = factor.Score();
				//profiling.LogDecisionFactor(Time.GetTicksUsec() - sustime, s, factor.ToString());
				Debug.Assert(!Mathf.IsNaN(s), "got nan");
				//Console.WriteLine($"LocalAI::Action::Score : \t\tutility of {factor} is {s}");
				score *= s;
				if (s <= 0f) {
					break;
				}
			}
			//ustime = Time.GetTicksUsec() - ustime;
			//profiling.LogAction(ustime, score, name);
			//Console.WriteLine($"LocalAI::Action::Score : \tscored *{score}* (took {ustime} us)");
			return score;
		}

		public override readonly string ToString() => name;

	}

	public static class Actions {

		private readonly static DecisionFactor[] NoFactors = [];

		public readonly static Action Idle = new(NoFactors, () => { }, "Idle");

		public static Action AssignWorkersToJob(DecisionFactor[] factors, FactionActions ac, Job job) {
			return new(factors, () => {
				AIAssert(job.NeedsWorkers, "Job doesn't \"need workers\"", ac);
				int maxChange = (int)(job.MaxWorkers * 0.75);
				if (job is ConstructBuildingJob || job is SolveProblemJob) maxChange = (int)job.MaxWorkers;
				maxChange = Math.Min((int)ac.GetFreeWorkers(), maxChange);
				maxChange = Math.Min(maxChange, (int)job.MaxWorkers - job.Workers);
				//if (job is not ConstructBuildingJob)
				maxChange = GD.RandRange(maxChange / 2, maxChange);
				ac.ChangeJobWorkerCount(job, maxChange);
			}, $"AssignWorkersToJob({job})");
		}

		public static Action RemoveWorkersFromJob(DecisionFactor[] factors, FactionActions ac, Job job) {
			return new(factors, () => {
				AIAssert(job.NeedsWorkers, "Job doesn't \"need workers\"", ac);
				int maxChange = (int)job.Workers;
				ac.ChangeJobWorkerCount(job, -maxChange);
			}, "RemoveWorkersFromJob({job})");
		}

		public static Action RemoveJob(DecisionFactor[] factors, FactionActions ac, Job job) {
			return new(factors, () => {
				ac.RemoveJob(job);
			}, "RemoveJob({job})");
		}

		public static Action AddMapObjectJob(DecisionFactor[] factors, FactionActions ac, MapObject mapObject, MapObjectJob job) {
			return new(factors, () => {
				ac.AddJob(mapObject, job);
			}, "AddMapObjectJob({job})");
		}

		public static Action CreateGatherJob(DecisionFactor[] factors, FactionActions ac, IResourceSiteType siteType, IResourceType wantedResource) {
			return new(factors, () => {
				foreach (var mop in ac.GetMapObjects()) {
					if (mop is ResourceSite rs) {
						int wix = 0;
						foreach (var well in rs.Wells) {
							if (rs.Type == siteType && well.ResourceType == wantedResource && well.HasBunches && ac.GetMapObjectsJob(rs) == null) {
								ac.AddJob(rs, new GatherResourceJob(wix, rs));
								return;
							}
							wix++;
						}
					}
				}
				AIAssert(false, "Didn't find resource site to add job to", ac);
			}, "CreateGatherJob({siteType.AssetName}, {wantedResource.AssetName})");
		}

		public static Action CreateProcessMarketJob(DecisionFactor[] factors, FactionActions ac) {
			return new(factors, () => {
				var mp = ac.GetMarketplace();
				AIAssert(mp is not null, "Didn't find market to add job to", ac);
				ac.AddJob(mp, new ProcessMarketJob());
			}, "CreateProcessMarketJob()");
		}

		public static Action PlaceBuildingJob(DecisionFactor[] factors, FactionActions ac, IBuildingType buildingType) {
			return new(factors, () => {
				AIAssert(ac.Faction.HasBuildingMaterials(buildingType), $"Don't have building materials for {buildingType.AssetIDString}", ac);
				foreach (var (pos, _) in ac.GetTiles()) {
					if (!ac.CanPlaceBuilding(buildingType, pos)) continue;
					ac.PlaceBuilding(buildingType, pos);
					return;
				}
				// we're dropping it silentyly, ideally would check (in a way that isn't abysmally slow)1q1141
				//AIAssert(false, $"No free tiles left to place building {buildingType.AssetName}", ac);
			}, "PlaceBuildingJob({buildingType.AssetName})");
		}

		public static Action PlaceQuarryBuildingJob(DecisionFactor[] factors, FactionActions ac, GroundTileType whatground) {
			var btype = Registry.BuildingsS.Quarry;
			return new(factors, () => {
				AIAssert(ac.Faction.HasBuildingMaterials(btype), "Don't have building materials", ac);
				foreach (var (pos, ground) in ac.GetTiles()) {
					if ((ground & whatground) == 0 || !ac.CanPlaceBuilding(btype, pos)) continue;
					ac.PlaceBuilding(btype, pos);
					return;
				}
				AIAssert(false, $"No free tiles left to place building {btype.AssetName}", ac);
			}, "PlaceQuarryBuildingJob({whatground})");
		}

		public static Action SendTradeOffer(DecisionFactor[] factors, FactionActions ac, Faction[] partners, int giveSilver, ResourceBundle wantResources) {
			return new(factors, () => {
				AIAssert(ac.Faction.Silver >= giveSilver, "Don't have enough silver to make trade offer", ac);
				var from = ac.Faction;
				AIAssert(partners.Length > 0, "Need more partner than0", ac);
				var to = partners[GD.Randi() % partners.Length];
				AIAssert(!ac.Faction.IsAtWarWith(to), "Sjpiöldt be at awar", ac);
				from.SendTradeOfferTo(to, new(from, giveSilver, to, wantResources, false));
			}, $"SendTradeOffer(silver -> resources)");
		}

		public static Action SendTradeOffer(DecisionFactor[] factors, FactionActions ac, Faction[] partners, ResourceBundle giveResources, int wantSilver) {
			return new(factors, () => {
				AIAssert(ac.GetResourceStorage().HasEnough(giveResources.Type, giveResources.Amount), "Don't have enough resoucres to make trde offer", ac);
				var from = ac.Faction;
				AIAssert(partners.Length > 0, "Need more partner than0", ac);
				var to = partners[GD.Randi() % partners.Length];
				AIAssert(!ac.Faction.IsAtWarWith(to), "Sjpiöldt be at awar", ac);
				from.SendTradeOfferTo(to, new(from, giveResources, to, wantSilver, false));
			}, $"SendTradeOffer(resources -> silver)");
		}

		public static Action AcceptTradeOffer(DecisionFactor[] factors, Faction me, Faction from, TradeOffer offer, int units = 0) {
			return new(factors, () => {
				me.AcceptTradeOffer(from, offer, units <= 0 ? offer.StoredUnits : units);
			}, $"AcceptTradeOffer()");
		}

		public static Action RejectTradeOffer(DecisionFactor[] factors, Faction me, Faction from, TradeOffer offer) {
			return new(factors, () => {
				me.RejectTradeOffer(from, offer);
			}, $"RejectTradeOffer()");
		}

		public static Action CancelTradeOffer(DecisionFactor[] factors, Faction me, Faction to, TradeOffer offer) {
			return new(factors, () => {
				me.CancelTradeOffer(to, offer);
			}, $"CancelTradeOffer()");
		}

		public static Action FoodPanicCancelEverything(DecisionFactor[] factors, FactionActions ac) {
			return new(factors, () => {
				foreach (var job in ac.GetMapObjectJobs()) {
					if (job is CraftJob j && j.Outputs.Any(o => Registry.ResourcesS.FoodValues.TryGetValue(o.Type, out var _))) continue;
					if (job is GatherResourceJob g && Registry.ResourcesS.FoodValues.TryGetValue(g.GetProduction().ResourceType, out var _)) continue;
					if (job.Locked) continue;
					ac.RemoveJob(job);
				}
			}, "FoodPANIC!!!CancelEverything");
		}

		// hacky way to get more free workers over time instead of mindfully removing them from jobs
		public static Action FreeAWorker(DecisionFactor[] factors, FactionActions ac) {
			return new(factors, () => {
				Job maxj = null;
				int maxworkers = 0;
				foreach (var job in ac.Faction.GetJobs()) {
					if (job.Workers > maxworkers) {
						maxj = job;
						maxworkers = job.Workers;
					}
				}
				if (maxj is null || maxworkers < 2) return;
				AIAssert(maxj is not null && maxworkers != 0, "Bad, ", ac);
				ac.ChangeJobWorkerCount(maxj, -1);
			}, "FreeAWorker");
		}

		static readonly string[] starts = [
			"{0} hereby declares war on {1}.",
			"The mighty Empire of {0} must expand.",
			"The new project of the Empire of {0} is to abolish borders between {0} and {1}. We also aim to abolish {1} itself.",
			"The eons-lasting feud between peoples of {0} and {1} must be stopped at once. So we're gonna invade you.",
			"At this historical moment, {0} no longer recognises the territorial integrity of {1}. Nor the humanity of its people."
		];
		static readonly string[] insults = [
			"And another thing: you're ugly.",
			"Consider this a mercy toward the world...",
			"Your ugly city should be demolished, dug up and paved over, converting it into a fancy beach resort.",
			"We encourage all citizens to wear clown noses so you make a funny noise once we come and stab you.",
			"Your unsightly city will be no big loss.",
			"Your city will be forgotten about by history.",
			"Our peoples shouldn't be divided like this, by a border -- instead, they should live alongside, except yours at like a lower social rank and you'd also serve us and bring us drinks while we lounge.",
			"\nP.S. Your city is really ugly.",
		];
		public static Action DeclareWar(DecisionFactor[] factors, FactionActions ac, Faction target) {
			return new(factors, () => {
				StringBuilder reason = new();
				reason.Append(string.Format(starts[GD.Randi() % starts.Length], ac.Faction.Name, target.Name));
				reason.Append(' ');
				reason.Append(insults[GD.Randi() % insults.Length]);
				ac.Faction.StartMilitaryOperation(target, reason.ToString());
			}, $"DeclareWarWith({target})");
		}

		public static Action SendPeaceRequest(DecisionFactor[] factors, FactionActions ac, Faction target) {
			return new(factors, () => {
				ac.Faction.RequestPeace(target);
			}, $"SendPeaceRequest({ac.Faction},{target})");
		}

		public static Action StartTileInvasion(DecisionFactor[] factors, FactionActions ac, Faction against, Vector2I globalTile) {
			return new(factors, () => {
				int free = Math.Max(0, (int)ac.GetFreeWorkers());
				if (free == 0) return;
				var job = FactionActions.GetAttackJob(ac.Faction, against, globalTile);
				FactionActions.ApplyAttackJob(ac.Faction, job);
				ac.ChangeJobWorkerCount(job, Math.Min(free, Math.Min(5, (int)(GD.Randf() * free))));
			}, $"StartTileInvasion({ac.Faction},{against})");
		}

	}

}

// decision factors
public partial class LocalAI {

	public readonly struct DecisionFactor(Func<float> score, string name) {

		public readonly float Score() {
			return score();
		}

		public override readonly string ToString() => name;

	}

	protected static class Factors {

		public static readonly DecisionFactor Null = new(() => 0.0f, "Null");
		public static readonly DecisionFactor One = new(() => 1.0f, "One");

		public static DecisionFactor Const(float val) {
			return new(() => val, $"C");
		}

		public static DecisionFactor Ease(DecisionFactor fac, float t) {
			return new(() => Mathf.Ease(fac.Score(), t), "Ease({fac}?)");
		}

		public static DecisionFactor Curve(DecisionFactor fac, Curve curve) {
			return new(() => curve.SampleBaked(fac.Score()), "Curve({fac}?)");
		}

		public static DecisionFactor Max(DecisionFactor fac, float with) {
			return new(() => Mathf.Max(with, fac.Score()), "Max({fac}?)");
		}

		public static DecisionFactor OneMinus(DecisionFactor fac) {
			return new(() => 1.0f - fac.Score(), "(1-{fac})");
		}

		public static DecisionFactor Mult(DecisionFactor fac, float with) {
			return new(() => Mathf.Clamp(fac.Score() * with, 0f, 1f), "({fac}*)");
		}

		public static DecisionFactor Pow(DecisionFactor fac, float up) {
			return new(() => { var a = fac.Score(); return Mathf.Pow(a, up); }, "({fac}^)");
		}

		public static DecisionFactor Group(DecisionFactor[] factors) {
			if (factors.Length == 0) return One;
			if (factors.Length == 1) return factors[0];
			return new(() => {
				float score = 1.0f;
				foreach (var fac in factors) {
					score *= fac.Score();
				}
				return score;
			}, "Group");
		}

		public static DecisionFactor HomelessnessRate(FactionActions ac) {
			return new(() => Mathf.Clamp(ac.GetHomelessPopulationCount() / 7f, 0f, 1f), "HomelessnessRate");
		}

		public static DecisionFactor HousingSlotsPerPerson(FactionActions ac) {
			return new(() => {
				IEnumerable<Building> buildings = ac.GetMapObjects().Where(a => a is Building).Select(a => (Building)a);
				int housingSpots = buildings.Sum(b => b.Type.GetPopulationCapacity());
				return Mathf.Clamp((float)housingSpots / (float)ac.Faction.GetPopulationCount(), 0.0f, 1.0f);
			}, "HousingSlotsPerPerson");
		}

		public static DecisionFactor HousingCapacity(IBuildingType buildingType) {
			return new(() => buildingType.GetPopulationCapacity() == 0 ? 0f : Mathf.Clamp(buildingType.GetPopulationCapacity() / 50f, 0f, 1f), $"HousingCapacity({buildingType.AssetName})");
		}

		public static DecisionFactor HasFreeWorkers(FactionActions ac) {
			return new(() => ac.GetFreeWorkers() > 0 ? 1.0f : 0.0f, "HasFreeWorkers");
		}

		public static DecisionFactor HasSpotForBuilding(FactionActions ac, IBuildingType buildingType) {
			return new(() => {
				foreach (var (pos, _) in ac.GetTiles()) {
					if (ac.CanPlaceBuilding(buildingType, pos)) return 1f;
				}
				return 0f;
			}, $"HasSpotForBuilding({buildingType.AssetName})");
		}

		public static DecisionFactor HasSpotForQuarryBuilding(FactionActions ac, GroundTileType whatground) {
			return new(() => {
				foreach (var (pos, ground) in ac.GetTiles()) {
					if ((ground & whatground) != 0 && ac.CanPlaceBuilding(Registry.BuildingsS.Quarry, pos)) return 1f;
				}
				return 0f;
			}, $"HasSpotForQuarryBuilding({whatground})");
		}

		public static DecisionFactor BuildingProgress(Building building) {
			return new(() => building.GetBuildProgress(), "BuildingProgress");
		}

		public static DecisionFactor FreeWorkerRate(FactionActions ac) {
			return new(() => ac.Faction.GetPopulationCount() == 0 ? 0f : (float)ac.GetFreeWorkers() / (float)ac.Faction.GetPopulationCount(), "FreeWorkerRate");
		}

		public static DecisionFactor JobEmploymentRate(Job job) {
			return new(() => (float)job.Workers / (float)job.MaxWorkers, "JobEmploymentRate");
		}

		public static DecisionFactor JobHasEmploymentSpots(FactionActions ac, Job job) {
			return new(() => job.NeedsWorkers && job.Workers < job.MaxWorkers ? 1.0f : 0.0f, "JobHasEmploymentSpots");
		}

		public static DecisionFactor JobHasEmployees(Job job) {
			return new(() => job.NeedsWorkers && job.Workers > 0 ? 1.0f : 0.0f, "JobHasEmployees");
		}

		public static DecisionFactor JobCompletion(Job job) => new(() => Mathf.Clamp(job.GetProgressEstimate(), 0f, 1f), "JobCompletion({job})");

		public static DecisionFactor HasResourceSiteThatProduces(FactionActions ac, IResourceType need) {
			return new(() => {
				foreach (var mop in ac.GetMapObjects()) if (mop is ResourceSite res) {
						foreach (var w in res.Wells) {
							if (w.ResourceType == need && w.HasBunches) return 1f;
						}
					}
				return 0f;
			}, $"HasResourceSiteThatProduces({need.AssetName})");
		}

		public static DecisionFactor HasFreeResourceSite(FactionActions ac, IResourceSiteType siteType, IResourceType need) {
			return new(() => {
				int matchingSites = 0;
				int matchingFreeSites = 0;
				foreach (var mop in ac.GetMapObjects()) {
					if (mop is ResourceSite rs) {
						if (rs.Type == siteType) {
							foreach (var well in rs.Wells) if (well.ResourceType == need && well.HasBunches) {
									matchingSites += 1;
									if (ac.GetMapObjectsJob(rs) == null) {
										matchingFreeSites += 1;
									}
								}
						}
					}
				}
				if (matchingSites == 0) return 0f;
				return (float)matchingFreeSites / (float)matchingSites;
			}, $"HasFreeResourceSite({siteType.AssetName})");
		}

		public static DecisionFactor ReasonableGatherJobCount(FactionActions ac, int count, IResourceType resourceType) {
			return new(() => {
				float jobs = 0;
				foreach (var job in ac.GetMapObjectJobs()) {
					if (job is GatherResourceJob gjob && gjob.GetProduction().ResourceType == resourceType) jobs += 1;
				}
				return 1f - Mathf.Clamp(jobs / count, 0.0f, 1.0f);
			}, $"ReasonableGatherJobCount({count}, {resourceType.AssetName})");
		}

		public static DecisionFactor ReasonableBuildingCount(FactionActions ac, IBuildingType buildingType, int count) {
			return new(() => {
				int bs = 0;
				foreach (var mo in ac.GetMapObjects()) {
					if (mo is Building building && building.Type == buildingType) bs += 1;
				}
				return 1f - Mathf.Clamp(bs / (float)count, 0f, 1f);
			}, $"ReasonableBuildingCount({buildingType.AssetName}, {count})");
		}

		public static DecisionFactor ReasonableBuildingCountByPopulation(FactionActions ac, IBuildingType buildingType, float popForOne) {
			return new(() => {
				int foundBuildings = 0;
				foreach (var mo in ac.GetMapObjects()) {
					if (mo is Building building && building.Type == buildingType) foundBuildings += 1;
				}
				float wantedBuildings = ac.Faction.GetPopulationCount() / popForOne;
				if (wantedBuildings == 0f) return 0f;
				return 1f - Mathf.Clamp(foundBuildings / wantedBuildings, 0f, 1f);
			}, $"ReasonableBuildingCountByPopulation({buildingType.AssetName}, {popForOne})");
		}

		public static DecisionFactor ApprovalRating(FactionActions ac) {
			return new(() => ac.Faction.Population.Approval, "ApprovalRating");
		}

		public static DecisionFactor ApprovalRatingChange(FactionActions ac) {
			return new(() => Mathf.Clamp(ac.Faction.Population.GetApprovalMonthlyChange(), 0f, 1f), "ApprovalRatingChange");
		}

		public static float CalculateWant(int want, int count) {
			//var a = Mathf.Max(0f, (float)(want - count) / want);
			//a -= (100f - want) / 100f;
			float a = (float)want / count;
			return Mathf.Clamp(a, 0f, 1f);
		}

		public static DecisionFactor ResourceWant(FactionActions ac, GamerAI ai, IResourceType resourceType) {
			return new(() => {
				return CalculateWant(ai.ResourceWants.GetValueOrDefault(resourceType, GamerAI.DefaultResourceWant), ac.GetResourceStorage().GetCount(resourceType));
			}, $"ResourceWant({resourceType.AssetName})");
		}

		public static DecisionFactor BuildingWant(FactionActions ac, GamerAI ai, IBuildingType buildingType) {
			return new(() => {
				return CalculateWant(ai.BuildingWants.GetValueOrDefault(buildingType, GamerAI.DefaultResourceWant), ac.GetBuildingCount(buildingType));
			}, $"BuildingWant({buildingType.AssetName})");
		}

		public static DecisionFactor ResourceNeed(FactionActions ac, IResourceType resourceType, int need) {
			return new(() => {
				return ac.GetResourceStorage().HasEnough(resourceType, need) ? 1f : 0f;
			}, $"ResourceNeed({resourceType.AssetName}, {need})");
		}

		public static DecisionFactor ResourcesNeed(FactionActions ac, ResourceConsumer[] resources) {
			return new(() => ac.GetResourceStorage().HasEnough(resources) ? 1f : 0f, $"ResourcesNeed()");
		}

		public static DecisionFactor SilverNeed(FactionActions ac, int amount) {
			Debug.Assert(amount > 0);
			return new(() => {
				if (amount > ac.Faction.Silver) return 0f;
				return Mathf.Clamp((float)(ac.Faction.Silver - amount) / ac.Faction.Silver, 0f, 1f);
			}, "SilverNeed");
		}

		public static DecisionFactor SilverWant() => new(() => 1f, "SilverWant");

		public static DecisionFactor RichnessSpendable(FactionActions ac, int whenRich) {
			return new(() => {
				float silver = ac.Faction.Silver;
				return Mathf.Clamp(silver / whenRich, 0f, 0f);
			}, $"Richness$pendable({whenRich})");
		}

		public static DecisionFactor FoodMakingNeed(FactionActions ac) {
			return new(() => {
				var res = ac.Faction.GetFoodUsage();
				return res > ac.Faction.GetFood() ? 1f : 0f;
			}, $"Foodneed()");
		}

		public static DecisionFactor SeeNoInterestInTradeOffer(Faction me, TradeOffer toff, TimeT disintrestTime) {
			return new(() => {
				var timeTaken = me.GetTime() - toff.LastInteractionMinute;
				return Mathf.Clamp((float)timeTaken / disintrestTime, 0f, 1f);
			}, $"SeeNoInterestInTradeOffer");
		}

		public static DecisionFactor CanAcceptTradeOffer(TradeOffer tradeOffer, int units = 0) {
			return new(() => tradeOffer.CanTrade(units <= 0 ? tradeOffer.StoredUnits : units) ? 1.0f : 0.0f, $"CanAcceptTradeOffer");
		}

		public static DecisionFactor MarketplaceIsBeingWorkedAt(FactionActions ac) {
			return new(() => {
				var job = ac.GetProcessMarketJob();
				if (job != null && job.Workers > 0) return 1.0f;
				return 0f;
			}, "MarketplaceIsBeingWorkedAt");
		}

		public static DecisionFactor HasMarketplace(FactionActions ac) {
			return new(() => {
				if (ac.GetMarketplace() != null) return 1f;
				return 0f;
			}, "HasMarketplace");
		}

		public static DecisionFactor IsMarketplaceConstructed(FactionActions ac) {
			return new(() => {
				var mp = ac.GetMarketplace();
				if (mp.IsConstructed) return 1f;
				return 0f;
			}, "IsMarketplaceConstructed");
		}

		public static DecisionFactor MarketplaceJobExists(FactionActions ac) {
			return new(() => {
				var job = ac.GetProcessMarketJob();
				if (job != null) return 1.0f;
				return 0f;
			}, "MarketplaceJobExists");
		}

		public static DecisionFactor TradeOfferLimit(Faction me, Faction other, int limit) {
			return new(() => {
				var sent = me.GetSentTradeOffers(other, out var amt);
				if (!sent) return 1.0f;
				return 1f - Mathf.Clamp(amt.Count / (float)limit, 0f, 1f);
			}, $"TradeOfferLimit({limit})");
		}

		public static DecisionFactor EmploymentRate(FactionActions actions) {
			return new(() => actions.Faction.GetPopulationCount() == 0 ? 0f : (float)actions.Faction.Population.EmployedCount / actions.Faction.GetPopulationCount(), "EmploymentRate");
		}

		public static DecisionFactor ArePeopleStarving(FactionActions actions) {
			return new(() => actions.Faction.Population.ArePeopleStarving ? 1f : 0f, "ArePeopleStarving");
		}

		public static DecisionFactor SentTradeOfferLimit(FactionActions actions, int limit) {
			return new(() => {
				int sent = actions.Faction.GetSentTradeOffers().Sum(kvp => kvp.Value.Count);
				if (sent == 0f) return 1.0f;
				return 1f - Mathf.Clamp(sent / (float)limit, 0f, 1f);
			}, $"TradeOfferLimit({limit})");
		}

		public static DecisionFactor IsJobUnlocked(Job job) {
			return new(() => job.Locked ? 0f : 1f, "IsJobUnlocked");
		}

		public static DecisionFactor MilitaryMight(FactionActions ac) {
			return new(() => Mathf.Min(1f, ac.Faction.Military / 100f), "MilitaryMight");
		}

		public static DecisionFactor MilitaryBehind(FactionActions ac) {
			return new(() => {
				int maxMil = 0;
				foreach (var n in ac.Region.Neighbors) {
					if (n.LocalFaction.Military > maxMil) maxMil = n.LocalFaction.Military;
				}
				int diffFromMax = ac.Faction.Military - maxMil;
				return Mathf.Clamp(-diffFromMax / 10f, 0f, 1f);
			}, "MilitaryBehind");
		}

		public static DecisionFactor IsAtWarWith(FactionActions ac, Faction fac) {
			return new(() => ac.Faction.IsAtWarWith(fac) ? 1f : 0f, "IsAtWarWith");
		}

		public static DecisionFactor MilitaryAdvantageOver(FactionActions ac, Faction fac) {
			return new(() => {
				float milsum = ac.Faction.Military + fac.Military;
				if (milsum == 0) return 0f;
				float stakes = milsum * 0.1f;
				return Mathf.Clamp((ac.Faction.Military - fac.Military) / milsum * stakes, 0f, 1f);
			}, "MilitaryAdvantageOver");
		}

		public static DecisionFactor AtMultipleWarsAlready(Faction fac) {
			return new(() => {
				int wars = fac.InHowManyWars();
				return Mathf.Clamp(wars * 0.1f, 0f, 1f);
			}, "AtMultipleWarsAlready");
		}

		public static DecisionFactor HaveWeNotSentPeaceRequest(FactionActions ac, Faction to) {
			return new(() => ac.Faction.HasSentPeaceRequestTo(to) ? 0f : 1f, "HaveWeNotSentPeaceRequest");
		}

		public static DecisionFactor HasOtherSentPeaceRequest(FactionActions ac, Faction other) {
			return new(() => other.HasSentPeaceRequestTo(ac.Faction) ? 1f : 0f, "HasOtherSentPeaceRequest");
		}

		public static DecisionFactor IsValidWarTarget(FactionActions ac, Region other) {
			return new(() => ac.Region.Neighbors.Contains(other) && other.OwnedTileCount > 0 ? 1f : 0f, "IsValidWarTarget");
		}
	}

}

// profiling && debugging

public partial class LocalAI {

	public class Profiling {

		readonly Dictionary<string, List<(ulong, float)>> ActionInfo = new();
		readonly static Dictionary<string, List<(ulong, float)>> ClassActionInfo = new();
		readonly Dictionary<string, List<(ulong, float)>> DecisionInfo = new();
		readonly static Dictionary<string, List<(ulong, float)>> ClassDecisionInfo = new();

		public void LogAction(ulong time, float score, string name) {
#if PROFILING
			if (!ActionInfo.TryGetValue(name, out var list)) {
				list = new();
				ActionInfo[name] = list;
			}
			list.Add((time, score));

			if (!ClassActionInfo.TryGetValue(name, out list)) {
				list = new();
				ClassActionInfo[name] = list;
			}
			list.Add((time, score));
#endif
		}

		public void LogDecisionFactor(ulong time, float score, string name) {
#if PROFILING
			if (!DecisionInfo.TryGetValue(name, out var list)) {
				list = new();
				DecisionInfo[name] = list;
			}
			list.Add((time, score));

			if (!ClassDecisionInfo.TryGetValue(name, out list)) {
				list = new();
				ClassDecisionInfo[name] = list;
			}
			list.Add((time, score));
#endif
		}

		public string LastActionInfo() => LastInfo(ActionInfo);
		public string LastDecisionInfo() => LastInfo(DecisionInfo);

		string LastInfo(Dictionary<string, List<(ulong, float)>> timess) {
#if PROFILING
			StringBuilder sb = new();
			var keys = timess.Keys.ToList();
			keys.Sort((k1, k2) => timess[k2].Last().Item2.CompareTo(timess[k1].Last().Item2));
			int longestKey = keys.Count > 0 ? keys?.MaxBy(k => k.Length).Length ?? 0 : 0;
			sb.Append("key".PadRight(longestKey)).Append($"{("score"),-20}{("time"),-10}\n");
			foreach (var k in keys) {
				var (ltime, lscore) = timess[k].Last();
				sb.Append(k.PadRight(longestKey)).Append($"{lscore:0.000000, -20}").Append($"{ltime,-10}\n");
			}
			return sb.ToString();
#else
			return "";
#endif
		}

		static void Ep(Dictionary<string, List<(ulong, float)>> timess) {
#if PROFILING
			if (timess.Count == 0) return; // who care
			var max = timess.MaxBy(kvp => kvp.Value.Max());
			const string eps = "LocalAI::Profile::Ep : ";
			var times = timess.ToList();
			times.Sort((kvp1, kvp2) => kvp1.Value.Average(l => (double)l.Item1).CompareTo(kvp2.Value.Average(l => (double)l.Item1)));
			foreach (var (name, vals) in times) {
				Console.WriteLine(eps + $"\t{name}:");
				Console.WriteLine(eps + $"\t\tSCORES: {vals.Count}");
				Console.WriteLine(eps + $"\t\tAVG   : {vals.Average(l => (double)l.Item1)} us");
				Console.WriteLine(eps + $"\t\tMAX   : {vals.Max()} us");
				Console.WriteLine(eps + $"\t\tTOTAL : {vals.Sum(l => (long)l.Item1)} us");
			}
			Console.WriteLine(eps + $"\tOMAX    : {max.Key} {max.Value.Max()} us");
#endif
		}

		public static void EndProfiling() {
#if PROFILING
			const string eps = "LocalAI::Profile::EndProfiling : ";
			Console.WriteLine(eps + "***************");
			Console.WriteLine(eps + "PROFILING ENDED");
			Console.WriteLine(eps + "***************");
			Console.WriteLine(eps + "");
			Console.WriteLine(eps + "******************");
			Console.WriteLine(eps + "ACTION SCORE TIMES");
			Console.WriteLine(eps + "******************");
			Console.WriteLine(eps + "");
			Ep(ClassActionInfo);
			Console.WriteLine(eps + "***************************");
			Console.WriteLine(eps + "DECISION FACTOR SCORE TIMES");
			Console.WriteLine(eps + "***************************");
			Console.WriteLine(eps + "");
			Ep(ClassDecisionInfo);
			Console.WriteLine(eps + "");
			Console.WriteLine(eps + "***************");
			Console.WriteLine(eps + "      BYE      ");
			Console.WriteLine(eps + "***************");
#endif
		}

	}

	public void AIAssert(bool cond, string msg) {
		AIAssert(cond, msg, factionActions);
	}

	public static void AIAssert(bool cond, string msg, FactionActions fa) {
		Debug.Assert(cond, $"LocalAI of {(fa?.Faction ?? null)} {msg}");
	}

}

public class GamerAI : LocalAI {

	public const int DefaultResourceWant = 10;
	public const int DefaultBuildingWant = 1;
	public readonly Dictionary<IResourceType, int> ResourceWants;
	public readonly Dictionary<IBuildingType, int> BuildingWants;
	public readonly float Militarism = Mathf.Clamp((float)GD.Randfn(0f, 0.15f), 0.0001f, 1f);
	readonly List<Action> mainActions;
	readonly List<Action> ephemeralActions;

	struct Thought {

		public float Traitorousness;
		public float AtWar;
		public float Peace;


		public void Decay(TimeT minutes) {
			AtWar = Mathf.MoveToward(AtWar, 0f, minutes);
			Peace = Mathf.MoveToward(Peace, 0f, minutes);
			Traitorousness = Mathf.MoveToward(Traitorousness, 0f, minutes);
		}

	}

	readonly Dictionary<Faction, Thought> thoughtsAboutFactions = new();

	TimeT time;

	readonly HashSet<IBuildingType> farms = [Registry.BuildingsS.GrainField];
	readonly HashSet<IBuildingType> military = Registry.Buildings.GetAssets().Where(b => b.GetMilitaryBoost() > 0).ToHashSet();

	static readonly Curve sendTradeOfferCurve = GD.Load<Curve>("res://resources/game/ai/send_trade_offer.tres");


	public GamerAI(FactionActions actions) : base(actions) {

		Debug.Assert(sendTradeOfferCurve != null);

		this.ResourceWants = new();
		foreach (var it in Registry.Resources.GetAssets()) ResourceWants[it] = DefaultResourceWant;
		this.BuildingWants = new();
		foreach (var it in Registry.Buildings.GetAssets()) BuildingWants[it] = DefaultBuildingWant;

		var startActions = new List<Action>();
		foreach (var rs in Registry.ResourceSites.GetAssets()) {
			foreach (var w in rs.GetDefaultWells()) {
				startActions.Add(CreateGatherJob(rs, w.ResourceType));
			}
		}
		//foreach (var b in Registry.Buildings.GetAssets()) {
		//	startActions.Add(Actions.PlaceBuildingJob([
		//		Factors.ResourcesNeed(factionActions, b.GetConstructionResources()),
		//		Factors.BuildingWant(actions, this, b),
		//		Factors.ReasonableBuildingCount(factionActions, b, b.GetBuiltLimit() == 0 ? GD.RandRange(1, 99) : b.GetBuiltLimit()),
		//	], factionActions, b));
		//}
		foreach (var bnodekvp in ProductionNet.Locations) {
			if (bnodekvp.Value is not ProductionNet.BuildingNode bnode) continue;
			var b = bnodekvp.Key.MapObjectType as BuildingType;
			Debug.Assert(b is not null);

			bool isCrafting = b.GetCraftJobs().Length > 0;
			bool isQuarry = b.GetSpecial() == IBuildingType.Special.Quarry;

			var rneed = Factors.ResourcesNeed(factionActions, b.GetConstructionResources());
			if (isQuarry) {
				var ground = GroundTileType.HasLand;
				List<IResourceType> gets = [Registry.ResourcesS.Rocks];
				if (bnodekvp.Key.Context == ProductionNet.LocationContext.SandyQuarry) {
					ground |= GroundTileType.HasSand;
					gets.Add(Registry.ResourcesS.Sand);
				}
				startActions.Add(Actions.PlaceQuarryBuildingJob([
					rneed,
					Factors.Group(gets.Select(rt => Factors.ResourceWant(factionActions, this, rt)).ToArray()),
					Factors.HasSpotForQuarryBuilding(factionActions, ground),
					Factors.ReasonableBuildingCount(factionActions, b, b.GetBuiltLimit() == 0 ? GD.RandRange(1, 6) : b.GetBuiltLimit()),
				], factionActions, ground));
				continue;
			}

			startActions.Add(Actions.PlaceBuildingJob([
				rneed,
				Factors.ReasonableBuildingCount(factionActions, b, b.GetBuiltLimit() == 0 ? GD.RandRange(1, 6) : b.GetBuiltLimit()),
				isCrafting ? Factors.Group([
					Factors.Group(b.GetCraftJobs().Select(j => Factors.Group(j.Outputs.Select(o => Factors.ResourceWant(factionActions, this, o.Type)).ToArray())).ToArray())
				]) : Factors.One,
				!isCrafting ? Factors.Const(0.001f) : Factors.One,
			], factionActions, b));
		}
		foreach (var b in Registry.Buildings.GetAssets()) {
			bool isFarm = farms.Contains(b);
			bool isHousing = b.GetPopulationCapacity() > 2;
			bool isMilitary = b.GetMilitaryBoost() > 0;
			if (!isFarm && !isHousing && !isMilitary) continue;

			var rneed = Factors.ResourcesNeed(factionActions, b.GetConstructionResources());
			if (isMilitary) {
				startActions.Add(Actions.PlaceBuildingJob([
					rneed,
					Factors.OneMinus(Factors.MilitaryMight(factionActions)),
					Factors.Max(Factors.MilitaryBehind(factionActions), Militarism),
				], factionActions, b));
				continue;
			}
			startActions.Add(Actions.PlaceBuildingJob([
				rneed,
				isHousing ? Factors.Ease(Factors.Group([
					Factors.HomelessnessRate(factionActions),
					Factors.OneMinus(Factors.HousingSlotsPerPerson(factionActions)),
					Factors.HousingCapacity(b),
				]), 0.1f) : isFarm ? Factors.ReasonableBuildingCountByPopulation(factionActions, b, 18f) : Factors.ReasonableBuildingCount(factionActions, b, b.GetBuiltLimit() == 0 ? GD.RandRange(1, 6) : b.GetBuiltLimit()),
				!isHousing && !isFarm ? Factors.Const(0.001f) : Factors.One,
			], factionActions, b));
		}
		startActions.Add(Actions.PlaceBuildingJob([
			Factors.OneMinus(Factors.HasMarketplace(actions)),
			//Factors.HasSpotForBuilding(factionActions, Registry.BuildingsS.Marketplace),
			Factors.ResourcesNeed(factionActions, Registry.BuildingsS.Marketplace.GetConstructionResources()),
		], factionActions, Registry.BuildingsS.Marketplace));
		startActions.Add(Actions.CreateProcessMarketJob([
			Factors.OneMinus(Factors.MarketplaceJobExists(actions)),
			Factors.HasMarketplace(actions),
			Factors.IsMarketplaceConstructed(actions),
		], actions));
		startActions.Add(Actions.FreeAWorker([
			Factors.Ease(Factors.EmploymentRate(actions), 5),
		], actions));
		startActions.Add(Actions.FoodPanicCancelEverything([
			Factors.ArePeopleStarving(actions),
			Factors.EmploymentRate(actions),
		], actions));

		this.mainActions = startActions.ToList();
		this.ephemeralActions = new();

		actions.Faction.StartedWarWith += (f, r) => {
			if (!thoughtsAboutFactions.TryGetValue(f, out var t)) {
				t = new();
			}
			t.Peace = 0f;
			t.AtWar = GameTime.Days(10);
			thoughtsAboutFactions[f] = t;
		};
		actions.Faction.PulledIntoWarWith += (f, r) => {
			if (!thoughtsAboutFactions.TryGetValue(f, out var t)) {
				t = new();
			}
			if (t.Peace > 0) {
				t.Traitorousness = t.Peace;
				t.Peace = -t.Peace;
			}
			t.AtWar = GameTime.Days(10);
			thoughtsAboutFactions[f] = t;
		};
		actions.Faction.EndedWarWith += (f) => {
			if (!thoughtsAboutFactions.TryGetValue(f, out var t)) {
				t = new();
			}
			t.Peace = GameTime.Days(10);
			thoughtsAboutFactions[f] = t;
		};
	}

	DecisionFactor[] GetCraftJobOutputsDecisionFactorsDivide(CraftJob craftJob) {
		return craftJob.Outputs.Select(o => Factors.Mult(Factors.ResourceWant(factionActions, this, o.Type), (float)o.Amount / craftJob.TimeTaken)).ToArray();
	}

	DecisionFactor[] GetCraftJobOutputsDecisionFactors(CraftJob craftJob) {
		return craftJob.Outputs.Select(o => Factors.ResourceWant(factionActions, this, o.Type)).ToArray();
	}

	public override void PreUpdate(TimeT minute) {

		// adjust thoughts
		foreach (var (fac, iterthought) in thoughtsAboutFactions) {
			var thought = iterthought;
			thought.Decay(1);
			thoughtsAboutFactions[fac] = thought;
		}

		// decay wants over time
		bool starving = factionActions.Faction.Population.ArePeopleStarving;
		foreach (var res in ResourceWants.Keys) {
			int want = ResourceWants[res];
			want = Mathf.Max(DefaultResourceWant, want - (int)(want * 0.025 + 1));
			if (starving) want = 0;

			// and increase want for prerequisites
			var parents = ProductionNet.GetParentMaterials(res).Concat(ProductionNet.GetParentBuildingMaterials(res));
			foreach (var r in parents) ResourceWants[r] = Math.Max(want, ResourceWants[r]);

			// and reduce want if we have it already
			var has = factionActions.GetResourceStorage().GetCount(res);
			want = Math.Max(DefaultResourceWant, want - has);

			ResourceWants[res] = want;
		}
		uint homeless = factionActions.GetHomelessPopulationCount() ;
		if (homeless > 0) {
			// housing materials are good, please
			foreach (var building in Registry.BuildingsS.HousingBuildings) {
				foreach (var material in building.Key.GetConstructionResources()) {
					foreach (var type in material.Types) {
						ResourceWants[type] += (int)(material.Amount * homeless * 0.005);
					}
				}
			}
		}
		var (food, eaten2day) = factionActions.GetFoodAndUsage();
		float daystoeat = food / eaten2day;
		if (daystoeat < 5) {
			// getting food places please
			foreach (var foodit in Registry.ResourcesS.FoodValues) {
				ResourceWants[foodit.Key] += (int)(eaten2day * 0.75 * foodit.Value * ((5f - daystoeat) / 5f));
				if (starving) ResourceWants[foodit.Key] += 10000;
			}
		}
		// gear up!!
		foreach (var ne in factionActions.Region.Neighbors) {
			int mildiff = factionActions.Faction.Military - ne.LocalFaction.Military;
			if (mildiff < 0) ResourceWants[Registry.ResourcesS.IronWeapons] += -mildiff;
			if (factionActions.Faction.IsAtWarWith(ne.LocalFaction)) ResourceWants[Registry.ResourcesS.IronWeapons] += 100;
		}
		// random inspiration to get some crap
		if (GD.Randf() < 0.0005f) {
			var random = Registry.Resources.GetAssets()[GD.Randi() % Registry.Resources.GetAssets().Length];
			ResourceWants[random] += 15000000;
		}

		ephemeralActions.Clear();
		foreach (var mopbject in factionActions.GetMapObjects()) {
			if (mopbject is Building building && factionActions.GetMapObjectsJob(mopbject) == null) {
				var aval = building.GetAvailableJobs();
				foreach (var job in aval) {
					if (job is CraftJob craftJob) ephemeralActions.Add(Actions.AddMapObjectJob([
						Factors.Group(GetCraftJobOutputsDecisionFactors(craftJob)),
					], factionActions, mopbject, craftJob));
					else if (job is QuarryJob qJob) ephemeralActions.Add(Actions.AddMapObjectJob([
						Factors.ResourceWant(factionActions, this, qJob.MineResources),
					], factionActions, mopbject, qJob));
				}
			}
		}
		// assigning workers to job
		List<DecisionFactor> factors = new(10);
		List<DecisionFactor> negativeFactors = new(10);
		foreach (var job in factionActions.Faction.GetJobs()) {
			factors.Clear();
			negativeFactors.Clear();
			if (job is GatherResourceJob gjob) {
				var prod = gjob.GetProduction();
				var rwant = Factors.ResourceWant(factionActions, this, prod.ResourceType);
				factors.Add(rwant);
				negativeFactors.Add(Factors.Mult(Factors.Pow(Factors.OneMinus(rwant), 3f), 0.000001f));
			} else if (job is ConstructBuildingJob bjob) {
				factors.Add(Factors.Max(Factors.BuildingProgress(bjob.Building), 0.5f));
				// something about if the building wouyld produce something we have a resourceneed for?
			} else if (job is ProcessMarketJob pjob) {
			} else if (job is CraftJob cjob) {
				factors.Add(Factors.Mult(Factors.Group(GetCraftJobOutputsDecisionFactors(cjob)), 0.1f));
				factors.Add(Factors.ResourcesNeed(factionActions, cjob.Inputs));
				//var mopject = factionActions.Region.GetMapObject(cjob.GlobalPosition - factionActions.Region.WorldPosition);
				//IEnumerable<CraftJob> othersPossible = mopject.GetAvailableJobs().Where(j => j is CraftJob cj && cj.Outputs != cjob.Outputs).Cast<CraftJob>();
			} else if (job is QuarryJob qjob) {
				factors.Add(Factors.Mult(Factors.ResourceWant(factionActions, this, qjob.MineResources), 1f / qjob.TimeTaken));
			} else if (job is SolveProblemJob sjob) {
				factors.Add(Factors.One);
			} else if (job is TileAttackJob atkjob) {
				factors.Add(Factors.Ease(Factors.FreeWorkerRate(factionActions), 0.2f));
			}
			factors.Add(Factors.JobHasEmploymentSpots(factionActions, job));
			factors.Add(Factors.HasFreeWorkers(factionActions));
			factors.Add(Factors.Ease(Factors.OneMinus(Factors.JobEmploymentRate(job)), 4f));
			factors.Add(Factors.IsJobUnlocked(job));
			if (factors != null) {
				var facarr = factors.ToArray();
				ephemeralActions.Add(Actions.AssignWorkersToJob(facarr, factionActions, job));
				ephemeralActions.Add(Actions.RemoveWorkersFromJob([
					job is not TileAttackJob && job is not SolveProblemJob ? Factors.One : Factors.Null,
					Factors.IsJobUnlocked(job),
					Factors.Ease(Factors.OneMinus(Factors.Group(facarr)), 5),
					Factors.Const(0.001f),
					Factors.OneMinus(Factors.JobCompletion(job)),
					Factors.JobEmploymentRate(job),
					Factors.SentTradeOfferLimit(factionActions, 15),
				], factionActions, job));
			}
			if (negativeFactors.Count != 0) {
				negativeFactors.Add(Factors.IsJobUnlocked(job));
				ephemeralActions.Add(Actions.RemoveJob(negativeFactors.ToArray(), factionActions, job));
			}
		}
		// fuck every body
		foreach (var n in factionActions.Region.Neighbors) {
			if (!factionActions.Faction.IsAtWarWith(n.LocalFaction)) {
				ephemeralActions.Add(Actions.DeclareWar([
					Factors.OneMinus(Factors.IsAtWarWith(factionActions, n.LocalFaction)),
					Factors.Max(Factors.MilitaryAdvantageOver(factionActions, n.LocalFaction), 0.00000005f),
					Factors.Max(Factors.AtMultipleWarsAlready(n.LocalFaction), 0.3f),
					Factors.IsValidWarTarget(factionActions, n),
				], factionActions, n.LocalFaction));
			} else {
				ephemeralActions.Add(Actions.SendPeaceRequest([
					(thoughtsAboutFactions.GetValueOrDefault(n.LocalFaction, new()).Traitorousness > 0 ? Factors.Null : Factors.One),
					Factors.HaveWeNotSentPeaceRequest(factionActions, n.LocalFaction),
					Factors.Max(Factors.HasOtherSentPeaceRequest(factionActions, n.LocalFaction), 0.5f),
					Factors.Const((1f / Militarism) * 0.01f),
					Factors.OneMinus(Factors.Const(Mathf.Clamp(thoughtsAboutFactions[n.LocalFaction].AtWar * Militarism, 0f, 1f))),
				], factionActions, n.LocalFaction));

				var edges = n.GetEdges();
				if (edges.Length == 0) continue;
				var ep = Vector2I.Left;
				for (int xxx = 0; xxx < 20; xxx++) {
					var rp = edges[(int)(GD.Randi() % edges.Length)].Key;
					ep = n.WorldPosition + rp;
					if (!n.GetGroundTile(rp, out var tile) || (tile & GroundTileType.HasLand) == 0) continue;
					if (factionActions.Faction.GetJob(ep, out _)) continue;
					if (!FactionActions.CanAttack(factionActions.Region, n, ep)) continue;
					break;
				}
				if (ep == Vector2I.Left) continue;
				if (!FactionActions.CanAttack(factionActions.Region, n, ep)) continue;
				ephemeralActions.Add(Actions.StartTileInvasion([
					Factors.Ease(Factors.FreeWorkerRate(factionActions), 0.05f),
					Factors.HasFreeWorkers(factionActions),
				], factionActions, n.LocalFaction, ep));
			}
		}

		var marketJob = factionActions.GetProcessMarketJob();
		if (marketJob == null || marketJob.Workers == 0) return;
		var tradePartners = marketJob.TradeOffers.Keys.ToArray();
		AIAssert(!tradePartners.Any(f => factionActions.Faction.IsAtWarWith(f)), "Should not have any warring going on in trade pardners", factionActions);
		if (tradePartners.Length == 0) return; // don't know any partners yet

		// creating trade offeers
		// if we have a bunch of something and don't really want it, try to sel, it
		foreach (var (res, store) in factionActions.GetResourceStorage()) {
			int inStorage = store.Amount;
			if (inStorage < 30) continue;
			int srand1 = (int)(GD.Randi() % 3) + 1;
			int tradeAmount = (int)(inStorage * 0.5 + (GD.Randf() * inStorage * 0.5));
			int silver = tradeAmount / (srand1 + 2) + srand1;
			if (silver == 0) continue;
			ephemeralActions.Add(Actions.SendTradeOffer([
				Factors.OneMinus(Factors.Mult(Factors.ResourceWant(factionActions, this, res), tradeAmount)),
				Factors.SilverWant(),
				Factors.Const(0.36f),
			], factionActions, tradePartners, new ResourceBundle(res, tradeAmount), silver));
		}
		// if we have a really bad need for something try to send an expensive offer for it
		foreach (var (res, want) in ResourceWants) {
			float wantf = Mathf.Ease(Factors.CalculateWant(want, factionActions.GetResourceStorage().GetCount(res)), 4.9f);
			if (GD.Randf() < wantf && factionActions.Faction.Silver > 0) {
				int unitprice = Mathf.Max(1, (int)(wantf * GD.Randfn(4, 2)) + 1);
				int cansend = factionActions.Faction.Silver / unitprice;
				if (cansend == 0) continue;
				ephemeralActions.Add(Actions.SendTradeOffer([
					Factors.One,
				], factionActions, tradePartners, unitprice * cansend, new(res, cansend)));
			}
		}
		// remoing trade offers that we've sent and not gotten a response on
		foreach (var (partner, toffs) in factionActions.Faction.GetSentTradeOffers()) {
			foreach (var toff in toffs) {
				toff.Log($"ephemeral actions sent to {partner}");
				ephemeralActions.Add(Actions.CancelTradeOffer([
					!toff.OffererGivesRecipientSilver
						? Factors.ResourceWant(factionActions, this, toff.OffererSoldResourcesUnit.Type)
						: Factors.Const(0.0015f),
					Factors.SeeNoInterestInTradeOffer(factionActions.Faction, toff, GameTime.Days(8)),
					Factors.Mult(Factors.OneMinus(Factors.SentTradeOfferLimit(factionActions, 15)), 0.3f),
				], factionActions.Faction, partner, toff));
			}
		}
		// looking at trade offer's we've gotten and procedssed in marketplace
		foreach (var (partner, toffs) in marketJob.TradeOffers) {
			foreach (var toff in toffs) {
				toff.Log($"ephemeral actions from TradeOffers with {partner}");
				if (!toff.IsValid) GD.PushWarning(toff.History);
				AIAssert(toff.IsValid, "This trade offer isn't valid, delete!!!!!");

				int maxunits = toff.GetMaxUnitsTradeable();
				if (maxunits != 0) {
					DecisionFactor[] extraFactors;
					if (toff.OffererGivesRecipientSilver) {
						extraFactors = [
							Factors.Ease(Factors.OneMinus(Factors.ResourceWant(factionActions, this, toff.RecepientRequiredResourcesUnit.Type)), 0.2f),
						];
					} else {
						// we pay silver
						float want = ResourceWants[toff.OffererSoldResourcesUnit.Type] / (float)toff.RecipientPaidSilverUnit;
						extraFactors = [
							Factors.Const(Mathf.Clamp(want / 100f, 0f, 1f)),
						];
					}

					ephemeralActions.Add(Actions.AcceptTradeOffer([
						//Factors.MarketplaceIsBeingWorkedAt(factionActions), // don't need this, are checking above
						//Factors.CanAcceptTradeOffer(toff), // know we can since we specify maxunits
						Factors.Group(extraFactors),
						//toff.OffererGivesRecipientSilver
						//	? Factors.OneMinus(Factors.ResourceWant(factionActions, this, toff.RecepientRequiredResourcesUnit.Type))
						//	: Factors.ResourceWant(factionActions, this, toff.OffererSoldResourcesUnit.Type),
					], factionActions.Faction, partner, toff, maxunits));
				}
				ephemeralActions.Add(Actions.RejectTradeOffer([
					Factors.Mult(Factors.OneMinus(Factors.CanAcceptTradeOffer(toff)), 0.0015f),
					toff.OffererGivesRecipientSilver
						? Factors.Const(0.5f)
						: Factors.OneMinus(Factors.ResourceWant(factionActions, this, toff.OffererSoldResourcesUnit.Type)),
				], factionActions.Faction, partner, toff));
			}
		}
	}

	public override void Update(TimeT minute) {
		//if (factionActions.Region == GameMan.Game.PlayRegion) Console.WriteLine($"LocalAI::Update : (of {factionActions}) doing update");
		//var ustime = Time.GetTicksUsec();
		// shuffle and evaluate only 30 actions
		var span = mainActions.Concat(ephemeralActions).OrderBy(_ => GD.Randi()).Take(30).ToArray().AsSpan();
		ChooseAction(out Action chosenAction, out float chosenScore, span, minute, GD.Randf() * 0.000005f);
		//ustime = Time.GetTicksUsec() - ustime;
		if (factionActions.Region == GameMan.Game.PlayRegion) Console.WriteLine($"LocalAI::Update : (of {factionActions}) chose action {chosenAction} (score {chosenScore})!");
		//if (factionActions.Region == GameMan.Game.PlayRegion) Console.WriteLine($"LocalAI::Update : choosing took {ustime} us!\n");
		chosenAction.Do();

		time = minute;
	}

	Action CreateGatherJob(IResourceSiteType resourceSiteType, IResourceType resourceType) {
		return Actions.CreateGatherJob([
				Factors.ResourceWant(factionActions, this, resourceType),
				Factors.FreeWorkerRate(factionActions),
				Factors.HasFreeResourceSite(factionActions, resourceSiteType, resourceType),
				Factors.ReasonableGatherJobCount(factionActions, 4, resourceType),
			], factionActions, resourceSiteType, resourceType);
	}

}

public class NatureAI : LocalAI {

	readonly Action[] actions;


	public NatureAI(FactionActions factionActions) : base(factionActions) {
		this.actions = [
			Actions.Idle,
		];
	}

	public override void PreUpdate(TimeT moment) {
	}

	public override void Update(TimeT moment) {
		ChooseAction(out var chosenAction, out _, actions.AsSpan(), moment);
		chosenAction.Do();
	}

}
