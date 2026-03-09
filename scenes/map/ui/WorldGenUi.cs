using System.Threading.Tasks;
using Godot;

namespace scenes.map.ui;

public partial class WorldGenUi : MarginContainer {

	[Export] WorldGenerator worldGenerator;
	[Export] WorldRenderer worldRenderer;
	[Export] Camera camera;

	[Export] Button genRegionsButton;
	[Export] LineEdit worldSeedLabel;
	[Export] SpinBox worldWidthSpinbox;
	[Export] SpinBox worldHeightSpinbox;
	[Export] SpinBox noiseScaleSpinbox;
	[Export] SpinBox depthSpinbox;

	Map map;
	World world;


	public override void _Ready() {

		genRegionsButton.Pressed += OnGenRegionsPressed;

		worldWidthSpinbox.ValueChanged += OnWorldWidthChanged;
		worldHeightSpinbox.ValueChanged += OnWorldHeightChanged;
		noiseScaleSpinbox.ValueChanged += OnNoiseScaleChanged;
		depthSpinbox.ValueChanged += OnDepthChanged;
        
		worldSeedLabel.TextSubmitted += OnWorldSeedEntered;

		worldSeedLabel.Text = "" + GD.Randi();

		NewWorld();
		Task.Run(() => GenerateContinents()).GetAwaiter().GetResult();

		OnWorldGenerated();
	}

	void NewWorld() {
		this.world = new((int)worldWidthSpinbox.Value, (int)worldHeightSpinbox.Value, (uint)worldSeedLabel.Text.ToFloat());
	}

	async Task GenerateContinents() => await worldGenerator.GenerateContinents(world, (float)noiseScaleSpinbox.Value, (float)depthSpinbox.Value);

	void DisplayWorld(World world) {
		worldRenderer.Draw(world);
	}

	void OnWorldGenerated() {
		DisplayWorld(world);
		camera.Position = new(world.Width * 0.5f, world.Height * 0.5f);
	}

	async void OnWorldSeedEntered(string what) {
		if (!what.IsValidInt()) {
			worldSeedLabel.Text = "" + what.Hash();
		}
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
	}

	void OnEndGenerating() {
		genRegionsButton.Disabled = false;
		worldWidthSpinbox.Editable = true;
		worldHeightSpinbox.Editable = true;
		noiseScaleSpinbox.Editable = true;
		depthSpinbox.Editable = true;
		worldSeedLabel.Editable = true;
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
		OnEndGenerating();
	}

}
