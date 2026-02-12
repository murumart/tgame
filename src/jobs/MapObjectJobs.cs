using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using resources.visual;


public class ConstructBuildingJob : MapObjectJob {

	public override string Title => "Construct Building";
	public override bool IsValid => Building != null;

	public override Vector2I GlobalPosition => Building.GlobalPosition;

	readonly List<ResourceBundle> requirements;

	public Building Building { get; private set; }


	public ConstructBuildingJob(List<ResourceBundle> requirements) {
		this.requirements = requirements;
		MaxWorkers = 35;
	}

	public override bool CanInitialise(Faction ctxFaction, MapObject building) => ctxFaction.Resources.HasEnough(GetRequirements());

	public override void Initialise(Faction ctxFaction, MapObject mapObject) {
		Debug.Assert(CanInitialise(ctxFaction, mapObject), "Job cannot be initialised!!");
		Building = (Building)mapObject;
		ConsumeRequirements(requirements.ToArray(), ctxFaction.Resources);
	}

	public override void Deinitialise(Faction ctxFaction) {
		if (Building.IsConstructed) {
			requirements.Clear();
		} else {
			RefundRequirements(requirements.ToArray(), ctxFaction.Resources);
			ctxFaction.RemoveBuilding(Building.GlobalPosition - ctxFaction.Region.WorldPosition);
			//building = null;
		}
	}

	public List<ResourceBundle> GetRequirements() => requirements;

	public override void PassTime(TimeT minutes) {
		if (Building.IsConstructed) {
			return;
		}
		Building.ProgressBuild(minutes, this);
	}

	public override void CheckDone(Faction regionFaction) {
		if (Building.IsConstructed) {
			regionFaction.RemoveJob(this);
		}
	}

	public override float GetProgressEstimate() => Building.GetBuildProgress();

	public override Job Copy() {
		return new ConstructBuildingJob(requirements);
	}

	public override float GetWorkTime(TimeT minutes) {
		return minutes * Mathf.Pow(Workers, 0.7f);
	}

	public override string GetStatusDescription() {
		float hoursLeft = ((1 - GetProgressEstimate()) * Building.Type.GetHoursToConstruct());
		float speed = GetWorkTime(1);
		hoursLeft /= speed;
		float mins = hoursLeft - (int)hoursLeft;
		hoursLeft -= mins;
		GD.Print($"ConstructBuildingJob::GetStatusDescription : {GetProgressEstimate()} {Building.Type.GetHoursToConstruct()} {hoursLeft} {speed}");
		var str2 = "The construction will take ";
		if (hoursLeft > 0) str2 += $"{hoursLeft:0} more hours.";
		else str2 += $"{mins * 60:0} more minutes.";
		return str2;
	}

	public override string GetProductionDescription() {
		if (Building != null) {
			return $"A {Building.Type.AssetName} will be constructed.";
		}
		return "";
	}

	public override string ToString() => $"ConstructBuildingJob({(Building == null ? "null" : Building.Type.AssetName)})";

}

public class FishByHandJob : Job {

	public override string Title => "Fish by Hand";

	ResourceStorage storage;


	public FishByHandJob() {
		MaxWorkers = 5;
	}

	public override void Deinitialise(Faction ctxFaction) { }

	public override bool CanInitialise(Faction ctxFaction) => true;

	public override Job Copy() => new FishByHandJob();

	public override void Initialise(Faction ctxFaction) {
		storage = ctxFaction.Resources;
	}

	public override void PassTime(TimeT minutes) {
		GD.Print("FishByHandJob::PassTime : We fish x", minutes);
	}

	public override float GetWorkTime(TimeT minutes) {
		throw new NotImplementedException();
	}
}

public class GatherResourceJob : MapObjectJob {

	public override string Title {
		get {
			Debug.Assert(site != null, "Can't get GatherResourceJob Title without site");
			var well = site.Wells[wellIx];
			Debug.Assert(well.Production != null, "Need well's Production to get Title");
			return $"{well.Production.Infinitive.Capitalize()} " + well.ResourceType.AssetName;
		}
	}

	public override Vector2I GlobalPosition {
		get {
			Debug.Assert(site != null, $"Site of GatherResourceJob ({this}) is null!");
			return site.GlobalPosition;
		}
	}
	public override bool IsValid => site != null;

	ResourceStorage storage;
	readonly ResourceSite site;
	readonly int wellIx;

	float timeSpent; // storing as float due to workers
	readonly List<ResourceBundle> grant = new();


	public GatherResourceJob() {
		Debug.Assert(false, "Don't use no parameter GatherResourceJob constructor");
	}

	public ResourceSite.Well GetProduction() {
		Debug.Assert(site != null, "need a site to get production");
		return site.Wells[wellIx];
	}

	public GatherResourceJob(int wellix, ResourceSite site) {
		MaxWorkers = 10;
		wellIx = wellix;
		this.site = site;
	}

	public override Job Copy() => new GatherResourceJob(wellIx, site);

