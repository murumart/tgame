using Godot;
using System;

namespace scenes.region {

	public partial class Tilemaps : Node2D {

		[Export] TileMapLayer ground;

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
			GD.Print("TILEMAPS: displaying ground took " + elapsedMs + " ms");
		}
	}

	class GroundCellType {

		public readonly static GroundCellType GRASS = new() {
			SourceId = 1,
			AtlasCoords = Vector2I.Zero
		};
		public readonly static GroundCellType VOID = new() {
			SourceId = 1,
			AtlasCoords = Vector2I.Zero
		};

		public int SourceId;
		public Vector2I AtlasCoords;

		public static GroundCellType MatchTileTypeToCell(GroundTileType tile) {
			return tile switch {
				GroundTileType.VOID => VOID,
				GroundTileType.GRASS => GRASS,
				_ => throw new Exception($"Can't match {tile} to CellType")
			};
		}
	}

}
