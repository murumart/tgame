using System;
using Godot;

public partial class WorldRenderer : Sprite2D {

	World world;


	void DrawContinents() {
		if (world == null) return;
		Image image = Image.CreateEmpty(world.Width, world.Height, false, Image.Format.Rgba4444);
		for (int x = 0; x < world.Width; x++) {
			for (int y = 0; y < world.Height; y++) {
				Color color = world.GetTile(x, y) switch {
					GroundTileType.GRASS => new(0, 1, 0),
					GroundTileType.WATER => new(0, 0.5f, 1),
					_ => throw new NotImplementedException($"Rendering code for {world.GetTile(x, y)} not implemented"),
				};
				//DrawRect(new Rect2(x, y, 1, 1), col);
				image.SetPixel(x, y, color);
			}

		}
		Texture = ImageTexture.CreateFromImage(image);
	}

	public new void Draw(World world) {
		this.world = world;
		DrawContinents();
	}

	public void DrawRegions(Region[] regions) {
		var image = (Texture as ImageTexture).GetImage();
		foreach (var region in regions) {
			Color color = new(region.Color, 0.5f);
			foreach (var px in region.GroundTiles.Keys) {
				image.SetPixelv(px, color);
			}
		}
		(Texture as ImageTexture).Update(image);
	}

}
