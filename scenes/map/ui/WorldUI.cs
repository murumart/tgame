using System;
using System.Linq;
using Godot;

namespace scenes.map.ui;

public partial class WorldUI : Control {

	public event Func<Vector2I, (float, float, float)> WorldTileInfoRequested;
	public event Func<Vector2I, Region> RegionRequested;
	public event Action RegionPlayRequested;

	[Export] public ResourceDisplay ResourceDisplay;
	[Export] Camera camera;
	[Export] Control factionPanel;
	[Export] Label factionTitleLabel;
	[Export] RichTextLabel factionInfoLabel;
	[Export] Button factionPlayButton;

	Region selectedRegion;
	public Region SelectedRegion => selectedRegion;


	public override void _Ready() {
		factionPlayButton.Pressed += () => RegionPlayRequested?.Invoke();
		factionPanel.GuiInput += _GuiInput;

		camera.ClickedMouseEvent += MouseClicked;

		ResourceDisplay.Display(c => {
			if (!camera.IsInsideTree()) (c as Label).Text =  "...";
			var mousePos = (Vector2I)camera.GetMousePos();
			if (mousePos != oldMousePos) {
				oldMousePos = mousePos;
				var tileInfo =  WorldTileInfoRequested?.Invoke(mousePos) ?? (-4f, -4f, -4f);
				oldTileInfo = tileInfo;
			}
			(c as Label).Text = $"ele: {oldTileInfo.Item1} temp: {oldTileInfo.Item2} humi: {oldTileInfo.Item3}";
		});
		ResourceDisplay.DisplayFat();
	}

	Vector2 oldMousePos;
	(float, float, float) oldTileInfo;
	public override void _Process(double delta) {
		ResourceDisplay.Display();
	}

	void MouseClicked(Vector2I where) {
		var region = RegionRequested?.Invoke(where) ?? null;
		if (region == null) return;
		SelectRegion(region);
	}

	public override void _UnhandledKeyInput(InputEvent evt) {
		if (evt is InputEventKey k) {
			if (k.Pressed && k.Keycode == Key.Key7 && selectedRegion != null) RegionPlayRequested?.Invoke();
		}
	}

	public override void _GuiInput(InputEvent evt) {
		if (evt is InputEventMouseButton) {
			GetViewport().SetInputAsHandled();
		}
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
			(region.LocalFaction.HasOwningFaction()
				? "Colony of " + region.LocalFaction.GetOwningFaction()
				: region.LocalFaction.IsWild
					? "Howling wilderness"
					: "Sovereign territory") + "\n"
			+ $"Land tiles: {region.LandTileCount}\n"
			+ $"Sea tiles: {region.OceanTileCount}\n"
			+ $"Population: {(region.LocalFaction.GetPopulationCount())}\n"
			+ $"Natural Resources: {string.Join(", ", region.NaturalResources.Value.Select(a => a.ToString()))}\n"
			+ $"Potential Food: {(int)region.GetPotentialFoodFirstMonth()}\n"
			+ $"Map objects: {things}\n"
			+ $"Region IX: {region.WorldIndex}"
		;
		factionPlayButton.Disabled = region.LocalFaction.IsWild;
	}

}
