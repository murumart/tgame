using Godot;

namespace scenes.region;

public partial class OffsettableTilemap : TileMapLayer {

	internal Region region;
	internal World world;
	internal bool takeIn;
	internal Gradient heightColorGradient;

	readonly static Color BaseColor = Color.FromHtml("d5d6db");

	public override bool _UseTileDataRuntimeUpdate(Vector2I coords) => takeIn;

	public override void _TileDataRuntimeUpdate(Vector2I coords, TileData tileData) {
		var gpos = region.WorldPosition + coords;
		float elevation = world.GetElevation(gpos.X, gpos.Y);
		int voffset = Tilemaps.TileElevationVerticalOffset(elevation);
		tileData.TextureOrigin = new Vector2I(tileData.TextureOrigin.X, voffset - 8);
		tileData.YSortOrigin = -voffset;
		tileData.Modulate = heightColorGradient.Sample(elevation * 0.5f + 0.5f);
	}

}
