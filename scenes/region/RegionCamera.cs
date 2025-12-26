using Godot;
using scenes.autoload;
using scenes.region.ui;

namespace scenes.region {

	public partial class RegionCamera : Camera {

		[Export] Node2D cursor;
		[Export] RegionDisplay regionDisplay;
		[Export] UI ui;

		public Region Region;


		public override void _Ready() {
			RemoveChild(ui);
			UILayer.AddUIChild(ui);
			ClickedMouseEvent += (ac) => ui.OnLeftMouseClick(ac, regionDisplay.LocalToTile(ac));
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
			var tilepos = regionDisplay.GetMouseHoveredTilePos();
			cursor.GlobalPosition = Tilemaps.TilePosToWorldPos(tilepos);
			if (tilepos != lastTilePos) {
				lastTilePos = tilepos;
				ui.OnTileHighlighted(tilepos, Region);
			}
		}

	}

}
