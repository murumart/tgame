using System;
using System.Linq;
using System.Text;
using Godot;

namespace scenes.map.ui;

public partial class WorldUI : Control {

	enum Modes {
		Generation,
		InGame,
	}

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

	[Export] ControlButtons controlButtons;

	World world = null;
	Map map = null;
	Game game = null;

	Region selectedRegion;
	public Region SelectedRegion => selectedRegion;


	public override void _Ready() {
		factionPanel.GuiInput += _GuiInput;

		camera.ClickedMouseEvent += MouseClicked;

		foreach (var but in drawLayerButtons) but.Pressed += OnDrawLayersChanged;
		regionDisplayCheck.Toggled += OnRegionDisplayChanged;

		if (mode == Modes.InGame) {
		} else {
		}

		ResourceDisplay.Display(c => {
			if (!camera.IsInsideTree()) {
				(c as Label).Text = "...";
				return;
			}
			if (world is null) {
				(c as Label).Text = "...";
				return;
			}

			int x = oldMousePos.X;
			int y = oldMousePos.Y;
			(c as Label).Text = $"ele: {world.GetElevation(x, y):F3} temp: {world.GetTemperature(x, y):F3} humi: {world.GetTemperature(x, y):F3} drain: {world.GetDrainage(x, y):F3}";
		});
		ResourceDisplay.Display(c => (c as Label).Text = $"hover: {oldMousePos}");
		ResourceDisplay.DisplayFat();

		SelectRegion(null);

		_ready = true;
	}

	public override void _Notification(int what) {
		if (what == NotificationPredelete) {
			GD.Print("WorldUI::_Notification : IM BEING DELETEDD!!!");
		}
	}

	Vector2I oldMousePos;
	public override void _Process(double delta) {
		var mousePos = (Vector2I)camera.GetMousePos();
		if (mousePos != oldMousePos) {
			MouseMoved(mousePos);
			oldMousePos = mousePos;
		}
		ResourceDisplay.Display();
		controlButtons.UpdateDisplays();
	}

	void MouseMoved(Vector2I where) {
		if (map is not null && worldRenderer.World is not null) {
			map.TileOwners.TryGetValue(where, out Region region);
			worldRenderer.DrawRegionHighlight(region, SelectedRegion);
		}
	}

	void MouseClicked(Vector2I where) {
		if (map is null) return;
		if (!map.TileOwners.TryGetValue(where, out var region)) return;
		SelectRegion(region);
	}

	public override void _GuiInput(InputEvent evt) {
		if (evt is InputEventMouseButton) {
			GetViewport().SetInputAsHandled();
		}
	}

	public void SelectRegion(Region region) {
		selectedRegion = region;

		switch (mode) {
			case Modes.Generation:
				RegionDisplayGeneration(region);
				break;
			case Modes.InGame:
				if (game is null) break;
				Debug.Assert(game is not null, "Need a Game");
				RegionDisplayInGame(game.PlayRegion, region);
				break;
		}

		RegionSelected?.Invoke(region);
	}

	void RegionDisplayGeneration(Region region) {
		if (region == null) {
			factionTitleLabel.Text = ". . .";
			factionInfoLabel.Text = "Select a Faction";
			return;
		}
		factionTitleLabel.Text = region.LocalFaction.Name;
		var things = string.Join(", ", region.GetMapObjects().Select(a => ((IAssetType)a.Type).AssetName).Distinct());
		int landtiles = region.GetLandTileCount();
		StringBuilder naturalResources = new();
		naturalResources.Append('\n');
		foreach (var b in region.NaturalResources.Value) {
			naturalResources.Append('\t').Append(b.Type).Append(" x ").Append(b.Amount).Append('\n');
		}
		int score = region.GetStartEaseScore();
		var difficulty = region.GetStartDifficulty(score).Describe();
		factionInfoLabel.Text =
			(region.LocalFaction.HasOwningFaction()
				? "Colony of " + region.LocalFaction.GetOwningFaction()
				: region.LocalFaction.IsWild
					? "Howling wilderness"
					: "Sovereign territory") + "\n"
			+ $"Difficulty: {difficulty}\n"
			+ $"Land tiles: {landtiles} ({(int)(((float)landtiles / map.TotalLandTiles) * 1000) * 0.1:F1}% of world land)\n"
			+ $"Sea tiles: {region.GetOceanTileCount()}\n"
			+ $"Population: {(region.LocalFaction.GetPopulationCount())}\n"
			+ $"Natural Resources: {naturalResources}"
			+ $"Potential Food: {(int)region.GetPotentialFoodFirstMonth()}\n"
			+ $"Map objects: {things}\n"
			+ $"Region IX: {region.WorldIndex}"
		;
	}

