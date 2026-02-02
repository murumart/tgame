using System.Linq;
using Godot;
using scenes.autoload;

namespace scenes.map {

	public partial class WorldMan : Node2D {

		[Export] WorldGenerator worldGenerator;
		[Export] WorldRenderer worldRenderer;
		[Export] PackedScene regionScene;
		[Export] WorldUI worldUI;
		[Export] Camera camera;

		Map map;


		public override void _Ready() {
			RemoveChild(worldUI);
			UILayer.AddUIChild(worldUI);
			worldUI.SelectRegion(null);
			worldUI.RegionPlayRequested += SetupGame;
			worldUI.WorldDisplaySelected += which => worldRenderer.drawMode = (WorldRenderer.DrawMode)which;
			worldUI.RegionsDisplaySet += on => worldRenderer.RegionSprite.Visible = on;
			worldUI.WorldTileInfoRequested += where => {
				var w = new Vector2I((int)where.X, (int)where.Y);
				if (map == null) return (-3f, -3f, -3f);
				if (map.World == null) return (-2f, -2f, -2f);
				return (map.World.GetElevation(w.X, w.Y), map.World.GetTemperature(w.X, w.Y), map.World.GetHumidity(w.X, w.Y));
			};
			camera.ClickedMouseEvent += MouseClicked;
			camera.Position = new(worldGenerator.WorldWidth * 0.5f, worldGenerator.WorldHeight * 0.5f);

			if (GameMan.Singleton.Game.Map.World != null) {
				map = GameMan.Singleton.Game.Map;
				worldRenderer.Draw(map.World);
				worldRenderer.DrawRegions(map.GetRegions());
				return;
			}
			GenerateNewWorld();
		}

		void MouseClicked(Vector2I pos) {
			if (map == null) return;
			if (!map.TileOwners.TryGetValue(pos, out Region region)) return;
			worldUI.SelectRegion(region);
		}

		public override void _UnhandledInput(InputEvent evt) {
			if (evt.IsActionPressed("ui_accept") && !worldGenerator.Generating) {
				GenerateNewWorld();
			}
		}

		public override void _Process(double delta) {
			var mousePos = new Vector2I((int)GetGlobalMousePosition().X, (int)GetGlobalMousePosition().Y);
			worldUI.ResourceDisplay.Display(tilepos: mousePos);
			if (map != null) {
				map.TileOwners.TryGetValue(mousePos, out Region region);
				worldRenderer.DrawRegionHighlight(region, worldUI.SelectedRegion);
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

			GD.Print("WorldMan::GenerateNewWorld : map is ", map);
			this.map = map;
			drawRegionsCallable.Call();
		}
		// #endregion worldgen

		void SetupGame() {
			GameMan.Singleton.NewGame(worldUI.SelectedRegion, map);
			GD.Print("WorldMan::SetupGame : game set up.");

			EnterGame();
		}

		void EnterGame() {
			GetTree().ChangeSceneToPacked(regionScene);
		}

	}

}
