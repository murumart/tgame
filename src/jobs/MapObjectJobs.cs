using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using resources.visual;


public class ConstructBuildingJob : MapObjectJob {

	public override string Title => "Construct Building";
	public override bool IsValid => building != null;

	public override Vector2I GlobalPosition => building.GlobalPosition;

	readonly List<ResourceBundle> requirements;

	Building building;


	public ConstructBuildingJob(List<ResourceBundle> requirements) {
		this.requirements = requirements;
		MaxWorkers = 35;
	}

	public override bool CanInitialise(Faction ctxFaction, MapObject building) => ctxFaction.Resources.HasEnoughAll(GetRequirements());

	public override void Initialise(Faction ctxFaction, MapObject mapObject) {
		Debug.Assert(CanInitialise(ctxFaction, mapObject), "Job cannot be initialised!!");
		building = (Building)mapObject;
		ConsumeRequirements(requirements.ToArray(), ctxFaction.Resources);
	}

	public override void Deinitialise(Faction ctxFaction) {
		if (building.IsConstructed) {
			requirements.Clear();
		} else {
			RefundRequirements(requirements.ToArray(), ctxFaction.Resources);
			ctxFaction.RemoveBuilding(building.GlobalPosition - ctxFaction.Region.WorldPosition);
			//building = null;
		}
	}

	public List<ResourceBundle> GetRequirements() => requirements;

	public override void PassTime(TimeT minutes) {
		if (building.IsConstructed) {
			return;
		}
		building.ProgressBuild(minutes, this);
	}

	public override void CheckDone(Faction regionFaction) {
		if (building.IsConstructed) {
			regionFaction.RemoveJob(this);
		}
	}

	public override float GetProgressEstimate() => building.GetBuildProgress();

	public override Job Copy() {
		return new ConstructBuildingJob(requirements);
	}

	public override float GetWorkTime(TimeT minutes) {
		return minutes * Mathf.Pow(Workers, 0.7f);
	}

	public override string GetStatusDescription() {
		float hoursLeft = ((1 - GetProgressEstimate()) * building.Type.GetHoursToConstruct());
		float speed = GetWorkTime(1);
		hoursLeft /= speed;
		float mins = hoursLeft - (int)hoursLeft;
		hoursLeft -= mins;
		GD.Print($"ConstructBuildingJob::GetStatusDescription : {GetProgressEstimate()} {building.Type.GetHoursToConstruct()} {hoursLeft} {speed}");
		var str2 = "The construction will take ";
		if (hoursLeft > 0) str2 += $"{hoursLeft:0} more hours.";
		else str2 += $"{mins * 60:0} more minutes.";
		return str2;
	}

	public override string GetProductionDescription() {
		if (building != null) {
			return $"A {building.Type.AssetName} will be constructed.";
		}
		return "";
	}

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

	public List<ResourceSite.Well> GetProductions() {
		return site.Wells;
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

}

public class CraftJob : MapObjectJob {

	public override string Title => $"{Process.Infinitive.Capitalize()} {Product.Plural}";

	public override Vector2I GlobalPosition => building.GlobalPosition;

	ResourceStorage storage;
	Building building;

	public override bool IsValid => building != null;

	float timeSpent = 0f;

	readonly ResourceBundle[] inputs;
	readonly ResourceBundle[] outputs;
	public readonly Noun Product;
	public readonly Verb Process;
	readonly TimeT timeTaken;


	public CraftJob(ResourceBundle[] inputs, ResourceBundle[] outputs, TimeT timeTaken, uint maxWorkers, Noun product, Verb process) {
		this.inputs = inputs;
		this.outputs = outputs;
		this.timeTaken = timeTaken;
		Product = product;
		Process = process;
		MaxWorkers = maxWorkers;
	}

	public override Job Copy() => new CraftJob(inputs, outputs, timeTaken, MaxWorkers, Product, Process);

	public override void Initialise(Faction ctxFaction, MapObject mapObject) {
		storage = ctxFaction.Resources;
		Debug.Assert(mapObject is Building, "Crafting only happens at buildings? ");
		building = (Building)mapObject;
	}

	public override void Deinitialise(Faction ctxFaction) { }

	public override void PassTime(TimeT minutes) {
		timeSpent += GetWorkTime(minutes);
		while (timeSpent > timeTaken) {
			timeSpent -= timeTaken;
			if (storage.HasEnoughAll(inputs)) {
				storage.SubtractResources(inputs);
				Job.ProvideProduction(outputs, storage);
			}
		}
	}

	public override float GetWorkTime(TimeT minutes) => minutes * MathF.Pow(Workers, 0.5f);

	public override string GetProductionDescription() {
		var sb = new StringBuilder();
		if (Workers == 0) sb.Append($"Add workers to begin {Process.Progressive}...\n");
		else sb.Append($"The workers {Process.Infinitive}...\n");

		GetProductionBulletList(sb);
		if (inputs.Length > 0) {
			sb.Append("...with the required inputs...\n");
			GetInputBulletList(sb);
		}
		sb.Append($"It takes {GameTime.GetFancyTimeString(timeTaken)} to {Process.Infinitive} one set of {Product.Plural}.");

		return sb.ToString();
	}

	public void GetProductionBulletList(StringBuilder sb) {
		foreach (var thing in outputs) {
			sb.Append($" * {thing.Type.AssetName} x {thing.Amount}\n");
		}
	}

	public void GetInputBulletList(StringBuilder sb) {
		foreach (var thing in inputs) {
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