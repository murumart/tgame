using Godot;

namespace scenes.region;

// There's an option in Godot to change tile parameters individually, so
// I tried using to show different elevation values.
// It looks effective, but I didn't want to spend time figuring out
// how to properly calculate the cursor position and how to
// hide map objects that are behind some elevated tile.
// Maybe will return to this later... ? (which you know means Most Likely Not Ever)

public partial class OffsettableTilemap : TileMapLayer {

	public Region region;
	public World world;
	public bool takeIn;

	public override bool _UseTileDataRuntimeUpdate(Vector2I coords) => takeIn;

	public override void _TileDataRuntimeUpdate(Vector2I coords, TileData tileData) {
		var gpos = region.WorldPosition + coords;
		tileData.TextureOrigin = new(tileData.TextureOrigin.X, Tilemaps.TileElevationVerticalOffset(gpos, world) - 8);
	}

}
