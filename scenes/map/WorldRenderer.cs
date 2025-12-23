using System;
using Godot;

public partial class WorldRenderer : Node {

	[Export] Sprite2D groundSprite;
	[Export] Sprite2D regionSprite;

	World world;


	void DrawContinents() {
		if (world == null) return;
		Image image = Image.CreateEmpty(world.Width, world.Height, false, Image.Format.Rgba4444);
		Image regionImage = Image.CreateEmpty(world.Width, world.Height, false, Image.Format.Rgba4444);
		for (int x = 0; x < world.Width; x++) {
			for (int y = 0; y < world.Height; y++) {
				Color color = world.GetTile(x, y) switch {
					GroundTileType.GRASS => new(0, 1, 0),
					GroundTileType.WATER => new(0, 0.5f, 1),
					_ => new(0, 0, 0),
				};
				//DrawRect(new Rect2(x, y, 1, 1), col);
				image.SetPixel(x, y, color);
			}

		}
		groundSprite.Texture = ImageTexture.CreateFromImage(image);
		regionSprite.Texture = ImageTexture.CreateFromImage(regionImage);
	}

	public void Draw(World world) {
		this.world = world;
		DrawContinents();
	}

	public void DrawRegions(Region[] regions) {
		var image = (regionSprite.Texture as ImageTexture).GetImage();
		foreach (var region in regions) {
			Color color = new(region.Color, 0.5f);
			foreach (var px in region.GroundTiles.Keys) {
				image.SetPixelv(px + region.WorldPosition, color);
			}
		}
		(regionSprite.Texture as ImageTexture).Update(image);
	}

}
