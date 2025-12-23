using System.Linq;
using Godot;
using scenes.autoload;

namespace scenes.map {

	public partial class WorldMan : Node {

		[Export] WorldGenerator worldGenerator;
		[Export] WorldRenderer worldRenderer;
		[Export] PackedScene regionScene;
		[Export] WorldUI worldUI;


		public override void _Ready() {
			RemoveChild(worldUI);
			UILayer.AddUIChild(worldUI);
			GenerateNewWorld();
		}

		public override void _UnhandledKeyInput(InputEvent evt) {
			if (evt.IsActionPressed("ui_accept")) {
				GenerateNewWorld();
			}
		}

		public override void _ExitTree() {
			worldUI.QueueFree();
		}

		World world;
		void GenerateNewWorld() {
			world = new(worldGenerator.WorldWidth, worldGenerator.WorldHeight);
			worldGenerator.GenerateContinents(world);
			worldRenderer.Draw(world);
			worldGenerator.GenerateRegionStarts(world);
			worldRenderer.DrawRegions(worldGenerator.Regions);

			// displaying region growth dynamically
			var drawRegionsCallable = Callable.From(() => worldRenderer.DrawRegions(worldGenerator.Regions));
			var tw = CreateTween().SetLoops();
			tw.TweenInterval(0.05f);
			tw.TweenCallback(drawRegionsCallable);

			worldGenerator.GrowRegions(world, SetupGame);
		}

		void SetupGame(Map map) {
			GameMan.Singleton.NewGame(map);
			GD.Print("game set up.");

			EnterGame();
		}

		void EnterGame() {
			GetTree().ChangeSceneToPacked(regionScene);
		}

	}

}
