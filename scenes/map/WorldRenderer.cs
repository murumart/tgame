using Godot;

public partial class WorldRenderer : Node {

	ColorPalette palette = GD.Load<ColorPalette>("uid://cr4o125t00hli");

	[Export] Sprite2D groundSprite;
	[Export] Sprite2D regionSprite;
	[Export] Sprite2D highlightSprite;

	World world;


	void DrawContinents() {
		if (world == null) return;
		Image image = Image.CreateEmpty(world.Width, world.Height, false, Image.Format.Rgba4444);
		Image regionImage = Image.CreateEmpty(world.Width, world.Height, false, Image.Format.Rgba4444);
		for (int x = 0; x < world.Width; x++) {
			for (int y = 0; y < world.Height; y++) {
				Color color = world.GetTile(x, y) switch {
					GroundTileType.Grass => palette.Colors[16],
					GroundTileType.Ocean => palette.Colors[27],
					_ => new(0, 0, 0),
				};
				//DrawRect(new Rect2(x, y, 1, 1), col);
				image.SetPixel(x, y, color);
			}

		}
		groundSprite.Texture = ImageTexture.CreateFromImage(image);
		regionSprite.Texture = ImageTexture.CreateFromImage(regionImage);
		highlightSprite.Texture = ImageTexture.CreateFromImage(regionImage);
	}

	public void Draw(World world) {
		this.world = world;
		DrawContinents();
	}

	public void DrawRegions(Region[] regions) {
		if (regions == null) return;
		var image = (regionSprite.Texture as ImageTexture).GetImage();
		foreach (var region in regions) {;
			foreach (var px in region.GroundTiles.Keys) {
				image.SetPixelv(px + region.WorldPosition, region.Color);
			}
		}
		(regionSprite.Texture as ImageTexture).Update(image);
	}

	public void DrawRegionHighlight(Region region) {
		var image = (highlightSprite.Texture as ImageTexture).GetImage();
		image.Fill(Colors.Transparent);
		if (region != null) {
			var color = Colors.White;
			foreach (var px in region.GroundTiles.Keys) {
				image.SetPixelv(px + region.WorldPosition, color);
			}
		}
		(highlightSprite.Texture as ImageTexture).Update(image);
	}

}
