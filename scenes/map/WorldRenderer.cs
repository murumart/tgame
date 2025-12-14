using System;
using Godot;

public partial class WorldRenderer : Node2D {

	World world;


	public override void _Draw() {
		if (world == null) return;
		for (int x = 0; x < world.Width; x++) {
			for (int y = 0; y < world.Height; y++) {
				Color col = world.GetTile(x, y) switch {
					GroundTileType.GRASS => new(0, 1, 0),
					GroundTileType.WATER => new(0, 0.5f, 1),
					_ => throw new NotImplementedException($"Rendering code for {world.GetTile(x, y)} not implemented"),
				};
				DrawRect(new Rect2(x, y, 1, 1), col);
			}
		}
	}

	public new void Draw(World world) {
		this.world = world;
		QueueRedraw();
	}

}