	public override void Initialise(Faction ctxFaction, MapObject mapObject) {
		storage = ctxFaction.Resources;
		Debug.Assert(site == mapObject, $"Constructor and initialisation sites don't match ({site} vs {mapObject})");
		timeSpent = 0f;
	}

	public override void Deinitialise(Faction ctxFaction) { }

	public override float GetWorkTime(TimeT minutes) => minutes * MathF.Pow(Workers, 0.7f);

	public override void PassTime(TimeT minutes) {
		float ts = GetWorkTime(minutes);
		timeSpent += ts;

		var well = site.Wells[wellIx];

		while (timeSpent >= well.MinutesPerBunch && storage.CanAdd(well.BunchSize) && well.HasBunches) {
			timeSpent -= well.MinutesPerBunch;
			well.Deplete();
			grant.Add(new(well.ResourceType, well.BunchSize));
		}

		if (grant.Count != 0) {
			ProvideProduction(grant.ToArray(), storage);
			grant.Clear();
		}
	}

	public override void CheckDone(Faction regionFaction) {
		if (site.Wells[wellIx].HasBunches) return;

		regionFaction.RemoveJob(this);
		// remove a completely depleted resource site
		foreach (var well in site.Wells) if (well.HasBunches) return;
		regionFaction.Uproot(site.GlobalPosition - regionFaction.Region.WorldPosition);
	}

	// display

	public override float GetProgressEstimate() {
		float estimate = 0f;
		var well = site.Wells[wellIx];
		estimate = Math.Max(estimate, timeSpent / well.MinutesPerBunch);
		return estimate;
	}

	public override string GetProductionDescription() {
		if (site == null) {
			return Title;
		}
		Debug.Assert(Workers >= 0, $"Worker count can't be negative (is {Workers})");
		var str = $"";
		var well = site.Wells[wellIx];
		if (storage == null) str += $"Create a job to {well.Production.Infinitive} {well.ResourceType.AssetName}.";
		else if (Workers == 0) str += $"Employ workers to {well.Production.Infinitive} {well.ResourceType.AssetName}.";
		else {
			str = $"{well.Production.Progressive.Capitalize()} ";
			float time = well.MinutesPerBunch / MathF.Max(GetWorkTime(1), 1);
			str += $"{well.BunchSize} {well.ResourceType.AssetName} every {GameTime.GetFancyTimeString((TimeT)time)}.\n";
		}

		return str;
	}

	public override string GetStatusDescription() {
		if (Workers == 0) return "";
		var well = site.Wells[wellIx];
		float timeLeft = well.MinutesPerBunch - timeSpent;
		timeLeft /= GetWorkTime(1);
		return GameTime.GetFancyTimeString((TimeT)timeLeft) + " until more " + well.ResourceType.AssetName + ".";
	}

	public override string ToString() => $"GatherResourceJob({(site != null ? site.Wells[wellIx].ResourceType.AssetName : "?")})";

}

public class CraftJob : MapObjectJob {

	public override string Title => $"{Process.Infinitive.Capitalize()} {Product.Plural}";

	public override Vector2I GlobalPosition => building.GlobalPosition;

	ResourceStorage storage;
	Building building;

	public override bool IsValid => building != null;

	float timeSpent = 0f;

	public readonly ResourceBundle[] Inputs;
	public readonly ResourceBundle[] Outputs;

	public readonly Noun Product;
	public readonly Verb Process;
	readonly TimeT timeTaken;


	public CraftJob(ResourceBundle[] inputs, ResourceBundle[] outputs, TimeT timeTaken, uint maxWorkers, Noun product, Verb process) {
		this.Inputs = inputs;
		this.Outputs = outputs;
		this.timeTaken = timeTaken;
		Product = product;
		Process = process;
		MaxWorkers = maxWorkers;
	}

	public override Job Copy() => new CraftJob(Inputs, Outputs, timeTaken, MaxWorkers, Product, Process);

	public override void Initialise(Faction ctxFaction, MapObject mapObject) {
		storage = ctxFaction.Resources;
		Debug.Assert(mapObject is Building, "Crafting only happens at buildings? ");
		building = (Building)mapObject;
	}

	public override void Deinitialise(Faction ctxFaction) { }

	public override void PassTime(TimeT minutes) {
		if (storage.HasEnough(Inputs)) {
			timeSpent += GetWorkTime(minutes);
			while (timeSpent > timeTaken && storage.HasEnough(Inputs)) {
				timeSpent -= timeTaken;
				storage.SubtractResources(Inputs);
				Job.ProvideProduction(Outputs, storage);
			}
		}
	}

	public override float GetWorkTime(TimeT minutes) => minutes * MathF.Pow(Workers, 0.5f);

	public override string GetProductionDescription() {
		var sb = new StringBuilder();
		if (Workers == 0) sb.Append($"Add workers to begin {Process.Progressive}...\n");
		else sb.Append($"The workers {Process.Infinitive}...\n");

		GetProductionBulletList(sb);
		if (Inputs.Length > 0) {
			sb.Append("...with the required inputs...\n");
			GetInputBulletList(sb);
		}
		sb.Append($"It takes {GameTime.GetFancyTimeString(timeTaken)} to {Process.Infinitive} one set of {Product.Plural}.");

		return sb.ToString();
	}

