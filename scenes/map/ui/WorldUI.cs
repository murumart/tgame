using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		regionTitleLabel.Text = region.Name;
		var things = string.Join(", ", region.GetMapObjects().Select(a => ((IAssetType)a.Type).AssetName).Distinct());
		regionInfoLabel.Text =
			$"Property of: {(region.LocalFaction.HasOwningFaction() ? region.LocalFaction.GetOwningFaction() : "Sovereign")}\n"
			+ $"Land tiles: {region.LandTileCount}\n"
			+ $"Sea tiles: {region.OceanTileCount}\n"
			+ $"Population: {(region.LocalFaction == null ? "Uninhabited" : region.LocalFaction.GetPopulationCount())}\n"
			+ $"Natural Resources: {string.Join(", ", region.NaturalResources.Value.Select(a => a.ToString()))}\n"
			+ $"Map objects: {things}"
		;
		regionPlayButton.Disabled = !region.LocalFaction.HasOwningFaction();
	}

	class AnonymousMapbjectComparer : IEqualityComparer<MapObject> {
		public bool Equals(MapObject x, MapObject y) => x.Type == y.Type;
		public int GetHashCode([DisallowNull] MapObject obj) => obj.GetHashCode();
	}

}
