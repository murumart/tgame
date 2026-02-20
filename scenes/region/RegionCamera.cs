using Godot;
using scenes.autoload;
using scenes.region.ui;

namespace scenes.region {

	public partial class RegionCamera : Camera {

		[Export] public Node2D Cursor;
		[Export] Node2D debugCursor;
		[Export] RegionDisplay regionDisplay;
		[Export] UI ui;

		public Region Region;


		public override void _Ready() {
			base._Ready();
			RemoveChild(ui);
			UILayer.AddUIChild(ui);
			ClickedMouseEvent += (ac) => ui.OnLeftMouseClick(ac, regionDisplay.GetMouseHoveredTilePos(ac));
		}

		public override void _Process(double delta) {
			base._Process(delta);
			MouseHighlight();
		}

		protected override bool MouseButtonInput(InputEventMouseButton evt) {
			if (base.MouseButtonInput(evt)) return true;
			if (evt.ButtonIndex == MouseButton.Right && evt.IsPressed()) {
				var wPos = GetCanvasTransform().AffineInverse() * evt.Position;
				ui.OnRightMouseClick(wPos, regionDisplay.LocalToTile(wPos));
				return true;
			}
			return false;
		}

		private Vector2I lastTilePos;
		private void MouseHighlight() {
			debugCursor.Visible = !dragging;
			Cursor.Visible = !dragging;
			var tilepos = regionDisplay.GetMouseHoveredTilePos();
			debugCursor.GlobalPosition = Tilemaps.TilePosToWorldPos(tilepos);
			Cursor.GlobalPosition = Tilemaps.TilePosToWorldPos(tilepos) + Vector2.Up * Tilemaps.TileElevationVerticalOffset(GameMan.Singleton.Game.PlayRegion.WorldPosition + tilepos, GameMan.Singleton.Game.Map.World);
			if (tilepos != lastTilePos) {
				lastTilePos = tilepos;
				ui.OnTileHighlighted(tilepos, Region);
			}
		}

	}

}