	void RegionDisplayInGame(Region myRegion, Region region) {
		Faction myFaction = myRegion.LocalFaction;
		factionTitleLabel.Text = "...?";
		factionInfoLabel.Text = "";
		if (region is null) {
			factionInfoLabel.Text = "Select a Faction";
			return;
		}

		Faction faction = region.LocalFaction;
		bool iswild = region.LocalFaction.IsWild;
		bool isneighbor = myRegion.Neighbors.Contains(region);
		if (!isneighbor && myRegion != region) {
			factionInfoLabel.Text = "This faction is far away from us... Don't know much.";
			return;
		}
		factionTitleLabel.Text = region.LocalFaction.Name;
		if (myRegion == region) factionTitleLabel.Text += " (your location)";
		bool isatwar = myFaction.IsAtWarWith(faction);
		bool isdead = !iswild && faction.Population.Count == 0;
		if (isatwar) factionTitleLabel.Text += " (AT WAR WITH YOU)";
		if (isdead) factionTitleLabel.Text += " (Abandoned)";
		if (iswild) {
			factionInfoLabel.Text = "Empty of meaningful civilisation.\n"
				+ $"Land tiles: {region.GetLandTileCount()}\n"
				+ $"Sea tiles: {region.GetOceanTileCount()}\n";
			return;
		}
		int myMilitary = myFaction.Military;
		int military = faction.Military;
		int mildiff = military - myMilitary;
		int landtiles = region.GetLandTileCount();
		int totalsilver = region.LocalFaction.LiquidSilver;
		string mildesc = mildiff < 0
			? $"({-mildiff} less than ours)"
			: mildiff > 0
				? $"({mildiff} more than ours)"
				: "";
		factionInfoLabel.Text = ""
			+ $"Land tiles: {landtiles} ({(int)(((float)landtiles / map.TotalLandTiles) * 1000) * 0.1:F1}% of world land)\n"
			+ $"Sea tiles: {region.GetOceanTileCount()}\n"
			+ $"Population: {(region.LocalFaction.GetPopulationCount())}\n"
			+ $"Silver: {(region.LocalFaction.Silver)} (total {totalsilver}) ({(int)((float)totalsilver / map.TotalSilver * 100):F1}% of world silver)\n"
			+ $"Military power: {(region.LocalFaction.Military)} {mildesc}\n"
			+ $"Happiness with ruler: {((int)(region.LocalFaction.Population.Approval * 100))}%\n"
		;
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

	public void DisplayWorld(World world, Game game) {
		Debug.Assert(_ready);
		this.world = world;
		this.game = game;
		if (game is not null) this.map = game.Map;
		camera.LimitLeft = 0;
		camera.LimitTop = 0;
		camera.LimitRight = world.Width;
		camera.LimitBottom = world.Height;
		worldRenderer.World = world;
		worldRenderer.ResetImages();
		SetRendererParams();
		worldRenderer.DrawWorld();
		switch (mode) {
			case Modes.Generation:
				GenerationDisplay(world);
				break;
			case Modes.InGame:
				InGameDisplay(game.PlayRegion);
				break;
		}
	}

	void GenerationDisplay(World world) {
		camera.Position = new(world.Width * 0.5f, world.Height * 0.5f);
	}

	void InGameDisplay(Region myRegion) {
		camera.Position = myRegion.WorldPosition;
		camera.ZoomIn(3f);
	}

	public void DrawRegions(Region[] p_regions = null, Map p_map = null) {
		Debug.Assert(_ready);
		if (p_map is not null) {
			map = p_map;
			p_regions = map.GetRegions();
		}
		switch (mode) {
			case Modes.Generation: worldRenderer.DrawRegions(p_regions); break;
			case Modes.InGame:
				Debug.Assert(game is not null, "Need a Game");
				Debug.Assert(map is not null, "Need a Map");
				worldRenderer.DrawRegionsDark(game.PlayRegion, map.GetRegions());
				break;
		}

	}

	void OnDrawLayersChanged() {
		Debug.Assert(world is not null, "Need a Woirld");
		DisplayWorld(world, game);
		if (map is not null) DrawRegions(p_map: map);
	}

	void OnRegionDisplayChanged(bool to) {
		worldRenderer.RegionSprite.Visible = to;
	}

}
