using Godot;

namespace scenes.map {

	public partial class WorldMan : Node {

		[Export] WorldGenerator worldGenerator;
		[Export] WorldRenderer worldRenderer;


		public override void _Ready() {
			var world = worldGenerator.GenerateWorld();
			worldRenderer.Draw(world);
		}

		public override void _UnhandledKeyInput(InputEvent @event) {
			if (@event.IsActionPressed("ui_accept")) {
				var world = worldGenerator.GenerateWorld();
				worldRenderer.Draw(world);
			}
		}

	}

}
