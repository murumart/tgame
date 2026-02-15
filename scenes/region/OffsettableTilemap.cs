using Godot;

namespace scenes.region;

public partial class OffsettableTilemap : TileMapLayer {

	public Region region;
	public World world;
	public bool takeIn;

	readonly static Color BaseColor = Color.FromHtml("d5d6db");

	public override bool _UseTileDataRuntimeUpdate(Vector2I coords) => takeIn;

	public override void _TileDataRuntimeUpdate(Vector2I coords, TileData tileData) {
		var gpos = region.WorldPosition + coords;
		var voffset = Tilemaps.TileElevationVerticalOffset(gpos, world);
		tileData.TextureOrigin = new Vector2I(tileData.TextureOrigin.X, voffset - 8);
		tileData.YSortOrigin = -voffset;
		tileData.Modulate = new Color(BaseColor + Colors.White * voffset / (Tilemaps.TILE_SIZE.Y * Tilemaps.TILE_ELE_HEIGHT_MULTIPLIER) * 0.45f, 1f);
	}

}
