using System;
using System.Linq;
using Godot;

public partial class WorldUI : Control {

	public event Action RegionPlayRequested;

	[Export] public ResourceDisplay ResourceDisplay;

	[Export] Label regionTitleLabel;
	[Export] RichTextLabel regionInfoLabel;
	[Export] Button regionPlayButton;

	Region selectedRegion; public Region SelectedRegion => selectedRegion;


	public override void _Ready() {
		regionPlayButton.Pressed += () => RegionPlayRequested?.Invoke();
	}

	public void SelectRegion(Region region) {
		if (region == null) {
			regionTitleLabel.Text = "";
			regionInfoLabel.Text = "No region selected...";
			regionPlayButton.Disabled = true;
			selectedRegion = null;
			return;
		}
		selectedRegion = region;
		regionTitleLabel.Text = region.ToString();
		regionInfoLabel.Text =
			$"Land tiles: {region.GroundTiles.Values.Where((a) => a == GroundTileType.Grass).Count()}\n"
			+ $"Sea tiles: {region.GroundTiles.Values.Where((a) => a == GroundTileType.Ocean).Count()}"
		;
		regionPlayButton.Disabled = false;
	}

}
