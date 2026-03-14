using System;
using System.Threading.Tasks;
using Godot;
using scenes.autoload;

namespace scenes.map.ui;

public partial class WorldGenUi : MarginContainer {

	public event Action GoBackEvent;

	[Export] WorldGenerator worldGenerator;
	[Export] WorldRenderer worldRenderer;
	[Export] Camera camera;

	[Export] Button genRegionsButton;
	[Export] LineEdit worldSeedLabel;
	[Export] Button worldSeedRandomButton;
	[Export] SpinBox worldWidthSpinbox;
	[Export] SpinBox worldHeightSpinbox;
	[Export] SpinBox noiseScaleSpinbox;
	[Export] SpinBox depthSpinbox;

	[Export] Button backButton;

	[Export] Godot.Collections.Array<CheckButton> drawLayerButtons;
	[Export] CheckButton regionDisplayCheck;

	Map map;
	World world;


	public override void _Ready() {

		genRegionsButton.Pressed += OnGenRegionsPressed;

		worldWidthSpinbox.ValueChanged += OnWorldWidthChanged;
		worldHeightSpinbox.ValueChanged += OnWorldHeightChanged;
		noiseScaleSpinbox.ValueChanged += OnNoiseScaleChanged;
		depthSpinbox.ValueChanged += OnDepthChanged;

		worldSeedLabel.TextSubmitted += OnWorldSeedEntered;
		worldSeedRandomButton.Pressed += OnWorldSeedRandomiseRequested;

		backButton.Pressed += () => GoBackEvent?.Invoke();

		foreach (var but in drawLayerButtons) but.Pressed += OnDrawLayersChanged;
		regionDisplayCheck.Toggled += OnRegionDisplayChanged;

		worldSeedLabel.Text = "" + GD.Randi();

		NewWorld();
		Task.Run(() => GenerateContinents()).GetAwaiter().GetResult();

		SetRendererParams();
		OnWorldGenerated();
	}

	void NewWorld() {
		this.world = new((int)worldWidthSpinbox.Value, (int)worldHeightSpinbox.Value, (uint)Convert.ToUInt32(worldSeedLabel.Text));
		GameMan.Singleton.NewGame(new([], world));
	}

	async Task GenerateContinents() => await worldGenerator.GenerateContinents(world, (float)noiseScaleSpinbox.Value, (float)depthSpinbox.Value);

	void DisplayWorld(World world) {
		worldRenderer.Draw(world);
	}

	void OnWorldGenerated() {
		worldRenderer.World = world;
		worldRenderer.ResetImages();
		DisplayWorld(world);
		camera.Position = new(world.Width * 0.5f, world.Height * 0.5f);
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

	void SetRendererParams() {
		WorldRenderer.DrawLayers a = 0;
		if (drawLayerButtons[0].ButtonPressed) a |= WorldRenderer.DrawLayers.Ground;
		if (drawLayerButtons[1].ButtonPressed) a |= WorldRenderer.DrawLayers.Elevation;
		if (drawLayerButtons[2].ButtonPressed) a |= WorldRenderer.DrawLayers.Temperature;
		if (drawLayerButtons[3].ButtonPressed) a |= WorldRenderer.DrawLayers.Humidity;
		if (drawLayerButtons[4].ButtonPressed) a |= WorldRenderer.DrawLayers.SeaWind;
		worldRenderer.DrawMode = a;
	}

	void OnDrawLayersChanged() {
		SetRendererParams();
		DisplayWorld(world);
	}

	void OnRegionDisplayChanged(bool to) {
		worldRenderer.RegionSprite.Visible = to;
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
		var drawRegionsCallable = Callable.From(() => worldRenderer.DrawRegions(worldGenerator.Regions));
		var tw = CreateTween().SetLoops();
		tw.TweenInterval(0.05f);
		tw.TweenCallback(drawRegionsCallable);

		this.map = await worldGenerator.GenerateRegions(world);
		tw.Stop();

		worldRenderer.DrawRegions(map.GetRegions());

		GameMan.Singleton.NewGame(map);
		OnEndGenerating();
	}

}
