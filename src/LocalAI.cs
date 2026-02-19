using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
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

	readonly Dictionary<string, TimeT> lastUsed = new();

	// actions should be shuffled when input
	protected void ChooseAction(out Action chosenAction, out float chosenScore, Span<Action> actions, TimeT currentTime, float idleScore = 0f) {
		chosenAction = Actions.Idle;
		chosenScore = idleScore;
		foreach (ref readonly var action in actions) {
			float delta = currentTime - lastUsed.GetValueOrDefault(action.ToString(), 0);
			float debounce = Mathf.Pow(Mathf.Clamp(delta / (4 * GameTime.MINUTES_PER_HOUR), 0f, 1f), 4);
			float s = action.Score() * debounce;
			if (GameMan.Singleton.Game.PlayRegion == factionActions.Region) Console.WriteLine($"LocalAI::ChooseAction : {action} scored {s}");
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

		public readonly float Score() {
			//Console.WriteLine($"LocalAI::Action::Score : \tscoring {this}");
			ulong ustime = Time.GetTicksUsec();
			float score = 1f;
			foreach (ref readonly DecisionFactor factor in factors.AsSpan()) {
				ulong sustime = Time.GetTicksUsec();
				float s = factor.Score();
				Profile.AddDecisionTime(Time.GetTicksUsec() - sustime, factor.ToString());
				Debug.Assert(!Mathf.IsNaN(s), $"Action {this} Got NOT A NUMBER from scoring decision {factor}");
				//Console.WriteLine($"LocalAI::Action::Score : \t\tutility of {factor} is {s}");
				score *= s;
				if (s <= 0f) {
					break;
				}
			}
			ustime = Time.GetTicksUsec() - ustime;
			Profile.AddActionTime(ustime, name);
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
				int maxChange = Math.Min((int)ac.GetFreeWorkers(), (int)job.MaxWorkers);
				maxChange = Math.Min(maxChange, (int)job.MaxWorkers - job.Workers);
				maxChange = GD.RandRange(maxChange / 2, maxChange);
				ac.ChangeJobWorkerCount(job, maxChange);
			}, $"AssignWorkersToJob({job})");
		}

		public static Action RemoveWorkersFromJob(DecisionFactor[] factors, FactionActions ac, Job job) {
			return new(factors, () => {
				AIAssert(job.NeedsWorkers, "Job doesn't \"need workers\"", ac);
				int maxChange = (int)job.Workers;
				ac.ChangeJobWorkerCount(job, -maxChange);
			}, $"RemoveWorkersFromJob({job})");
		}

		public static Action RemoveJob(DecisionFactor[] factors, FactionActions ac, Job job) {
			return new(factors, () => {
				ac.RemoveJob(job);
			}, $"RemoveJob({job})");
		}

		public static Action AddMapObjectJob(DecisionFactor[] factors, FactionActions ac, MapObject mapObject, MapObjectJob job) {
			return new(factors, () => {
				ac.AddJob(mapObject, job);
			}, $"AddMapObjectJob({job})");
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
			}, $"CreateGatherJob({siteType.AssetName}, {wantedResource.AssetName})");
		}

		public static Action CreateProcessMarketJob(DecisionFactor[] factors, FactionActions ac) {
			return new(factors, () => {
				foreach (var mop in ac.GetMapObjects()) {
					if (mop is Building b) {
						if (b.Type.GetSpecial() == IBuildingType.Special.Marketplace && b.IsConstructed) {
							ac.AddJob(b, new ProcessMarketJob());
							return;
						}
					}
				}
				AIAssert(false, "Didn't find market to add job to", ac);
			}, $"CreateProcessMarketJob()");
		}

		public static Action PlaceBuildingJob(DecisionFactor[] factors, FactionActions ac, IBuildingType buildingType) {
			return new(factors, () => {
				AIAssert(ac.Faction.HasBuildingMaterials(buildingType), "Don't have building materials", ac);
				foreach (var pos in ac.GetTiles()) {
					if (!ac.CanPlaceBuilding(buildingType, pos)) continue;
					ac.PlaceBuilding(buildingType, pos);
					return;
				}
				AIAssert(false, $"No free tiles left to place building {buildingType.AssetName}", ac);
			}, $"PlaceBuildingJob({buildingType.AssetName})");
		}

		// sucks don't use
		public static Action CrappySendTradeOffer(DecisionFactor[] factors, Faction from, Faction to) {
			return new(factors, () => {
				TradeOffer toffer;
				if (false && GD.Randf() < 0.5 && from.Silver > 10) {
					var resources = Registry.Resources.GetAssets();
					IResourceType wantResource = resources[(int)(GD.Randi() % resources.Length)];
					toffer = new(from, (int)(GD.Randi() % (from.Silver - 10)) + 1, to, new(wantResource, (int)(GD.Randi() % 6) + 1), 1);
					from.SendTradeOffer(to, toffer);
				} else {
					KeyValuePair<IResourceType, ResourceStorage.InStorage> excessiveResource = new(null, 0);
					foreach (var res in from.Resources) {
						if (res.Value.Amount > excessiveResource.Value.Amount) excessiveResource = res;
					}
					if (excessiveResource.Value == 0) return; // nothing to give
					var give = new ResourceBundle(excessiveResource.Key, Math.Max(1, excessiveResource.Value / 14));
					toffer = new(from, give, to, give.Amount / (int)(GD.Randf() * 2 + 1) + 1, 1);
					from.SendTradeOffer(to, toffer);
				}
			}, $"CrappySendTradeOffer()");
		}

		public static Action SendTradeOffer(DecisionFactor[] factors, FactionActions ac, int giveSilver, ResourceBundle wantResources) {
			return new(factors, () => {
				AIAssert(ac.Faction.Silver >= giveSilver, "Don't have enough silver to make trade offer", ac);
				var from = ac.Faction;
				var to = from.Region.Neighbors.ToArray()[GD.Randi() % from.Region.Neighbors.Count].LocalFaction;
				from.SendTradeOffer(to, new(from, giveSilver, to, wantResources, 1));
			}, $"SendTradeOffer({giveSilver}, {wantResources})");
		}

		public static Action SendTradeOffer(DecisionFactor[] factors, FactionActions ac, ResourceBundle giveResources, int wantSilver) {
			return new(factors, () => {
				AIAssert(ac.GetResourceStorage().HasEnough(giveResources), "Don't have enough resoucres to make trde offer", ac);
				var from = ac.Faction;
				var to = from.Region.Neighbors.ToArray()[GD.Randi() % from.Region.Neighbors.Count].LocalFaction;
				from.SendTradeOffer(to, new(from, giveResources, to, wantSilver, 1));
			}, $"SendTradeOffer({giveResources}, {wantSilver})");
		}

		public static Action AcceptTradeOffer(DecisionFactor[] factors, Faction me, Faction from, TradeOffer offer) {
			return new(factors, () => {
				me.AcceptTradeOffer(from, offer, offer.StoredUnits);
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
					ac.RemoveJob(job);
				}
			}, "FoodPanicCancelEverything");
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
			return new(() => val, $"{val}");
		}

		public static DecisionFactor Max(DecisionFactor fac, float with) {
			return new(() => Mathf.Max(with, fac.Score()), $"Max({fac}, {with})");
		}

		public static DecisionFactor OneMinus(DecisionFactor fac) {
			return new(() => 1.0f - fac.Score(), $"(1 - {fac})");
		}

		public static DecisionFactor Mult(DecisionFactor fac, float with) {
			return new(() => fac.Score() * with, $"({fac} * {with})");
		}

		public static DecisionFactor Pow(DecisionFactor fac, float up) {
			return new(() => { var a = fac.Score(); return Mathf.Pow(a, up); }, $"({fac}^{up})");
		}

		public static DecisionFactor Group(DecisionFactor[] factors) {
			if (factors.Length == 1) return factors[0];
			return new(() => {
				float score = 1.0f;
				foreach (var fac in factors) {
					score *= fac.Score();
				}
				return score;
			}, $"[{string.Join(", ", factors)}]");
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
				foreach (var pos in ac.GetTiles()) {
					if (ac.CanPlaceBuilding(buildingType, pos)) return 1f;
				}
				return 0f;
			}, $"HasSpotForBuilding({buildingType.AssetName})");
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

		public static DecisionFactor ResourceWant(FactionActions ac, GamerAI ai, IResourceType resourceType) {
			return new(() => {
				int want = ai.ResourceWants.GetValueOrDefault(resourceType, 30);
				var count = ac.GetResourceStorage().GetCount(resourceType);
				var a = Mathf.Max(0f, (float)(want - count) / want);
				a -= (100f - want) / 100f;
				return Mathf.Clamp(a, 0f, 1f);
			}, $"ResourceWant({resourceType.AssetName})");
		}

		public static DecisionFactor ResourceNeed(FactionActions ac, IResourceType resourceType, int need) {
			return new(() => {
				return ac.GetResourceStorage().HasEnough(new ResourceBundle(resourceType, need)) ? 1f : 0f;
			}, $"ResourceNeed({resourceType.AssetName}, {need})");
		}

		public static DecisionFactor ResourcesNeed(FactionActions ac, ResourceBundle[] resources) {
			return new(() => ac.GetResourceStorage().HasEnough(resources) ? 1f : 0f, $"ResourcesNeed({string.Join(", ", resources)})");
		}

		public static DecisionFactor FoodMakingNeed(FactionActions ac) {
			return new(() => {
				var res = ac.Faction.GetFoodUsage();
				return res > ac.Faction.GetFood() ? 1f : 0f;
			}, $"Foodneed()");
		}

		public static DecisionFactor SeeNoInterestInTradeOffer(Faction me, TradeOffer toff, TimeT disintrestTime) {
			return new(() => {
				var timeTaken = me.GetTime() - toff.CreationMinute;
				return Mathf.Clamp((float)timeTaken / disintrestTime, 0f, 1f);
			}, $"SeeNoInterestInTradeOffer");
		}

		public static DecisionFactor CanAcceptTradeOffer(TradeOffer tradeOffer) {
			return new(() => tradeOffer.CanTrade(tradeOffer.StoredUnits) ? 1.0f : 0.0f, $"CanAcceptTradeOffer");
		}

		public static DecisionFactor HasFunctionalMarketplace(FactionActions ac) {
			return new(() => {
				var job = ac.GetProcessMarketJob();
				if (job != null) return 1.0f;
				return 0f;
			}, "HasFunctionalMarketplace");
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

		public static DecisionFactor TradeOfferLimit(Faction me, Faction other, int limit) {
			return new(() => {
				var sent = me.GetSentTradeOffers(other, out var amt);
				if (!sent) return 1.0f;
				return 1f - Mathf.Clamp(amt.Count / (float)limit, 0f, 1f);
			}, $"TradeOfferLimit({limit})");
		}

		public static DecisionFactor EmploymentRate(FactionActions actions) {
			return new(() => actions.Faction.GetPopulationCount() == 0 ? 0f : (float)actions.GetUnemployedPopulationCount() / actions.Faction.GetPopulationCount(), "EmploymentRate");
		}

		public static DecisionFactor ArePeopleStarving(FactionActions actions) {
			return new(() => actions.Faction.Population.ArePeopleStarving ? 1f : 0f, "ArePeopleStarving");
		}
	}

}

