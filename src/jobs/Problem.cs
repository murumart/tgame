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

	public Faction Attacker { get; private set; }
	public bool Active => Attacker is not null;

	float timeSpent;
	readonly TimeT timeTaken = GameTime.Hours(16);


	public TileAttackJob(Region target, Vector2I globalPos) {
		GlobalPosition = globalPos;
		Target = target;
		MaxWorkers = 100;
		Debug.Assert(GameMan.Game.Map.TileOwners.GetValueOrDefault(globalPos, null) == target, "Target region doesn't have a tile ehre: TileOwners");
		Debug.Assert(target.GetGroundTile(globalPos - target.WorldPosition, out _), $"Target region doesnä't have a tile {globalPos}.");
		Debug.Assert(Target.GetEdge(GlobalPosition - Target.WorldPosition, out _), $"Target {Target} doesnä't have an edge at {GlobalPosition}.");
	}

	public override bool CanInitialise(Faction ctxFaction) {
		return true;
	}

	public override void Initialise(Faction ctxFaction) {
		Attacker = ctxFaction;
	}

	public override void Deinitialise(Faction ctxFaction) {
		Debug.Assert(ctxFaction == Attacker);
		Attacker = null;
	}

	(float AttackerMil, float DefenderMil) GetMils() => (Attacker.Military, Target.LocalFaction.Military);

	public override float GetWorkTime(TimeT minutes) {
		var (atkmil, defmil) = GetMils();
		float mypart;
		if (atkmil < defmil) mypart = 1f / (defmil - atkmil);
		else if (atkmil > defmil) mypart = Mathf.Ease((atkmil - defmil) / (atkmil + defmil), 3f) + 1f;
		else mypart = 1f;
		return minutes * Mathf.Pow(Workers, 0.7f) * mypart;
	}

	public override void PassTime(TimeT minutes) {
		timeSpent += GetWorkTime(minutes);
	}

	public override void CheckDone(Faction regionFaction) {
		var atk = regionFaction.Region;
		if (!Target.GetEdge(GlobalPosition - Target.WorldPosition, out var edge) // inside the other faction now
			|| (edge.Above != atk && edge.Below != atk && edge.ToLeft != atk && edge.ToRight != atk) // blocked by another acquisition
		) {
			// we got cut off!!
			// maybe even delete the workers in here?
			//GD.Print("REMOVED ", ToMoreDescriptiveString());
			FactionActions.RemoveAttackingJob(regionFaction, this);
			return;
		}
		if (Workers == 0) {
			FactionActions.RemoveAttackingJob(regionFaction, this);
			return;
		}
		{
		//	Debug.Assert(Target.GetEdge(GlobalPosition - Target.WorldPosition, out var dge), $"Target {Target} doesnä't have an edge at {GlobalPosition}.");
		//	Job j = null;
		//	(bool isbad, Region culprit) bad = (false, null);
		//	bad = (bad.isbad || (dge.Above?.LocalFaction != regionFaction && (dge.Above?.LocalFaction.GetJob(GlobalPosition, out j) ?? false) && j is TileAttackJob), dge.Above);
		//	if (bad.isbad) {
		//		Debug.Assert(false, $"It's Bad: target {Target} is already being attacked at {GlobalPosition} by {bad.culprit}");
		//		GD.Print($"It's Bad: target {Target} is already being attacked at {GlobalPosition} by {bad.culprit}, removing job of {regionFaction} {ToMoreDescriptiveString()}");
		//		FactionActions.RemoveAttackingJob(regionFaction, this);
		//		return;
		//	}
		//	bad = (bad.isbad || (dge.Below?.LocalFaction != regionFaction && (dge.Below?.LocalFaction.GetJob(GlobalPosition, out j) ?? false) && j is TileAttackJob), dge.Below);
		//	if (bad.isbad) {
		//		Debug.Assert(false, $"It's Bad: target {Target} is already being attacked at {GlobalPosition} by {bad.culprit}");
		//		GD.Print($"It's Bad: target {Target} is already being attacked at {GlobalPosition} by {bad.culprit}, removing job of {regionFaction} {ToMoreDescriptiveString()}");
		//		FactionActions.RemoveAttackingJob(regionFaction, this);
		//		return;
		//	}
		//	bad = (bad.isbad || (dge.Left?.LocalFaction != regionFaction && (dge.Left?.LocalFaction.GetJob(GlobalPosition, out j) ?? false) && j is TileAttackJob), dge.Left);
		//	if (bad.isbad) {
		//		Debug.Assert(false, $"It's Bad: target {Target} is already being attacked at {GlobalPosition} by {bad.culprit}");
		//		GD.Print($"It's Bad: target {Target} is already being attacked at {GlobalPosition} by {bad.culprit}, removing job of {regionFaction} {ToMoreDescriptiveString()}");
		//		FactionActions.RemoveAttackingJob(regionFaction, this);
		//		return;
		//	}
		//	bad = (bad.isbad || (dge.Right?.LocalFaction != regionFaction && (dge.Right?.LocalFaction.GetJob(GlobalPosition, out j) ?? false) && j is TileAttackJob), dge.Right);
		//	if (bad.isbad) {
		//		Debug.Assert(false, $"It's Bad: target {Target} is already being attacked at {GlobalPosition} by {bad.culprit}");
		//		GD.Print($"It's Bad: target {Target} is already being attacked at {GlobalPosition} by {bad.culprit}, removing job of {regionFaction} {ToMoreDescriptiveString()}");
		//		FactionActions.RemoveAttackingJob(regionFaction, this);
		//		return;
		//	}
		}
		Debug.Assert(GameMan.Game.Map.TileOwners.TryGetValue(GlobalPosition, out var reg) && reg == Target, $"Tile {GlobalPosition} somehow slipped from {Target}");
		if (timeSpent < timeTaken) return;
		Attacker.Region.AnnexTile(Target, GlobalPosition - Target.WorldPosition, GameMan.Game.Map.TileOwners);
		FactionActions.RemoveAttackingJob(regionFaction, this);
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

	public override string ToString() => $"TileAttackJob";

	public string ToMoreDescriptiveString() {
		return $"{GlobalPosition},{Target},{Attacker}";
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


