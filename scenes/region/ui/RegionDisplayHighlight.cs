global using RegionDisplayHighlightDisplayFunc = System.Action<Godot.Sprite2D, Godot.Vector2I, Godot.Vector2I, float>;

using System;
using System.Collections.Generic;
using Godot;

namespace scenes.region.ui;

public partial class RegionDisplayHighlight : Node2D {

	const int maxRadius = 24;
	static readonly Texture2D IsoTexture = GD.Load<Texture2D>("uid://ethlsfg8xmhk");

	readonly Dictionary<Vector2I, Sprite2D> spritesByTile = new();

	RegionDisplayHighlightDisplayFunc displayFunc = DefaultDisplay;


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
				int sqdist = x * x + y * y;
				if (sqdist > radius * radius) continue;
				var wpos = centerWorldPosition + new Vector2I(x, y);
				var lpos = new Vector2I(x, y);
				var s = spritesByTile[lpos];
				s.Position = Tilemaps.TilePosToWorldPos(lpos - Vector2I.Right/* ??? */) + Vector2.Up * Tilemaps.TileElevationVerticalOffset(wpos, world);
				displayFunc(s, wpos, lpos, MathF.Sqrt(sqdist));
			}
		}
	}

	public void SetDisplay(RegionDisplayHighlightDisplayFunc display = null) {
		this.displayFunc = display;
	}

	public static void DefaultDisplay(Sprite2D sprite, Vector2I worldPosition, Vector2I radiusPosition, float distance) {
		//sprite.Modulate = new(Colors.White, 1 - distance / maxRadius);
	}

}
