using Godot;

public partial class WorldRenderer : Node {

	ColorPalette palette = GD.Load<ColorPalette>("uid://cr4o125t00hli");

	[Export] Sprite2D groundSprite;
	[Export] Sprite2D regionSprite;
	[Export] Sprite2D highlightSprite;
	[Export] Gradient elevationGradient;

	World world;


	public void ResetImages() {
		GD.Print("WorldRenderer::ResetImages : reset images");
		Image worldImage = Image.CreateEmpty(world.Longitude, world.Latitude, false, Image.Format.Rgba4444);
		Image regionImage = Image.CreateEmpty(world.Longitude, world.Latitude, false, Image.Format.Rgba4444);
		regionImage.Fill(Colors.White);
		Image highlightImage = Image.CreateEmpty(world.Longitude, world.Latitude, false, Image.Format.Rgba4444);
		groundSprite.Texture = ImageTexture.CreateFromImage(worldImage);
		regionSprite.Texture = ImageTexture.CreateFromImage(regionImage);
		highlightSprite.Texture = ImageTexture.CreateFromImage(highlightImage);
	}

	Image GetImage(Sprite2D sprite) {
		var image = (sprite.Texture as ImageTexture).GetImage();
		return image;
	}

	void UpdateImage(Image image, Sprite2D sprite) {
		(sprite.Texture as ImageTexture).Update(image);
	}

	void DrawContinents() {
		if (world == null) return;
		ResetImages();
		var worldImage = GetImage(groundSprite);
		for (int x = 0; x < world.Longitude; x++) {
			for (int y = 0; y < world.Latitude; y++) {
				const float eleColorThreshold = 0.02f;
				const float ldarkAmount = 0.15f;
				float ele = world.GetElevation(x, y);
				float eleAbove = world.GetElevation(x - 1, y + 1);
				Color color = elevationGradient.Sample(ele * 0.5f + 0.5f);
				if (eleAbove - ele > eleColorThreshold) color = color.Darkened(ldarkAmount);
				else if (eleAbove - ele < -eleColorThreshold) color = color.Lightened(ldarkAmount);

				worldImage.SetPixel(x, y, color);
			}

		}
		UpdateImage(worldImage, groundSprite);
	}

	public void Draw(World world) {
		this.world = world;
		DrawContinents();
	}

	public void DrawRegions(Region[] regions) {
		if (regions == null) return;
		var image = GetImage(regionSprite);
		foreach (var region in regions) {
			foreach (var px in region.GroundTiles.Keys) {
				image.SetPixelv(px + region.WorldPosition, region.Color.Lightened(0.5f));
			}
		}
		UpdateImage(image, regionSprite);
	}

	public void DrawRegionHighlight(Region hovered, Region highlighted) {
		var image = GetImage(highlightSprite);
		image.Fill(Colors.Transparent);
		if (hovered != null) {
			var color = Colors.White;
			foreach (var px in hovered.GroundTiles.Keys) {
				image.SetPixelv(px + hovered.WorldPosition, color);
			}
		}
		if (highlighted != null && hovered != highlighted) {
			var color = Colors.Gray;
			foreach (var px in highlighted.GroundTiles.Keys) {
				image.SetPixelv(px + highlighted.WorldPosition, color);
			}
		}
		UpdateImage(image, highlightSprite);
	}

}
