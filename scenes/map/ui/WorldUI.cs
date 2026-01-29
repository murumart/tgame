using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

public partial class WorldUI : Control {

	public event Action RegionPlayRequested;

	[Export] public ResourceDisplay ResourceDisplay;

	[Export] Label factionTitleLabel;
	[Export] RichTextLabel factionInfoLabel;
	[Export] Button factionPlayButton;

	Region selectedRegion; public Region SelectedRegion => selectedRegion;


	public override void _Ready() {
		factionPlayButton.Pressed += () => RegionPlayRequested?.Invoke();
	}

	public void SelectRegion(Region region) {
		if (region == null) {
			factionTitleLabel.Text = ". . .";
			factionInfoLabel.Text = "Select a Faction";
			factionPlayButton.Disabled = true;
			selectedRegion = null;
			return;
		}
		selectedRegion = region;
		factionTitleLabel.Text = region.LocalFaction.Name;
		var things = string.Join(", ", region.GetMapObjects().Select(a => ((IAssetType)a.Type).AssetName).Distinct());
		factionInfoLabel.Text =
			(region.LocalFaction.HasOwningFaction() ? "Submits to " + region.LocalFaction.GetOwningFaction() : "Sovereign territory") + "\n"
			+ $"Land tiles: {region.LandTileCount}\n"
			+ $"Sea tiles: {region.OceanTileCount}\n"
			+ $"Population: {(region.LocalFaction.GetPopulationCount())}\n"
			+ $"Natural Resources: {string.Join(", ", region.NaturalResources.Value.Select(a => a.ToString()))}\n"
			+ $"Map objects: {things}"
		;
		factionPlayButton.Disabled = !region.LocalFaction.HasOwningFaction();
	}

	class AnonymousMapbjectComparer : IEqualityComparer<MapObject> {
		public bool Equals(MapObject x, MapObject y) => x.Type == y.Type;
		public int GetHashCode([DisallowNull] MapObject obj) => obj.GetHashCode();
	}

}
