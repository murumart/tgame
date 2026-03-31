using System;
using System.Linq;
using Godot;
using scenes.autoload;
using scenes.ui;

namespace scenes.region.ui;

public partial class WarInfoPanel : VBoxContainer {

	[Export] UI ui;
	[Export] Label tutorialLabel;

	[Export] Container ongoingOperationsParent;
	[Export] Container ongoingOperationsContainer;
	[Export] PackedScene jobInfopanelScene;

	[Export] Label ourNameLabel;
	[Export] RichTextLabel ourInfo;

	[Export] Control theirDisplayParent;
	[Export] Label theirNameLabel;
	[Export] RichTextLabel theirInfo;
	[Export] Button aggressionAdjustmentButton;

	[Export] JobInfoPanel attackDisplay;
	[Export] Button attackStartButton;

	[Export] WarEssay essayPrompt;

	Faction us;
	Faction them;

	TileAttackJob attackJob = null;
	Vector2I targetTile;


	public override void _Ready() {
		essayPrompt.WarDeclared += EssaySubmitted;
		aggressionAdjustmentButton.Pressed += AggressionAdjustmentPressed;
		attackStartButton.Pressed += OnAttackStartPressed;
	}

	public void Display(Faction ourFaction) {
		this.us = ourFaction;
		ourNameLabel.Text = us.Name;
		SetThem(null);

		ourInfo.Text = GetDescription(us);
		ui.Camera.SetHighlightDisplay(GetHighlightFunc());
		
		foreach (var n in ongoingOperationsContainer.GetChildren()) n.QueueFree();
		bool found = false;
		foreach (var fac in us.GetMilitaryOperatingAgainst()) {
			var l = new Label() {
				Text = fac.Name,
			};
			found = true;
			ongoingOperationsContainer.AddChild(l);
		}
		ongoingOperationsParent.Visible = found;
	}

	public void Click(Vector2I wpos) {
		targetTile = wpos;
		if (!GameMan.Game.Map.TileOwners.TryGetValue(wpos, out var reg)) return;
		if (reg == us.Region) {
			SetThem(null);
			return;
		}
		SetThem(reg.LocalFaction);
	}

	const string Tut1 = "Select a different faction's tile for further options.";
	const string TutCanStartWar = "Is it a good idea to start a war? Compare stats.";
	const string TutAttackable = "You can start attacking the enemy's borders: click on the green tiles.";
	const string TutStartAttack = "Assign soldiers to begin the attack. Our military might and people are behind them.";
	const string TutNeedWorkers = "You need free soldiers to begin an attack.";
	const string TutFaraway = "This land is unknown to us.";

	void SetThem(Faction them) {
		attackJob = null;

		tutorialLabel.Text = Tut1;
		aggressionAdjustmentButton.Disabled = true;
		aggressionAdjustmentButton.Text = "Diplomacy";
		attackDisplay.Modulate = new(Colors.White, 0.5f);
		attackDisplay.ClearDisplay();
		attackDisplay.Editable = false;
		attackStartButton.Disabled = true;
		attackStartButton.Text = $"({attackStartButton.Shortcut.GetAsText()}) Begin Attack";
		theirDisplayParent.Modulate = new(Colors.White, 0.5f);
		theirNameLabel.Text = "?";
		theirInfo.Text = "?";
		this.them = them;
		if (them is null) {
			ui.Camera.SetHighlightDisplay(GetHighlightFunc());
			return;
		}
		if (!us.Region.Neighbors.Contains(them.Region)) {
			this.them = null;
			ui.Camera.SetHighlightDisplay(GetHighlightFunc());
			tutorialLabel.Text = TutFaraway;
			return;
		}
		ui.Camera.SetHighlightDisplay(GetHighlightFunc());

		bool emptyorwild = them.Population.Count == 0 || them.IsWild;
		theirDisplayParent.Modulate = Colors.White;
		theirNameLabel.Text = them.Name;
		theirInfo.Text = GetDescription(them, us);
		tutorialLabel.Text = TutCanStartWar;

		if (us.IsAtWarWith(them)) {
			aggressionAdjustmentButton.Disabled = us.HasSentPeaceRequestTo(them);
			aggressionAdjustmentButton.Text = !emptyorwild ? "Plead Mercy" : "Stop Operation";
			if (them.HasSentPeaceRequestTo(us)) aggressionAdjustmentButton.Text = "Accept Peace Treaty";
			tutorialLabel.Text = TutAttackable;

			bool attackingTile = FactionActions.IsAttacking(us, them, targetTile, out attackJob);
			if (!attackingTile && FactionActions.CanAttack(us.Region, them.Region, targetTile)) {
				if (ui.GetMaxFreeWorkers() == 0) {
					attackStartButton.Text = TutNeedWorkers;
					return;
				}
				attackJob = FactionActions.GetAttackJob(us, them, targetTile);
				attackDisplay.DisplayPreview(attackJob);
				attackDisplay.Modulate = Colors.White;
				tutorialLabel.Text = TutStartAttack;
				attackStartButton.Disabled = false;
			} else if (attackingTile) {
				uint sliderMax = JobInfoPanel.GetSliderMax(attackJob, ui.GetMaxFreeWorkers());
				attackDisplay.Modulate = Colors.White;
				attackDisplay.Display(ui, attackJob, 0, sliderMax, OnJobWorkerCountChanged);
				attackStartButton.Disabled = false;
				attackStartButton.Text = $"({attackStartButton.Shortcut.GetAsText()}) Stop Attack";
			}
		} else {
			aggressionAdjustmentButton.Disabled = false;
			if (them.IsWild || them.Population.Count == 0) {
				aggressionAdjustmentButton.Text = "Lay Claim";
			} else {
				aggressionAdjustmentButton.Text = "Declare War";
			}
		}
	}

