using System;
using Godot;
using scenes.autoload;

namespace scenes.region;

public partial class Tilemaps : Node2D {

	public readonly static Vector2I TILE_SIZE = new(64, 32);

	[Export] Gradient heightColorGradient;
	[Export] OffsettableTilemap ground; public TileMapLayer Ground => ground;


	public override void _Ready() {

	}

	public void DisplayGround(Region from) {
		var watch = System.Diagnostics.Stopwatch.StartNew();
		ground.Clear();
		// see OffsettableTilemap.cs
		foreach (var pair in from.GroundTiles) {
			var type = GroundCellType.MatchTileTypeToCell(pair.Value);
			ground.SetCell(pair.Key, type.SourceId, type.AtlasCoords);
		}
		ground.takeIn = true;
		ground.world = GameMan.Singleton.Game.Map.World;
		ground.region = from;
		ground.heightColorGradient = heightColorGradient;
		ground.UpdateInternals();
		ground.takeIn = false;
		watch.Stop();
		var elapsedMs = watch.ElapsedMilliseconds;
		GD.Print("Tilemaps::DisplayGround : displaying ground took " + elapsedMs + " ms");
	}

	// matches Godot's TileLayout.DIAMOND_DOWN
	public static Vector2 TilePosToWorldPos(Vector2I tilePos) {
		var halfTs = TILE_SIZE / 2;
		return new Vector2(
			halfTs.X + tilePos.X * halfTs.X - tilePos.Y * halfTs.X,
			halfTs.Y + tilePos.X * halfTs.Y + tilePos.Y * halfTs.Y
		);
	}

	// matches Godot's TileLayout.STACKED
	public static Vector2 TilePosToWorldPosStackedMode(Vector2I tilePos) {
		var halfTs = TILE_SIZE / 2;
		var tilecenter = new Vector2(tilePos.X, tilePos.Y / 2) * TILE_SIZE + halfTs;
		if (tilePos.Y % 2 != 0) {
			tilecenter.X += halfTs.X;
			tilecenter.Y += (tilePos.Y > 0) ? halfTs.Y : -halfTs.Y;
		}
		return tilecenter;
	}

	public const int TILE_ELE_HEIGHT_MULTIPLIER = 8;
	public static int TileElevationVerticalOffset(Vector2I globalTilePos, World world) {
		return TileElevationVerticalOffset(world.GetElevation(globalTilePos.X, globalTilePos.Y));
	}

	public static int TileElevationVerticalOffset(float elevation) {
		var visualEle = Mathf.Max(0f, elevation);
		return (int)(visualEle * TILE_SIZE.Y * TILE_ELE_HEIGHT_MULTIPLIER);
	}

}

public class GroundCellType {

	public readonly static GroundCellType VOID = new() {
		SourceId = 1,
		AtlasCoords = Vector2I.Zero,
	};
	public readonly static GroundCellType LAND = new() {
		SourceId = 1,
		AtlasCoords = new Vector2I(3, 0),
	};
	public readonly static GroundCellType GRASS = new() {
		SourceId = 1,
		AtlasCoords = Vector2I.Zero,
	};
	public readonly static GroundCellType SANDY_GRASS = new() {
		SourceId = 1,
		AtlasCoords = Vector2I.One,
	};
	public readonly static GroundCellType WATER_BED = new() {
		SourceId = 1,
		AtlasCoords = new Vector2I(1, 2),
	};
	public readonly static GroundCellType SAND = new() {
		SourceId = 1,
		AtlasCoords = new Vector2I(1, 0),
	};
	public readonly static GroundCellType SNOW = new() {
		SourceId = 1,
		AtlasCoords = new Vector2I(0, 1),
	};
	public readonly static GroundCellType WATER = new() {
		SourceId = 1,
		AtlasCoords = new Vector2I(2, 0),
	};

	public int SourceId;
	public Vector2I AtlasCoords;


	public static GroundCellType MatchTileTypeToCell(GroundTileType t) {
		if ((t & GroundTileType.HasLand) == 0) {
			if ((t & GroundTileType.HasVeg) != 0) return GroundCellType.WATER_BED;
			return GroundCellType.WATER;
		}
		if ((t & GroundTileType.HasLand) != 0) {
			if ((t & GroundTileType.HasSand) != 0 && (t & GroundTileType.HasVeg) != 0) return GroundCellType.SANDY_GRASS;
			if ((t & GroundTileType.HasSand) != 0) return GroundCellType.SAND;
			if ((t & GroundTileType.HasVeg) != 0) return GroundCellType.GRASS;
			if ((t & GroundTileType.HasSnow) != 0) return GroundCellType.SNOW;
			return GroundCellType.LAND;
		}
		Debug.Assert(false, "Unimplemented ground type tile");
		return GroundCellType.VOID;

	}

}


