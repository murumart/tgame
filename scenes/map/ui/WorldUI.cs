using System;
using System.Linq;
using Godot;
using scenes.autoload;

namespace scenes.map.ui;

public partial class WorldUI : Control {

	enum Modes {
		Generation,
		InGame,
	}

	public event Func<Vector2I, (float, float, float)> WorldTileInfoRequested;
	public event Func<Vector2I, Region> RegionRequested;
	public event Action<Region> RegionSelected;

	bool _ready;

	[Export] Modes mode;
	[Export] WorldRenderer worldRenderer;
	[Export] public ResourceDisplay ResourceDisplay;
	[Export] Camera camera;
	[Export] Control factionPanel;
	[Export] Label factionTitleLabel;
	[Export] RichTextLabel factionInfoLabel;

	[Export] Godot.Collections.Array<CheckButton> drawLayerButtons;
	[Export] CheckButton regionDisplayCheck;

	Region selectedRegion;
	public Region SelectedRegion => selectedRegion;


	public override void _Ready() {
		factionPanel.GuiInput += _GuiInput;

		camera.ClickedMouseEvent += MouseClicked;

		foreach (var but in drawLayerButtons) but.Pressed += OnDrawLayersChanged;
		regionDisplayCheck.Toggled += OnRegionDisplayChanged;

		ResourceDisplay.Display(c => {
			if (!camera.IsInsideTree()) (c as Label).Text = "...";
			var mousePos = (Vector2I)camera.GetMousePos();
			if (mousePos != oldMousePos) {
				oldMousePos = mousePos;
				var tileInfo =  WorldTileInfoRequested?.Invoke(mousePos) ?? (-4f, -4f, -4f);
				oldTileInfo = tileInfo;
			}
			(c as Label).Text = $"ele: {oldTileInfo.Item1} temp: {oldTileInfo.Item2} humi: {oldTileInfo.Item3}";
		});
		ResourceDisplay.DisplayFat();

		_ready = true;
	}

	public override void _Notification(int what) {
		if (what == NotificationPredelete) {
			GD.Print("WorldUI::_Notification : IM BEING DELETEDD!!!");
		}
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

	public override void _GuiInput(InputEvent evt) {
		if (evt is InputEventMouseButton) {
			GetViewport().SetInputAsHandled();
		}
	}

	public void SelectRegion(Region region) {
		if (region == null) {
			factionTitleLabel.Text = ". . .";
			factionInfoLabel.Text = "Select a Faction";
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
		RegionSelected?.Invoke(region);
	}

	void SetRendererParams() {
		WorldRenderer.DrawLayers a = 0;
		if (drawLayerButtons[0].ButtonPressed) a |= WorldRenderer.DrawLayers.Ground;
		if (drawLayerButtons[1].ButtonPressed) a |= WorldRenderer.DrawLayers.Elevation;
		if (drawLayerButtons[2].ButtonPressed) a |= WorldRenderer.DrawLayers.Temperature;
		if (drawLayerButtons[3].ButtonPressed) a |= WorldRenderer.DrawLayers.Humidity;
		if (drawLayerButtons[4].ButtonPressed) a |= WorldRenderer.DrawLayers.Drainage;
		if (drawLayerButtons[5].ButtonPressed) a |= WorldRenderer.DrawLayers.SeaWind;
		worldRenderer.DrawMode = a;
	}

	public void DisplayWorld(World world) {
		Debug.Assert(_ready);
		worldRenderer.World = world;
		worldRenderer.ResetImages();
		SetRendererParams();
		worldRenderer.DrawWorld();
		camera.Position = new(world.Width * 0.5f, world.Height * 0.5f);
	}

	public void DrawRegions(Region[] regions) {
		Debug.Assert(_ready);
		worldRenderer.DrawRegions(regions);
	}

	void OnDrawLayersChanged() {
		DisplayWorld(GameMan.Game.Map.World);
		DrawRegions(GameMan.Game.Map.GetRegions());
	}

	void OnRegionDisplayChanged(bool to) {
		worldRenderer.RegionSprite.Visible = to;
	}

}