	public void Undisplay() {
		ui.Camera.SetHighlightDisplay(null);
	}

	public void AggressionAdjustmentPressed() {
		if (us.IsAtWarWith(them)) {
			us.RequestPeace(them);
		} else {
			if (them.IsWild || them.Population.Count == 0) {
				us.StartMilitaryOperation(them, "Claiming");
			} else {
				essayPrompt.PopupCentered();
			}
		}
		Rebuild();
	}

	void EssaySubmitted(string reason) {
		us.StartMilitaryOperation(them, reason);
		Rebuild();
	}

	void OnAttackStartPressed() {
		if (attackJob is null) return;
		if (!attackJob.Active) {
			FactionActions.ApplyAttackJob(us, attackJob);
			// it's weird if there's no soldiers to begin with
			int workersToAdd = (int)Math.Min(ui.GetMaxFreeWorkers(), 5);
			if (workersToAdd > 0) ui.ChangeJobWorkerCount(attackJob, workersToAdd);
		} else FactionActions.RemoveAttackingJob(us, attackJob);
		Rebuild();
	}

	void OnJobWorkerCountChanged(int ix, float amount) {
		Debug.Assert(Mathf.Floor(amount) == Mathf.Ceil(amount));
		if (ix == 0) {
			Debug.Assert(attackJob is not null);
			ui.ChangeJobWorkerCount(attackJob, (int)amount);
			Rebuild();
		}
	}

	void Rebuild() {
		var pos = targetTile;
		Display(us);
		Click(pos);
	}

	readonly string BadColorTag = $"[color={Palette.BrownRust.ToHtml()}]";
	readonly string GoodColorTag = $"[color={Palette.BrassGreen.ToHtml()}]";
	const string NoColorTag = "[color=]";

	string GetDescription(Faction fac, Faction us = null) {
		int pop = (int)fac.GetPopulationCount();
		string poptag = NoColorTag;
		if (us is not null) poptag = us.Population.Count > pop
			? GoodColorTag
			: us.Population.Count < pop
				? BadColorTag
				: NoColorTag;

		int mil = fac.Military;
		string miltag = "[color=]";
		if (us is not null) miltag = us.Military > mil
			? GoodColorTag
			: us.Military < mil
				? BadColorTag
				: NoColorTag;

		return ""
			+ $"Population: {poptag}{fac.GetPopulationCount()}[/color]\n"
			+ $"Military Power: {miltag}{fac.Military}[/color]\n"
		;
	}

	RegionDisplayHighlightDisplayFunc GetHighlightFunc() {
		return (s, gb, lp, d) => {
			float alpha = 0.75f;
			Color color = Palette.Dusk;

			if (GameMan.Game.Map.TileOwners.TryGetValue(gb, out var reg)) {
				if (us.IsAtWarWith(reg.LocalFaction)) {
					if (FactionActions.CanAttack(us.Region, reg, gb)) color = Palette.BrassGreen;
					else color = Palette.BrownRust;
				} else if (reg.LocalFaction == us) {
					color = Palette.Pavlova;
				}
			}
			if (reg is not null && reg.LocalFaction == them) color = color.Lightened(0.15f);
			if (gb == targetTile) color = color.Lightened(0.3f);

			alpha *= Mathf.Ease(d, 0.1f);
			s.Modulate = new Color(color, alpha);
		};
	}

}
