using Godot;
using scenes.autoload;
using scenes.region.ui;

namespace scenes.region {

	public partial class RegionCamera : Camera {

		readonly static Vector2I TILE_SIZE = new(64, 32);

		[Export] Node2D cursor;
		[Export] TileMapLayer regionTiles;
		[Export] UI ui;

		public Region Region;


		public override void _Ready() {
			RemoveChild(ui);
			UILayer.AddUIChild(ui);
			ClickedMouseEvent += (ac) => ui.OnLeftMouseClick(ac, PosToTilePos(ac));
		}

		public override void _Process(double delta) {
			base._Process(delta);
			MouseHighlight();
		}

		protected override bool MouseButtonInput(InputEventMouseButton evt) {
			if (base.MouseButtonInput(evt)) return true;
			if (evt.ButtonIndex == MouseButton.Right && evt.IsPressed()) {
				var wPos = GetCanvasTransform().AffineInverse() * evt.Position;
				ui.OnRightMouseClick(wPos, PosToTilePos(wPos));
				return true;
			}
			return false;
		}

		private Vector2I lastTilePos;
		private void MouseHighlight() {
			var localMousePos = regionTiles.GetLocalMousePosition();
			var tilepos = regionTiles.LocalToMap(localMousePos);
			cursor.GlobalPosition = TilePosToWorldPos(tilepos);
			if (tilepos != lastTilePos) {
				lastTilePos = tilepos;
				ui.OnTileHighlighted(tilepos, Region);
			}
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

		public Vector2I PosToTilePos(Vector2 pos) {
			return regionTiles.LocalToMap(pos);
		}
	}

}
