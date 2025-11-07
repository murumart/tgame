using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using static Faction;

// abstract jobs

public abstract class Job {

	public virtual string Title => "Some king of job???";
	public virtual bool NeedsWorkers => true;
	public virtual bool IsInternal => false;


	/// <summary>
	/// Call before adding job. Do things like consume resources here.
	/// </summary>
	/// <param name="ctxFaction">The faction that will own this job</param>
	public abstract void Initialise(RegionFaction ctxFaction);

	public abstract Job Copy();

	/// <summary>
	/// Call before removing job. Can uninitialise things here.
	/// Workers get removed in RegionFaction.
	/// </summary>
	/// <param name="ctxFaction">The faction owning this job</param>
	public abstract void Deinitialise(RegionFaction ctxFaction);

	public abstract void PassTime(TimeT minutes);

	public virtual bool CanInitialise(RegionFaction ctxFaction) => true;
	public virtual void CheckDone(RegionFaction regionFaction) { }

	// sandbox methods vv

	public virtual ref Population Workers => throw new NotImplementedException("No workers on default job class!");

	public virtual List<ResourceBundle> GetRequirements() => null;
	public virtual List<ResourceBundle> GetRewards() => null;

	protected static void ConsumeRequirements(List<ResourceBundle> requirements, RegionFaction ctxFaction) {
		foreach (var r in requirements) {
			ctxFaction.Resources.SubtractResource(r);
		}
	}

	protected static void RefundRequirements(List<ResourceBundle> requirements, RegionFaction ctxFaction) {
		foreach (var r in requirements) {
			ctxFaction.Resources.AddResource(r);
		}
	}

	protected static void ProvideRewards(List<ResourceBundle> rewards, RegionFaction ctxFaction) {
		foreach (var r in rewards) {
			ctxFaction.Resources.AddResource(r);
		}
	}

	public virtual string GetResourceRequirementDescription() {
		StringBuilder sb = new();
		var resourceReqs = GetRequirements();
		if (resourceReqs != null) {
			sb.Append("Required Resources:\n");
			foreach (ResourceBundle res in resourceReqs) {
				sb.Append("  ").Append(res.Type.Name).Append(" x ").Append(res.Amount).Append('\n');
			}
		}
		return sb.ToString();
	}


}

public class JobBox : Job {

	private readonly Job job;
	private readonly MapObject attachment;
	private Population jobWorkersCopy;

	public override ref Population Workers => ref jobWorkersCopy;
	public override string Title => job.Title;
	public override bool NeedsWorkers => job.NeedsWorkers;

	public bool IsDeletable => !job.IsInternal;

	public JobBox(Job job) {
		this.job = job;
		this.jobWorkersCopy = job.Workers;
	}

	public JobBox(Job job, MapObject mapObject) : this(job) {
		this.attachment = mapObject;
	}

	public Job Debox() => job;
	public Vector2I Position {
		get {
			Debug.Assert(attachment != null, $"Can't get position of job with no building attachment {job}");
			return attachment.Position;
		}
	}

	public override string GetResourceRequirementDescription() => job.GetResourceRequirementDescription();

	public override void Deinitialise(RegionFaction ctxFaction) => throw new System.NotImplementedException("Don't do these things on a boxed job!!");
	public override bool CanInitialise(RegionFaction ctxFaction) => throw new System.NotImplementedException("Pls Unbox");
	public override Job Copy() => throw new System.NotImplementedException("Don't do these things on a boxed job!!");
	public override void Initialise(RegionFaction ctxFaction) => throw new System.NotImplementedException("Don't do these things on a boxed job!!");
	public override void PassTime(TimeT minutes) => throw new System.NotImplementedException("Don't do these things on a boxed job!!");

}
