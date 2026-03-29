using Godot;
using scenes.autoload;
using scenes.ui;

namespace scenes.region.ui;

public partial class WarInfoPanel : VBoxContainer {

	[Export] UI ui;
	[Export] Label tutorialLabel;

	[Export] Label ourNameLabel;
	[Export] RichTextLabel ourInfo;

	[Export] Control theirDisplayParent;
	[Export] Label theirNameLabel;
	[Export] RichTextLabel theirInfo;
	[Export] Button aggressionAdjustmentButton;

	[Export] Control attackDisplay;
	[Export] JobSlider attackJobSlider;

	[Export] WarEssay essayPrompt;

	Faction us;

	Faction them;


	public override void _Ready() {
		essayPrompt.WarDeclared += EssaySubmitted;
		aggressionAdjustmentButton.Pressed += AggressionAdjustmentPressed;
	}

	public void Display(Faction ourFaction) {
		this.us = ourFaction;
		ourNameLabel.Text = us.Name;
		SetThem(null);

		ourInfo.Text = GetDescription(us);
		ui.Camera.RegionDisplayHighlight.SetDisplay(GetHighlightFunc());
	}

	public void Click(Vector2I wpos) {
		if (!GameMan.Game.Map.TileOwners.TryGetValue(wpos, out var reg)) return;
		if (reg == us.Region) {
			SetThem(null);
			return;
		}
		SetThem(reg.LocalFaction);
	}

	void SetThem(Faction them) {
		attackDisplay.Hide();
		aggressionAdjustmentButton.Disabled = true;
		aggressionAdjustmentButton.Text = "Diplomacy";
		this.them = them;
		theirDisplayParent.Modulate = new(Colors.White, 0.5f);
		theirNameLabel.Text = "?";
		theirInfo.Text = "?";
		if (them is null) return;
		theirNameLabel.Text = them.Name;
		theirInfo.Text = GetDescription(them, us);
		if (us.IsAtWarWith(them)) {
			aggressionAdjustmentButton.Disabled = false;
			aggressionAdjustmentButton.Text = "Plead Mercy";
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
		ui.Camera.RegionDisplayHighlight.SetDisplay(null);
		ui.Camera.RegionDisplayHighlight.TransparentiseAll();
	}

	public void AggressionAdjustmentPressed() {

	}

	void EssaySubmitted(string reason) {
		us.DeclareWarOn(them, reason);
		Display(us);
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
			float alpha = 0.5f;
			Color color = Palette.Dark;

			if (GameMan.Game.Map.TileOwners.TryGetValue(gb, out var reg)) {
				if (us.IsAtWarWith(reg.LocalFaction)) {
					color = Palette.BrownRust;
					if (reg.GetEdge(gb - reg.WorldPosition, out var edge)) {
						if (edge.Above == us.Region || edge.Below == us.Region || edge.Left == us.Region || edge.Right == us.Region) {
							color = Palette.BrassGreen;
						}
					}
				} else if (reg.LocalFaction == us) {
					color = Palette.Hoki;
				}
			}

			alpha *= Mathf.Ease(d, 0.5f);
			s.Modulate = new Color(color, alpha);
		};
	}

}