// profiling && debugging
public partial class LocalAI {

	public static class Profile {

		static readonly Dictionary<string, List<ulong>> ActionTimes = new();
		static readonly Dictionary<string, List<ulong>> DecisionTimes = new();


		public static void AddActionTime(ulong time, string name) {
			if (!ActionTimes.TryGetValue(name, out var list)) {
				list = new();
				ActionTimes[name] = list;
			}
			list.Add(time);
		}

		public static void AddDecisionTime(ulong time, string name) {
			if (!DecisionTimes.TryGetValue(name, out var list)) {
				list = new();
				DecisionTimes[name] = list;
			}
			list.Add(time);
		}

		static void Ep(Dictionary<string, List<ulong>> timess) {
			if (timess.Count == 0) return; // who care
			var max = timess.MaxBy(kvp => kvp.Value.Max());
			const string eps = "LocalAI::Profile::Ep : ";
			var times = timess.ToList();
			times.Sort((kvp1, kvp2) => kvp1.Value.Average(l => (double)l).CompareTo(kvp2.Value.Average(l => (double)l)));
			foreach (var (name, vals) in times) {
				Console.WriteLine(eps + $"\t{name}:");
				Console.WriteLine(eps + $"\t\tSCORES: {vals.Count}");
				Console.WriteLine(eps + $"\t\tAVG   : {vals.Average(l => (double)l)} us");
				Console.WriteLine(eps + $"\t\tMAX   : {vals.Max()} us");
				Console.WriteLine(eps + $"\t\tTOTAL : {vals.Sum(l => (long)l)} us");
			}
			Console.WriteLine(eps + $"\tOMAX    : {max.Key} {max.Value.Max()} us");

		}

