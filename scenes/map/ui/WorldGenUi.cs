using System;
using System.Threading.Tasks;
using Godot;
using scenes.autoload;

namespace scenes.map.ui;

public partial class WorldGenUi : MarginContainer {

	static readonly PackedScene regionScene = GD.Load<PackedScene>("res://scenes/region/player_region.tscn");

	public event Action GoBackEvent;

	[Export] WorldGenerator worldGenerator;
	[Export] WorldUI worldUI;

	[Export] Button genRegionsButton;
	[Export] Button playHereButton;
	[Export] LineEdit worldSeedLabel;
	[Export] Button worldSeedRandomButton;
	[Export] SpinBox worldWidthSpinbox;
	[Export] SpinBox worldHeightSpinbox;
	[Export] SpinBox noiseScaleSpinbox;
	[Export] SpinBox depthSpinbox;
	[Export] bool drawFast;


	[Export] Button backButton;

	Map map;
	World world;


	public override void _Ready() {

		genRegionsButton.Pressed += OnGenRegionsPressed;
		playHereButton.Pressed += () => {
			SetupGame();
			EnterGame();
		};

		worldWidthSpinbox.ValueChanged += OnWorldWidthChanged;
		worldHeightSpinbox.ValueChanged += OnWorldHeightChanged;
		noiseScaleSpinbox.ValueChanged += OnNoiseScaleChanged;
		depthSpinbox.ValueChanged += OnDepthChanged;

		worldSeedLabel.TextSubmitted += OnWorldSeedEntered;
		worldSeedRandomButton.Pressed += OnWorldSeedRandomiseRequested;

		backButton.Pressed += () => GoBackEvent?.Invoke();

		worldUI.RegionSelected += OnRegionSelected;

		worldSeedLabel.Text = "" + GD.Randi();
	}

	public void InitialiseNewWorld() {
		NewWorld();
		Task.Run(() => GenerateContinents()).GetAwaiter().GetResult();

		OnWorldGenerated();
	}

	public void LoadCurrentWorld() {
		this.map = GameMan.Game.Map;
		this.world = this.map.World;
		OnWorldGenerated();
	}

	void NewWorld() {
		this.world = new((int)worldWidthSpinbox.Value, (int)worldHeightSpinbox.Value, (uint)Convert.ToUInt32(worldSeedLabel.Text));
		//GameMan.NewGame(new([], world));
	}

	async Task GenerateContinents() => await worldGenerator.GenerateContinents(world, (float)noiseScaleSpinbox.Value, (float)depthSpinbox.Value);

	void OnWorldGenerated() {
		worldUI.DisplayWorld(world);
	}

	async void OnWorldSeedEntered(string what) {
		if (!what.IsValidInt()) {
			worldSeedLabel.Text = "" + what.Hash();
		}
		await SomethingChanged();
	}

	async void OnWorldSeedRandomiseRequested() {
		worldSeedLabel.Text = "" + GD.Randi();
		await SomethingChanged();
	}

	async void OnWorldWidthChanged(double to) {
		await SomethingChanged();
	}

	async void OnWorldHeightChanged(double to) {
		await SomethingChanged();
	}

	async void OnNoiseScaleChanged(double to) {
		await SomethingChanged();
	}

	async void OnDepthChanged(double to) {
		await SomethingChanged();
	}

	async Task SomethingChanged() {
		Debug.Assert(!worldGenerator.Generating);
		OnStartGenerating();
		NewWorld();
		await GenerateContinents();
		OnWorldGenerated();
		OnEndGenerating();
	}

	void OnStartGenerating() {
		genRegionsButton.Disabled = true;
		worldWidthSpinbox.Editable = false;
		worldHeightSpinbox.Editable = false;
		noiseScaleSpinbox.Editable = false;
		depthSpinbox.Editable = false;
		worldSeedLabel.Editable = false;
		worldSeedRandomButton.Disabled = true;
	}

	void OnEndGenerating() {
		genRegionsButton.Disabled = false;
		worldWidthSpinbox.Editable = true;
		worldHeightSpinbox.Editable = true;
		noiseScaleSpinbox.Editable = true;
		depthSpinbox.Editable = true;
		worldSeedLabel.Editable = true;
		worldSeedRandomButton.Disabled = false;
	}


	async void OnGenRegionsPressed() {
		Debug.Assert(!worldGenerator.Generating);
		OnStartGenerating();

		if (!drawFast && !OS.HasFeature("editor_runtime")) {
			var drawRegionsCallable = Callable.From(() => worldUI.DrawRegions(worldGenerator.Regions));
			var tw = CreateTween().SetLoops();
			tw.TweenInterval(0.05f);
			tw.TweenCallback(drawRegionsCallable);

			this.map = await worldGenerator.GenerateRegions(world, 1);
			tw.Stop();
		} else {
			this.map = await worldGenerator.GenerateRegions(world, 128);
		}


		worldUI.DrawRegions(p_map: map);

		//GameMan.NewGame(map);
		OnEndGenerating();
	}

	void OnRegionSelected(Region region) {
		playHereButton.Disabled = region.LocalFaction.IsWild;
	}

	void SetupGame() {
		GameMan.NewGame(map);
		GD.Print("WorldGenUi::SetupGame : game set up.");
	}

	void EnterGame() {
		GameMan.Game.SetPlayRegion(worldUI.SelectedRegion);
		GameMan.SceneTransition(regionScene);
	}

}
