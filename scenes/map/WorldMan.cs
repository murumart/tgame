using Godot;

namespace scenes.map {

	public partial class WorldMan : Node {

		[Export] WorldGenerator worldGenerator;
		[Export] WorldRenderer worldRenderer;


		public override void _Ready() {
			GenerateNewWorld();
		}

		public override void _UnhandledKeyInput(InputEvent evt) {
			if (evt.IsActionPressed("ui_accept")) {
				GenerateNewWorld();
			}
		}

		void GenerateNewWorld() {
			World world = new(worldGenerator.WorldWidth, worldGenerator.WorldHeight);
			worldGenerator.GenerateContinents(world);
			worldRenderer.Draw(world);
			worldGenerator.GenerateRegionStarts(world);
			worldRenderer.DrawRegions(worldGenerator.Regions);
			var drawRegionsCallable = Callable.From(() => worldRenderer.DrawRegions(worldGenerator.Regions));
			var tw = CreateTween().SetLoops();
			tw.TweenInterval(0.5f);
			tw.TweenCallback(drawRegionsCallable);
			worldGenerator.GrowRegions(world);
		}

	}

}
