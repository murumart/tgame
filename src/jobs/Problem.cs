using Godot;

public abstract class Problem {

	public abstract string Title { get; }

	public readonly TimeT MaxTime;
	public float ProblemTime { get; private set; }

	public Vector2I LocalPosition { get; private set; }

	bool applied = false;


	protected Problem(Vector2I pos) {
		LocalPosition = pos;
	}

	public void IncreaseProblem(TimeT minutes) {
		ProblemTime += minutes;
	}

	public void DecreaseProblem(float minutes) {
		ProblemTime -= minutes;
	}

	public void CheckDone(Faction fac) {
		if (!applied && ProblemTime >= MaxTime) {
			OnFailedToAddress(fac);
			applied = true;
		}
	}

	public abstract void OnFailedToAddress(Faction fac);

}

public class SolveProblemJob : Job {

	public override string Title => $"Solve {problem.Title}";
	public override bool NeedsWorkers => true;

	readonly Problem problem;


	public SolveProblemJob(Problem problem, uint maxWorkers = 10) {
		this.problem = problem;
		MaxWorkers = maxWorkers;
	}

	public override void Initialise(Faction ctxFaction) { }
	public override void Deinitialise(Faction ctxFaction) {
		if (problem.ProblemTime <= 0) {
			ctxFaction.RemoveProblem(problem.LocalPosition);
		}
	}

	public override float GetWorkTime(TimeT minutes) {
		return minutes * 2;
	}

	public override void PassTime(TimeT minutes) {
		problem.DecreaseProblem(GetWorkTime(minutes));
	}

	public override bool CanInitialise(Faction ctxFaction) {
		return true;
	}

	public override float GetProgressEstimate() {
		return (problem.MaxTime - problem.ProblemTime) / problem.MaxTime;
	}

	public override void CheckDone(Faction regionFaction) {
		if (problem.ProblemTime <= 0) {
			regionFaction.RemoveJob(this);
		}
	}

}

public class FishingBoatProblem : Problem {

	public override string Title => "Tipped Boat";

	readonly GatherResourceJob fishingJob;


	public FishingBoatProblem(Vector2I pos, GatherResourceJob fishingJob) : base(pos) {
		this.fishingJob = fishingJob;
	}

	public override void OnFailedToAddress(Faction fac) {
		throw new System.NotImplementedException();
	}

}