	public void GetProductionBulletList(StringBuilder sb) {
		foreach (var thing in Outputs) {
			sb.Append($" * {thing.Type.AssetName} x {thing.Amount}\n");
		}
	}

	public void GetInputBulletList(StringBuilder sb) {
		foreach (var thing in Inputs) {
			sb.Append($" * {thing.Type.AssetName} x {thing.Amount}\n");
		}
	}

	public override string GetStatusDescription() {
		if (Workers == 0) return "";
		float timeLeft = timeTaken - timeSpent;
		timeLeft /= GetWorkTime(1);
		return GameTime.GetFancyTimeString((TimeT)timeLeft) + " until more " + Product.Plural + ".";
	}


	public interface ICraftingJobDef {

		public ResourceBundle[] Inputs { get; }
		public ResourceBundle[] Outputs { get; }
		public TimeT TimeTaken { get; }

		public CraftJob GetJob();

	}

}

public class ProcessMarketJob : MapObjectJob {

	public override string Title => "Process Market";

	Building marketplace;
	Faction faction;

	public override Vector2I GlobalPosition => marketplace.GlobalPosition;

	public override bool IsValid => marketplace != null;

	public Faction Faction { get => faction; }

	float timeSpent;
	readonly Dictionary<Faction, List<TradeOffer>> tradeOffers = new();
	public Dictionary<Faction, List<TradeOffer>> TradeOffers {
		get {
			foreach (var (_, l) in tradeOffers) {
				for (int i = l.Count - 1; i > -1; i--) {
					if (!l[i].IsValid) {
						l[i].Log("getting trade offers, removing because it's not valid");
						l.RemoveAt(i);
					}
				}
			}
			return tradeOffers;
		}
	}
	readonly TimeT timeTaken;


	public ProcessMarketJob() {
		MaxWorkers = 10;
		timeTaken = GameTime.Hours(12);
	}

	public override Job Copy() => throw new NotImplementedException();

	public override void Deinitialise(Faction ctxFaction) {
		faction = null;
	}

	public override void Initialise(Faction ctxFaction, MapObject mapObject) {
		faction = ctxFaction;
		Debug.Assert(mapObject is Building b && b.IsConstructed && b.Type.GetSpecial() == Building.IBuildingType.Special.Marketplace, "This map object isn't a marketplace");
		marketplace = mapObject as Building;
	}

	public override float GetWorkTime(TimeT minutes) => minutes * MathF.Pow(Workers, 0.6f);

	public override void PassTime(TimeT minutes) {
		timeSpent += GetWorkTime(minutes);
		while (timeSpent > timeTaken) {
			timeSpent -= timeTaken;
			TryAddTradeOffer();
		}
	}

	void TryAddTradeOffer() {
		var newpartners = faction.GetTradePartners().Where(a => !tradeOffers.ContainsKey(a)).ToArray();
		if (newpartners.Length != 0) {
			var newpartner = newpartners[GD.Randi() % newpartners.Length];
			tradeOffers.Add(newpartner, new());
			faction.GetGottenTradeOffers(newpartner, out var offersenu);
			var offers = offersenu.Where(o => o.IsValid).ToArray();
			if (offers.Length == 0) return;
			tradeOffers[newpartner].Add(offers[GD.Randi() % offers.Length]);
		} else {
			var unaddedOffers = new Dictionary<Faction, TradeOffer[]>();
			var partnersWithUnaddedOffers = faction.GetTradePartners().Where(p => {
				var has = faction.GetGottenTradeOffers(p, out var l);
				var wo = l.Where(o => o.IsValid && !tradeOffers[p].Contains(o)).ToArray();
				unaddedOffers[p] = wo;
				return has && wo.Length != 0;
			}).ToArray();
			if (partnersWithUnaddedOffers.Length == 0) return; // there are no unadded offers
			var partner = partnersWithUnaddedOffers[GD.Randi() % partnersWithUnaddedOffers.Length];
			faction.GetGottenTradeOffers(partner, out var list);
			var withoutExtant = unaddedOffers[partner][GD.Randi() % unaddedOffers[partner].Length];
			tradeOffers[partner].Add(withoutExtant);
		}
	}

	// --------------------------

	public override string GetProductionDescription() {
		return "Trade offers from other factions are processed here before you can accept them.";
	}

	public override float GetProgressEstimate() {
		return timeSpent / timeTaken;
	}

	public override string GetStatusDescription() {
		if (Workers == 0) return "No workers are assigned to process trade.";
		float timeLeft = timeTaken - timeSpent;
		timeLeft /= GetWorkTime(1);
		return GameTime.GetFancyTimeString((TimeT)timeLeft) + " until more trade offers are processed.";
	}

}