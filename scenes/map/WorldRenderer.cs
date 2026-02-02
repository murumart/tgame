using Godot;

public partial class WorldRenderer : Node {

	ColorPalette palette = GD.Load<ColorPalette>("uid://cr4o125t00hli");

	[Export] Sprite2D groundSprite;
	[Export] public Sprite2D RegionSprite;
	[Export] Sprite2D highlightSprite;
	[Export] Gradient elevationGradient;
	[Export] Gradient temperatureGradient;
	[Export] Gradient seaWindGradient;

	public enum DrawMode {
		Continents,
		SeaWind,
		Temperature,

		Max
	}

	World world;
	DrawMode _drawMode;
	[Export]
	public DrawMode drawMode {
		set {
			_drawMode = value;
			switch (drawMode) {
				case DrawMode.Continents:
					DrawContinents();
					break;
				case DrawMode.SeaWind:
					DrawSeaWind();
					break;
				case DrawMode.Temperature:
					DrawTemperature();
					break;
			}
		}
		get => _drawMode;
	}


	public void ResetImages() {
		GD.Print("WorldRenderer::ResetImages : reset images");
		Image worldImage = Image.CreateEmpty(world.Longitude, world.Latitude, false, Image.Format.Rgba4444);
		Image regionImage = Image.CreateEmpty(world.Longitude, world.Latitude, false, Image.Format.Rgba4444);
		regionImage.Fill(Colors.White);
		Image highlightImage = Image.CreateEmpty(world.Longitude, world.Latitude, false, Image.Format.Rgba4444);
		groundSprite.Texture = ImageTexture.CreateFromImage(worldImage);
		RegionSprite.Texture = ImageTexture.CreateFromImage(regionImage);
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

	void DrawTemperature() {
		if (world == null) return;
		var worldImage = GetImage(groundSprite);
		for (int x = 0; x < world.Longitude; x++) {
			for (int y = 0; y < world.Latitude; y++) {
				Color color = temperatureGradient.Sample(world.GetTemperature(x, y) * 0.5f + 0.5f);
				worldImage.SetPixel(x, y, color);
			}

		}
		UpdateImage(worldImage, groundSprite);
	}

	void DrawSeaWind() {
		if (world == null) return;
		var worldImage = GetImage(groundSprite);
		for (int x = 0; x < world.Longitude; x++) {
			for (int y = 0; y < world.Latitude; y++) {
				Color color = seaWindGradient.Sample(world.GetSeaWind(x, y));
				worldImage.SetPixel(x, y, color);
			}

		}
		UpdateImage(worldImage, groundSprite);
	}

	public void Draw(World world) {
		this.world = world;
		ResetImages();
		switch (drawMode) {
			case DrawMode.Continents:
				DrawContinents();
				break;
			case DrawMode.SeaWind:
				DrawSeaWind();
				break;
			case DrawMode.Temperature:
				DrawTemperature();
				break;
		}
	}

	public void DrawRegions(Region[] regions) {
		if (regions == null) return;
		var image = GetImage(RegionSprite);
		foreach (var region in regions) {
			var color = region.Color.Lightened(0.5f);
			if (region.LocalFaction != null && region.LocalFaction.HasOwningFaction()) color = region.LocalFaction.GetOwningFaction().Region.Color.Darkened(0.25f);
			foreach (var px in region.GroundTiles.Keys) {
				image.SetPixelv(px + region.WorldPosition, color);
			}
		}
		UpdateImage(image, RegionSprite);
	}

	public void DrawRegionHighlight(Region hovered, Region highlighted) {
		var image = GetImage(highlightSprite);
		image.Fill(Colors.Transparent);
		if (hovered != null) {
			var color = Colors.White;
			foreach (var px in hovered.GroundTiles.Keys) {
				image.SetPixelv(px + hovered.WorldPosition, color);
			}
			if (hovered.LocalFaction.HasOwningFaction()) {
				var owner = hovered.LocalFaction.GetOwningFaction().Region;
				foreach (var px in owner.GroundTiles.Keys) {
					color = (px.X * px.Y) % 2 == 0 ? Colors.Blue : Colors.White;
					image.SetPixelv(px + owner.WorldPosition, color);
				}
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
