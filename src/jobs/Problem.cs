using System;
using Godot;

public abstract class Problem {

	public abstract string Title { get; }
	public abstract string SolveJobTitle { get; }

	public readonly TimeT MaxTime;
	public float ProblemTime { get; private set; }

	public Vector2I LocalPosition { get; private set; }

	public bool Applied { get; private set; }
	public SolveProblemJob Job { get; private set; }


	protected Problem(Vector2I pos, TimeT maxTime) {
		LocalPosition = pos;
		MaxTime = maxTime;
	}

	public void IncreaseProblem(TimeT minutes) {
		ProblemTime += minutes;
	}

	public void DecreaseProblem(float minutes) {
		ProblemTime -= minutes;
	}

	public void CheckDone(Faction fac) {
		if (!Applied && ProblemTime >= MaxTime) {
			OnFailedToAddress(fac);
			Applied = true;
			fac.RemoveProblem(LocalPosition);
		}
	}

	public void SetJob(SolveProblemJob solveProblemJob) {
		Debug.Assert(Job is null, "Already have a job solving this problem");
		Job = solveProblemJob;
	}

	public float GetProgress() {
		return ProblemTime / MaxTime;
	}

	public abstract void OnFailedToAddress(Faction fac);

}

public class SolveProblemJob : Job {

	public override string Title => Problem is not null ? Problem.SolveJobTitle : "Solve Problem";
	public override bool NeedsWorkers => true;

	public Problem Problem { get; private set; }


	public SolveProblemJob(Problem problem, uint maxWorkers = 10) {
		this.Problem = problem;
		MaxWorkers = maxWorkers;
	}

	public override void Initialise(Faction ctxFaction) {
		Debug.Assert(Problem is not null);
		Problem.SetJob(this);
	}
	public override void Deinitialise(Faction ctxFaction) {
		if (Problem.ProblemTime <= 0) {
			ctxFaction.RemoveProblem(Problem.LocalPosition);
		}
	}

	public override float GetWorkTime(TimeT minutes) {
		return minutes * 1.2f * Mathf.Pow(Workers, 0.5f);
	}

	public override void PassTime(TimeT minutes) {
		Problem.DecreaseProblem(GetWorkTime(minutes));
	}

	public override bool CanInitialise(Faction ctxFaction) {
		return true;
	}

	public override float GetProgressEstimate() {
		return 1f - Problem.GetProgress();
	}

	public override void CheckDone(Faction regionFaction) {
		if (Problem.ProblemTime <= 0) {
			regionFaction.RemoveJob(this);
		}
	}

}

public class FishingBoatProblem : Problem {

	public override string Title => "Tipped Boat";
	public override string SolveJobTitle => "Save boatmen";

	readonly GatherResourceJob fishingJob;


	public FishingBoatProblem(Vector2I pos, GatherResourceJob fishingJob) : base(pos, GameTime.Hours(3)) {
		this.fishingJob = fishingJob;
	}

	public override void OnFailedToAddress(Faction fac) {
		fac.Population.WorkplaceAccident(fishingJob);
		fac.RemoveJob(fishingJob);
	}

}