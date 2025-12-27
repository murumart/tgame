using Godot;
using System;

namespace scenes.region {

	public partial class Tilemaps : Node2D {

		public readonly static Vector2I TILE_SIZE = new(64, 32);

		[Export] TileMapLayer ground; public TileMapLayer Ground => ground;


		public override void _Ready() {

		}

		public void DisplayGround(Region from) {
			var watch = System.Diagnostics.Stopwatch.StartNew();
			ground.Clear();
			foreach (var pair in from.GroundTiles) {
				var type = GroundCellType.MatchTileTypeToCell(pair.Value);
				ground.SetCell(pair.Key, type.SourceId, type.AtlasCoords);
			}
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

	}

	class GroundCellType {

		public readonly static GroundCellType VOID = new() {
			SourceId = 1,
			AtlasCoords = Vector2I.Zero,
		};
		public readonly static GroundCellType GRASS = new() {
			SourceId = 1,
			AtlasCoords = Vector2I.Zero,
		};
		public readonly static GroundCellType SAND = new() {
			SourceId = 1,
			AtlasCoords = new Vector2I(1, 0),
		};
		public readonly static GroundCellType WATER = new() {
			SourceId = 1,
			AtlasCoords = new Vector2I(2, 0),
		};

		public int SourceId;
		public Vector2I AtlasCoords;


		public static GroundCellType MatchTileTypeToCell(GroundTileType tile) {
			return tile switch {
				GroundTileType.Void => VOID,
				GroundTileType.Grass => GRASS,
				GroundTileType.Sand => SAND,
				GroundTileType.Ocean => WATER,
				_ => throw new Exception($"Can't match {tile} to CellType")
			};
		}

	}

}
