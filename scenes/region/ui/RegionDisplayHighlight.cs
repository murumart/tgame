using System;
using System.Collections.Generic;
using Godot;

namespace scenes.region.ui;

using DisplayFunc = Action<Sprite2D, Vector2I, Vector2I, float>;

public partial class RegionDisplayHighlight : Node2D {

	const int maxRadius = 10;
	static readonly Texture2D IsoTexture = GD.Load<Texture2D>("uid://ethlsfg8xmhk");

	readonly Dictionary<Vector2I, Sprite2D> spritesByTile = new();

	DisplayFunc displayFunc = DefaultDisplay;


	public override void _Ready() {
		for (int x = -maxRadius; x < maxRadius; x++) {
			for (int y = -maxRadius; y < maxRadius; y++) {
				if (x * x + y * y > maxRadius * maxRadius) continue;
				var s = new Sprite2D {
					Texture = IsoTexture,
					Position = Tilemaps.TilePosToWorldPos(new(x, y)),
				};
				AddChild(s);
				spritesByTile[new(x, y)] = s;
			}
		}
		TransparentiseAll();
	}

	public void TransparentiseAll() {
		foreach (var s in spritesByTile.Values) s.Modulate = new(s.Modulate, 0f);
	}

	public void Update(World world, Vector2I centerLocalPosition, Vector2I centerWorldPosition, int radius = maxRadius) {
		if (displayFunc == null) return;
		Position = Tilemaps.TilePosToWorldPos(centerLocalPosition); // individual tiles get world offset
		for (int x = -radius; x < radius; x++) {
			for (int y = -radius; y < radius; y++) {
				int dist = x * x + y * y;
				if (dist > radius * radius) continue;
				Vector2I wpos = new(x + centerWorldPosition.X, y + centerWorldPosition.Y);
				Vector2I lpos = new(x, y);
				float eleoff = Tilemaps.TileElevationVerticalOffset(wpos, world);
				var s = spritesByTile[lpos];
				s.Position = Tilemaps.TilePosToWorldPos(lpos) + Vector2.Up * eleoff;
				displayFunc(s, wpos, lpos, MathF.Sqrt(dist));
			}
		}
	}

	public void SetDisplay(DisplayFunc display = null) {
		this.displayFunc = display;
	}

	public static void DefaultDisplay(Sprite2D sprite, Vector2I worldPosition, Vector2I radiusPosition, float distance) {
		//sprite.Modulate = new(Colors.White, 1 - distance / maxRadius);
	}

}
