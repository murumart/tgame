using System;
using System.Collections.Generic;
using Godot;
using scenes.autoload;

public abstract class Problem {

	public abstract string Title { get; }
	public abstract string SolveJobTitle { get; }
	public abstract string NotificationTitle { get; }

	public readonly TimeT MaxTime;
	public float ProblemTime { get; private set; }

	public Vector2I LocalPosition { get; private set; }

	public bool Applied { get; private set; }
	public SolveProblemJob SolveJob { get; private set; }


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
		Debug.Assert(SolveJob is null, "Already have a job solving this problem");
		SolveJob = solveProblemJob;
	}

	public float GetProgress() {
		return ProblemTime / MaxTime;
	}

	public abstract void OnFailedToAddress(Faction fac);
	public abstract void OnAddressed(Faction fac);

}

public class SolveProblemJob : Job {

	public override string Title => Problem is not null ? Problem.SolveJobTitle : "Solve Problem";
	public override bool NeedsWorkers => true;

	public Problem Problem { get; private set; }


	public SolveProblemJob(Problem problem, uint maxWorkers = 10) {
		Debug.Assert(problem is not null);
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

public class WorkplaceAccidentProblem : Problem {

	readonly string title;
	readonly string solveJobTitle;
	readonly string notificationTitle;

	public override string Title => title;
	public override string SolveJobTitle => solveJobTitle;
	public override string NotificationTitle => notificationTitle;

	readonly Job localJob;


	public WorkplaceAccidentProblem(Vector2I pos, Job accidentJob, string title, string solveJobTitle, string notificationTitle, uint hoursToSolve) : base(pos, GameTime.Hours(hoursToSolve)) {
		this.title = title;
		this.solveJobTitle = solveJobTitle;
		this.notificationTitle = notificationTitle;
		this.localJob = accidentJob;
		accidentJob.Lock();
	}

	public override void OnFailedToAddress(Faction fac) {
		fac.Population.WorkplaceAccident(localJob);
		localJob.Lock(false);
		fac.RemoveJob(localJob);
	}

	public override void OnAddressed(Faction fac) {
		localJob.Lock(false);
	}

}

public class TileAttackJob : Job {

	public override string Title => $"Attack {Target.LocalFaction.Name} at {GlobalPosition}";

	public readonly Vector2I GlobalPosition;
	public readonly Region Target;

	public Faction Attacker {get; private set;}
	public bool Active => Attacker is not null;

	float timeSpent;
	readonly TimeT timeTaken = GameTime.Hours(34);


	public TileAttackJob(Region target, Vector2I globalPos) {
		Debug.Assert(GameMan.Game.Map.TileOwners.GetValueOrDefault(globalPos, null) == target, "Target region doesn't have a tile ehre");
		GlobalPosition = globalPos;
		Target = target;
		MaxWorkers = 100;
	}

	public override bool CanInitialise(Faction ctxFaction) {
		return true;
	}

	public override void Initialise(Faction ctxFaction) {
		Attacker = ctxFaction;
	}

	public override void Deinitialise(Faction ctxFaction) {
		Debug.Assert(ctxFaction == Attacker);
	}

	(float AttackerMil, float DefenderMil) GetMils() => (Attacker.Military, Target.LocalFaction.Military);

	public override float GetWorkTime(TimeT minutes) {
		var (atkmil, defmil) = GetMils();
		float milsum = atkmil + defmil;
		float mypart;
		if (milsum == 0) mypart = 1f;
		else mypart = atkmil / milsum / 0.5f;
		return minutes * Mathf.Pow(Workers, 0.7f) * mypart;
	}

	public override void PassTime(TimeT minutes) {
		timeSpent += GetWorkTime(minutes);
	}

	public override void CheckDone(Faction regionFaction) {
		if (timeSpent < timeTaken) return;
		regionFaction.RemoveJob(this);
		Attacker.Region.AnnexTile(Target, GlobalPosition - Target.WorldPosition, GameMan.Game.Map.TileOwners);
	}

	readonly string[] productions = [
		"misery",
		"more land",
		"power",
		"might",
		"glory",
	];

	public override string GetProductionDescription() {
		return $"This job produces {productions[GD.Randi() % productions.Length]}.";
	}

	public override float GetProgressEstimate() {
		return timeSpent / timeTaken;
	}

	public override string GetStatusDescription() {
		if (!Active) return "Begin the attack to add soldiers.";
		if (Workers == 0) return "Awaiting soldier assignment.";
		float time = timeTaken / MathF.Max(GetWorkTime(1), 1);
		var (atkmil, defmil) = GetMils();
		var str = "";
		if (atkmil > defmil) str = "We have military advantage. ";
		else if (atkmil < defmil) str = "We are at a military disadvantage. ";
		return str + $"At this rate, the tile will be taken over in {GameTime.GetFancyTimeString((TimeT)time)}.";
	}

}

public class MilitaryAttackproblem : Problem {

	public override string Title => "Military attack";
	public override string SolveJobTitle => "Repel attack";
	public override string NotificationTitle => "An attack on our nation! Repel it!";


	public MilitaryAttackproblem(Vector2I pos, TimeT maxTime) : base(pos, maxTime) {
	}

	public override void OnAddressed(Faction fac) {

	}

	public override void OnFailedToAddress(Faction fac) {

	}

}


