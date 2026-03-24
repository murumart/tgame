using Godot;
using scenes.region;
using scenes.ui;

public partial class WorldRenderer : Node {

	[Export] Sprite2D groundSprite;
	[Export] public Sprite2D RegionSprite;
	[Export] Sprite2D highlightSprite;
	[Export] Gradient elevationGradient;
	[Export] Gradient temperatureGradient;
	[Export] Gradient seaWindGradient;
	[Export] Gradient humidityGradient;
	[Export] Gradient drainageGradient;

	[System.Flags]
	public enum DrawLayers {
		Ground      = 1,
		Elevation   = 2,
		Temperature = 4,
		Humidity    = 8,
		Drainage    = 16,
		SeaWind     = 32,
		Regions     = 64,
	}

	public World World;
	DrawLayers _drawMode;
	[Export]
	public DrawLayers DrawMode {
		set {
			_drawMode = value;
		}
		get => _drawMode;
	}


	public void ResetImages() {
		ResetWorldImage();
		Image regionImage = Image.CreateEmpty(World.Width, World.Height, false, Image.Format.Rgba4444);
		regionImage.Fill(Colors.White);
		Image highlightImage = Image.CreateEmpty(World.Width, World.Height, false, Image.Format.Rgba4444);
		RegionSprite.Texture = ImageTexture.CreateFromImage(regionImage);
		highlightSprite.Texture = ImageTexture.CreateFromImage(highlightImage);
	}

	void ResetWorldImage() {
		Image worldImage = Image.CreateEmpty(World.Width, World.Height, false, Image.Format.Rgba8);
		groundSprite.Texture = ImageTexture.CreateFromImage(worldImage);
	}

	Image GetImage(Sprite2D sprite) {
		var image = (sprite.Texture as ImageTexture)?.GetImage();
		return image;
	}

	void UpdateImage(Image image, Sprite2D sprite) {
		(sprite.Texture as ImageTexture).Update(image);
	}

	static void BytesToColors(byte[] a, Color[] b) {
		for (int i = 0; i < (a.Length >> 2); i += 4) {
			int bix = i << 2;
			b[i] = Color.Color8(a[bix + 0], a[bix + 1], a[bix + 2], a[bix + 3]);
		}
	}

	static void ColorsToBytes(Color[] a, byte[] b) {
		for (int i = 0; i < a.Length; i += 1) {
			int bix = i << 2;
			Color col = a[i];
			b[bix + 0] = (byte)col.R8;
			b[bix + 1] = (byte)col.G8;
			b[bix + 2] = (byte)col.B8;
			b[bix + 3] = (byte)col.A8;
		}
	}

	public void Draw(World world) {
		this.World = world;
		ResetWorldImage();

		var worldImage = GetImage(groundSprite);
		byte[] imgbytes = worldImage.GetData();
		int width = worldImage.GetWidth();
		int height = worldImage.GetHeight();
		Color[] colors = new Color[width * height];
		BytesToColors(imgbytes, colors); 
		if ((DrawMode & DrawLayers.Ground) != 0) {
			var textureImage = GD.Load<Texture2D>("res://scenes/region/tiles1.png").GetImage();
			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					var tile = world.GetTile(x, y);
					var celltype = GroundCellType.MatchTileTypeToCell(tile);
					var v = new Vector2I(celltype.AtlasCoords.X * 64 + 32, celltype.AtlasCoords.Y * 48 + 16);
					Color color = textureImage.GetPixelv(v);
					colors[x + y * width] = color;
				}

			}
		} else {
			System.Array.Fill<Color>(colors, Palette.WhiteSmoke);
		}
		if ((DrawMode & DrawLayers.Temperature) != 0) {
			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					Color color = temperatureGradient.Sample(world.GetTemperature(x, y) * 0.5f + 0.5f) * colors[x + y * width];
					colors[x + y * width] = color;
				}

			}
		}
		if ((DrawMode & DrawLayers.Humidity) != 0) {
			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					Color color = humidityGradient.Sample(world.GetHumidity(x, y)) * colors[x + y * width];
					colors[x + y * width] = color;
				}

			}
		}
		if ((DrawMode & DrawLayers.Drainage) != 0) {
			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					Color color = drainageGradient.Sample(world.GetDrainage(x, y)) * colors[x + y * width];
					colors[x + y * width] = color;
				}

			}
		}
		if ((DrawMode & DrawLayers.SeaWind) != 0) {
			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					Color color = seaWindGradient.Sample(world.GetSeaWind(x, y)) * colors[x + y * width];
					colors[x + y * width] = color;
				}
			}
		}
		if ((DrawMode & DrawLayers.Elevation) != 0) {
			for (int x = 0; x < world.Width; x++) {
				for (int y = 0; y < world.Height; y++) {
					const float eleColorThreshold = 0.03f;
					const float ldarkAmount = 0.15f;
					float ele = world.GetElevation(x, y);
					float eleAbove = world.GetElevation(x - 1, y + 1);
					Color color = colors[x + y * width] * elevationGradient.Sample(world.GetElevation(x, y) * 0.5f + 0.5f);
					if (x > 0 && x < world.Width - 1 && y > 0 && y < world.Height - 1) {
						if (eleAbove - ele > eleColorThreshold) color = color.Darkened(ldarkAmount * (eleAbove - ele) / eleColorThreshold * 0.5f);
						else if (eleAbove - ele < -eleColorThreshold) color = color.Lightened(ldarkAmount * (eleAbove - ele) / -eleColorThreshold * 0.5f);
					}
					colors[x + y * width] = color;
				}

			}
		}
		ColorsToBytes(colors, imgbytes);
		worldImage.SetData(width, height, false, Image.Format.Rgba8, imgbytes);
		UpdateImage(worldImage, groundSprite);
	}

	public void DrawRegions(Region[] regions) {
		if (regions == null) return;
		var image = GetImage(RegionSprite);
		foreach (var region in regions) {
			var color = region.LocalFaction?.Color.Lightened(0.5f) ?? Color.FromHsv(region.WorldIndex / (float)regions.Length, 1f, 1f);
			if (region.LocalFaction != null && region.LocalFaction.HasOwningFaction()) color = region.LocalFaction.GetOwningFaction().Region.LocalFaction.Color.Darkened(0.25f);
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
