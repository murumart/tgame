using System.Linq;
using Godot;
using scenes.autoload;

namespace scenes.map {

	public partial class WorldMan : Node {

		[Export] WorldGenerator worldGenerator;
		[Export] WorldRenderer worldRenderer;
		[Export] PackedScene regionScene;
		[Export] WorldUI worldUI;
		[Export] Node2D canvasAccess;

		Map map;


		public override void _Ready() {
			RemoveChild(worldUI);
			UILayer.AddUIChild(worldUI);
			worldUI.SelectRegion(null);
			worldUI.RegionPlayRequested += SetupGame;
			GenerateNewWorld();
		}

		public override void _UnhandledKeyInput(InputEvent evt) {
			if (evt.IsActionPressed("ui_accept") && !worldGenerator.Generating) {
				GenerateNewWorld();
			}
		}

		public override void _Process(double delta) {
			var mousePos = new Vector2I((int)canvasAccess.GetGlobalMousePosition().X, (int)canvasAccess.GetGlobalMousePosition().Y);
			worldUI.ResourceDisplay.Display(tilepos: mousePos);
			if (map != null) {
				map.TileOwners.TryGetValue(mousePos, out Region atMouse);
				worldUI.ResourceDisplay.Display(region: atMouse);
				worldRenderer.DrawRegionHighlight(atMouse, worldUI.SelectedRegion);
				if (Input.IsActionJustPressed("left_click") && atMouse != null) {
					RegionClicked(atMouse);
				}
			}
		}

		public override void _ExitTree() {
			worldUI.QueueFree();
		}

		void RegionClicked(Region region) {
			worldUI.SelectRegion(region);
		}

		// #region worldgen
		World world;
		async void GenerateNewWorld() {
			this.map = null;
			worldUI.SelectRegion(null);
			world = new(worldGenerator.WorldWidth, worldGenerator.WorldHeight);
			worldGenerator.GenerateContinents(world);
			worldRenderer.Draw(world);

			// displaying region growth dynamically
			var drawRegionsCallable = Callable.From(() => worldRenderer.DrawRegions(worldGenerator.Regions));
			var tw = CreateTween().SetLoops();
			tw.TweenInterval(0.05f);
			tw.TweenCallback(drawRegionsCallable);

			var map = await worldGenerator.GenerateRegions(world);
			tw.Stop();

			GD.Print("map is ", map);
			this.map = map;
			drawRegionsCallable.Call();
		}
		// #endregion worldgen

		void SetupGame() {
			GameMan.Singleton.NewGame(worldUI.SelectedRegion, map);
			GD.Print("game set up.");

			EnterGame();
		}

		void EnterGame() {
			GetTree().ChangeSceneToPacked(regionScene);
		}

	}

}