		public static void EndProfiling() {
			const string eps = "LocalAI::Profile::EndProfiling : ";
			Console.WriteLine(eps + "***************");
			Console.WriteLine(eps + "PROFILING ENDED");
			Console.WriteLine(eps + "***************");
			Console.WriteLine(eps + "");
			Console.WriteLine(eps + "******************");
			Console.WriteLine(eps + "ACTION SCORE TIMES");
			Console.WriteLine(eps + "******************");
			Console.WriteLine(eps + "");
			Ep(ActionTimes);
			Console.WriteLine(eps + "***************************");
			Console.WriteLine(eps + "DECISION FACTOR SCORE TIMES");
			Console.WriteLine(eps + "***************************");
			Console.WriteLine(eps + "");
			Ep(DecisionTimes);
			Console.WriteLine(eps + "");
			Console.WriteLine(eps + "***************");
			Console.WriteLine(eps + "      BYE      ");
			Console.WriteLine(eps + "***************");
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

	public readonly Dictionary<IResourceType, int> ResourceWants;
	readonly List<Action> mainActions;
	readonly List<Action> ephemeralActions;
	TimeT time;

	readonly HashSet<IBuildingType> farms = [Registry.BuildingsS.GrainField];
	readonly HashSet<IBuildingType> housing = [Registry.BuildingsS.LogCabin, Registry.BuildingsS.Housing, Registry.BuildingsS.BrickHousing];
	readonly HashSet<IBuildingType> crafting = [Registry.BuildingsS.Windmill, Registry.BuildingsS.Bakery,  Registry.BuildingsS.Kiln];


	public GamerAI(FactionActions actions) : base(actions) {

		this.ResourceWants = new();
		var startActions = new List<Action>();

		// currently only present resource sites are considered, when they change we should reconsider
		foreach (var mo in actions.Region.GetMapObjects()) if (mo is ResourceSite r) {
				foreach (var w in r.Type.GetDefaultWells()) {
					startActions.Add(CreateGatherJob(r.Type, w.ResourceType));
				}
			}
		foreach (var b in Registry.Buildings.GetAssets()) {
			bool isFarm = farms.Contains(b);
			bool isHousing = housing.Contains(b);
			bool isCrafting  = crafting.Contains(b);
			startActions.Add(Actions.PlaceBuildingJob([
				Factors.ResourcesNeed(factionActions, b.GetResourceRequirements()),
				isHousing ? Factors.Group([
					Factors.HomelessnessRate(factionActions),
					Factors.OneMinus(Factors.HousingSlotsPerPerson(factionActions)),
					Factors.HousingCapacity(b),
				]) : isFarm ? Factors.ReasonableBuildingCountByPopulation(factionActions, b, 12f) : Factors.ReasonableBuildingCount(factionActions, b, GD.RandRange(1, 6)),
				isCrafting ? Factors.Group([
					Factors.Group(b.GetCraftJobs().Select(j => Factors.Group(j.Outputs.Select(o => Factors.ResourceWant(factionActions, this, o.Type)).ToArray())).ToArray())
				]) : Factors.One,
				!isHousing && !isFarm && !isCrafting ? Factors.Const(0.001f) : Factors.One,
			], factionActions, b));
		}
		startActions.Add(Actions.PlaceBuildingJob([
			Factors.OneMinus(Factors.HasMarketplace(actions)),
			//Factors.HasSpotForBuilding(factionActions, Registry.BuildingsS.Marketplace),
			Factors.ResourcesNeed(factionActions, Registry.BuildingsS.Marketplace.GetResourceRequirements()),
		], factionActions, Registry.BuildingsS.Marketplace));

		startActions.Add(Actions.CreateProcessMarketJob([
			Factors.OneMinus(Factors.HasFunctionalMarketplace(actions)),
			Factors.HasMarketplace(actions),
			Factors.IsMarketplaceConstructed(actions),
		], actions));
		startActions.Add(Actions.FoodPanicCancelEverything([
			Factors.ArePeopleStarving(actions),
			Factors.EmploymentRate(actions),
		], actions));

		this.mainActions = startActions.ToList();
		this.ephemeralActions = new();
	}

	public override void PreUpdate(TimeT minute) {

		ephemeralActions.Clear();
		foreach (var mopbject in factionActions.GetMapObjects()) {
			if (mopbject is Building building && factionActions.GetMapObjectsJob(mopbject) == null) {
				var aval = building.GetAvailableJobs();
				foreach (var job in aval) {
					if (job is CraftJob craftJob) ephemeralActions.Add(Actions.AddMapObjectJob([
						Factors.Group(craftJob.Outputs.Select(o => Factors.ResourceWant(factionActions, this, o.Type)).ToArray()),
					], factionActions, mopbject, craftJob));
				}
			}
		}
		// assigning workers to job
		foreach (var job in factionActions.GetMapObjectJobs()) {
			List<DecisionFactor> factors = new();
			List<DecisionFactor> negativeFactors = new();
			if (job is GatherResourceJob gjob) {
				var prod = gjob.GetProduction();
				var rwant = Factors.ResourceWant(factionActions, this, prod.ResourceType);
				factors.Add(rwant);
				negativeFactors.Add(Factors.Mult(Factors.Pow(Factors.OneMinus(rwant), 3f), 0.000001f));
			} else if (job is ConstructBuildingJob bjob) {
				factors.Add(Factors.Max(Factors.BuildingProgress(bjob.Building), 0.5f));
			} else if (job is ProcessMarketJob pjob) {
			} else if (job is CraftJob cjob) {
				factors.Add(Factors.Mult(Factors.Group(cjob.Outputs.Select(o => Factors.ResourceWant(factionActions, this, o.Type)).ToArray()), 0.1f));
				factors.Add(Factors.HasFreeWorkers(factionActions));
				var mopject = factionActions.Region.GetMapObject(cjob.GlobalPosition - factionActions.Region.WorldPosition);
				IEnumerable<CraftJob> othersPossible = mopject.GetAvailableJobs().Where(j => j is CraftJob cj && cj.Outputs != cjob.Outputs).Cast<CraftJob>();
				negativeFactors.Add(Factors.Group(othersPossible.Select(p => Factors.Group(p.Outputs.Select(o => Factors.ResourceWant(factionActions, this, o.Type)).ToArray())).ToArray()));
			}
			factors.Add(Factors.Pow(Factors.JobHasEmploymentSpots(factionActions, job), 9f));
			factors.Add(Factors.HasFreeWorkers(factionActions));
			factors.Add(Factors.OneMinus(Factors.JobEmploymentRate(job)));
			if (factors != null) {
				ephemeralActions.Add(Actions.AssignWorkersToJob(factors.ToArray(), factionActions, job));
				ephemeralActions.Add(Actions.RemoveWorkersFromJob([
					Factors.Mult(Factors.OneMinus(Factors.Group(factors.ToArray())), 0.0000001f),
					Factors.JobEmploymentRate(job),
				], factionActions, job));
			}
			if (negativeFactors.Count != 0) {
				ephemeralActions.Add(Actions.RemoveJob(negativeFactors.ToArray(), factionActions, job));
			}
		}
		var marketJob = factionActions.GetProcessMarketJob();
		if (marketJob == null) return;
		foreach (var (partner, toffs) in factionActions.Faction.GetSentTradeOffers()) {
			foreach (var toff in toffs) {
				toff.Log($"ephemeral actions sent to {partner}");
				ephemeralActions.Add(Actions.CancelTradeOffer([
					Factors.HasFunctionalMarketplace(factionActions),
					!toff.OffererGivesRecipientSilver
						? Factors.ResourceWant(factionActions, this, toff.OffererSoldResourcesUnit.Type)
						: Factors.Const(0.0015f),
					Factors.SeeNoInterestInTradeOffer(factionActions.Faction, toff, GameTime.Days(8)),
				], factionActions.Faction, partner, toff));
			}
		}
		foreach (var (partner, toffs) in marketJob.TradeOffers) {
			foreach (var toff in toffs) {
				toff.Log($"ephemeral actions from TradeOffers with {partner}");
				if (!toff.IsValid) GD.PushWarning(toff.History);
				AIAssert(toff.IsValid, "This trade offer isn't valid, delete!!!!!");
				ephemeralActions.Add(Actions.AcceptTradeOffer([
					Factors.HasFunctionalMarketplace(factionActions),
					Factors.CanAcceptTradeOffer(toff),
					toff.OffererGivesRecipientSilver
						? Factors.OneMinus(Factors.ResourceWant(factionActions, this, toff.RecepientRequiredResourcesUnit.Type))
						: Factors.ResourceWant(factionActions, this, toff.OffererSoldResourcesUnit.Type),
				], factionActions.Faction, partner, toff));
				ephemeralActions.Add(Actions.RejectTradeOffer([
					Factors.HasFunctionalMarketplace(factionActions),
					Factors.Mult(Factors.OneMinus(Factors.CanAcceptTradeOffer(toff)), 0.00015f),
				], factionActions.Faction, partner, toff));
			}
		}
	}

	public override void Update(TimeT minute) {
		if (factionActions.Region == GameMan.Singleton.Game.PlayRegion) Console.WriteLine($"LocalAI::Update : (of {factionActions}) doing update");
		var ustime = Time.GetTicksUsec();
		// shuffle and evaluate only 30 actions
		var span = mainActions.Concat(ephemeralActions).OrderBy(_ => GD.Randi()).Take(30).ToArray().AsSpan();
		ChooseAction(out Action chosenAction, out float chosenScore, span, minute, GD.Randf() * 0.000005f);
		ustime = Time.GetTicksUsec() - ustime;
		if (factionActions.Region == GameMan.Singleton.Game.PlayRegion) Console.WriteLine($"LocalAI::Update : (of {factionActions}) chose action {chosenAction} (score {chosenScore})!");
		if (factionActions.Region == GameMan.Singleton.Game.PlayRegion) Console.WriteLine($"LocalAI::Update : choosing took {ustime} us!\n");
		chosenAction.Do();

		time = minute;
	}

	Action CreateGatherJob(IResourceSiteType resourceSiteType, IResourceType resourceType) {
		return Actions.CreateGatherJob([
				Factors.ResourceWant(factionActions, this, resourceType),
				Factors.FreeWorkerRate(factionActions),
				Factors.HasFreeResourceSite(factionActions, resourceSiteType, resourceType),
				Factors.ReasonableGatherJobCount(factionActions, 4, resourceType)
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
