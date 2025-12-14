using System.Linq;
using Godot;
using scenes.autoload;

namespace scenes.map {

	public partial class WorldMan : Node {

		[Export] WorldGenerator worldGenerator;
		[Export] WorldRenderer worldRenderer;
		[Export] PackedScene regionScene;


		public override void _Ready() {
			GenerateNewWorld();
		}

		public override void _UnhandledKeyInput(InputEvent evt) {
			if (evt.IsActionPressed("ui_accept")) {
				GenerateNewWorld();
			}
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
			tw.TweenInterval(0.5f);
			tw.TweenCallback(drawRegionsCallable);

			worldGenerator.GrowRegions(world, SetupGame);
		}

		void SetupGame(Region[] regions, Faction[] factions, RegionFaction[] regionFactions) {
			var map = new Map(regions.ToList(), factions.ToList(), regionFactions.ToList());
			GameMan.Singleton.NewGame(map);
			GD.Print("game set up.");

			EnterGame();
		}

		void EnterGame() {
			GetTree().ChangeSceneToPacked(regionScene);
		}

	}

}
