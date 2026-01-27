using System;
using System.Collections.Generic;
using System.Text;
using Godot;

// abstract jobs

public abstract class Job {

	public virtual string Title => "Some king of job???";
	public virtual bool NeedsWorkers => true;
	public virtual bool IsInternal => false;

	public virtual Group Workers => throw new NotImplementedException("No workers on default job class!");
	public virtual float GetWorkTime(TimeT minutes) => throw new NotImplementedException();

	/// <summary>
	/// Call before adding job. Do things like consume resources here.
	/// </summary>
	/// <param name="ctxFaction">The faction that will own this job</param>
	public abstract void Initialise(Faction ctxFaction);

	public abstract Job Copy();

	/// <summary>
	/// Call before removing job. Can uninitialise things here.
	/// Workers get removed in RegionFaction.
	/// </summary>
	/// <param name="ctxFaction">The faction owning this job</param>
	public abstract void Deinitialise(Faction ctxFaction);

	public abstract void PassTime(TimeT minutes);

	public virtual bool CanInitialise(Faction ctxFaction) => true;
	public virtual void CheckDone(Faction regionFaction) { }

	// sandbox methods vv

	public virtual List<ResourceBundle> GetRequirements() => null;
	//public virtual List<ResourceBundle> GetProductions() => null;

	protected static void ConsumeRequirements(ResourceBundle[] requirements, ResourceStorage resources) {
		foreach (var r in requirements) {
			resources.SubtractResource(r);
		}
	}

	protected static void RefundRequirements(ResourceBundle[] requirements, ResourceStorage resources) {
		var req = requirements.Clone() as ResourceBundle[]; // clone because AddToStorage edits the array

		AddToStorage(req, resources);
	}

	protected static void ProvideProduction(ResourceBundle[] rewards, ResourceStorage resources) {
		var rew = rewards.Clone() as ResourceBundle[]; // clone because AddToStorage edits the array

		AddToStorage(rew, resources);
	}

	// add to the storage one item at a time so we get a bit of every type in storage
	// even if it ends up filling up before we can grant everything
	// TODO this but in a smarter way with less brutish loops
	protected static void AddToStorage(ResourceBundle[] things, ResourceStorage storage) {
		while (true) {
			bool added = false;
			for (int i = 0; i < things.Length; i++) {
				if (!storage.CanAdd(1)) {
					GD.PushWarning("Job rewards can't fit in storage");
					break;
				}
				if (things[i].Amount <= 0) continue;

				storage.AddResource(new ResourceBundle(things[i].Type, 1));
				things[i] = new(things[i].Type, things[i].Amount - 1);
				added = true;
			}
			if (!added) break;
		}
	}

	public virtual string GetResourceRequirementDescription() {
		StringBuilder sb = new();
		var resourceReqs = GetRequirements();
		if (resourceReqs != null) {
			sb.Append("Required Resources:\n");
			foreach (ResourceBundle res in resourceReqs) {
				sb.Append("  ").Append(res.Type.AssetName).Append(" x ").Append(res.Amount).Append('\n');
			}
		}
		return sb.ToString();
	}

	public virtual string GetProductionDescription() => "This job produces nothing.";

	public virtual float GetProgressEstimate() => 0.0f;

	public virtual string GetStatusDescription() => $"This job is {(int)(GetProgressEstimate() * 100)}% done.";

}

public abstract class MapObjectJob : Job {

	public abstract Vector2I GlobalPosition { get; }

	public abstract void Initialise(Faction ctxFaction, MapObject mapObject);
	public override void Initialise(Faction ctxFaction) => throw new NotImplementedException("MapObjectJob requires MapObject argument as well!");

	public virtual bool CanInitialise(Faction ctxFaction, MapObject mapObject) => true;
	public override bool CanInitialise(Faction ctxFaction) => throw new NotImplementedException("MapObjectJob requires MapObject argument as well!");

}

