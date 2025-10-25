using Godot;
using scenes.autoload;
using scenes.region.ui;
using System;

namespace scenes.region {

	public partial class Camera : Camera2D {

		readonly static Vector2I TILE_SIZE = new(64, 32);

		const float SPEED = 360.0f;
		const float ACCEL = 60.0f;
		const float DECEL = 20.0f;

		[Export] Node2D cursor;
		[Export] TileMapLayer regionTiles;
		[Export] UI ui;

		public Region Region;

		Vector2 velocity = new();
		float zoomSize = 1.0f;

		public override void _Ready() {
			RemoveChild(ui);
			UILayer.Singleton.AddChild(ui);
		}

		public override void _UnhandledInput(InputEvent evt) {
			if (evt is InputEventMouseButton) {
				var bEvent = evt as InputEventMouseButton;
				if (bEvent.ButtonIndex == MouseButton.WheelUp) {
					zoomSize = Mathf.Min(zoomSize + 0.1f * zoomSize, 8.0f);
				} else if (bEvent.ButtonIndex == MouseButton.WheelDown) {
					zoomSize = Mathf.Max(zoomSize - 0.1f * zoomSize, 0.25f);
				} else if (bEvent.ButtonIndex == MouseButton.Left && evt.IsPressed()) {
					var wPos = GetCanvasTransform().AffineInverse() * bEvent.Position;
					ui.OnLeftMouseClick(wPos, PosToTilePos(wPos));
				} else if (bEvent.ButtonIndex == MouseButton.Right && evt.IsPressed()) {
					var wPos = GetCanvasTransform().AffineInverse() * bEvent.Position;
					ui.OnRightMouseClick(wPos, PosToTilePos(wPos));
				}
				if (bEvent.ButtonIndex != MouseButton.None) Zoom = new Vector2(zoomSize, zoomSize);
			}
		}

		public override void _Process(double delta) {
			Movement((float)delta);
			MouseHighlight();
		}

		private void Movement(float delta) {
			float speed = SPEED / zoomSize;
			velocity = velocity.MoveToward(Vector2.Zero, delta * DECEL);
			if (Input.IsKeyPressed(Key.Shift)) {
				velocity = Vector2.Zero;
				speed *= 0.5f;
			}

			Vector2 dir = Input.GetVector("left", "right", "up", "down");
			if (dir.X != 0.0f) velocity.X = Mathf.MoveToward(velocity.X, dir.X * speed * delta, delta * ACCEL);
			if (dir.Y != 0.0f) velocity.Y = Mathf.MoveToward(velocity.Y, dir.Y * speed * delta, delta * ACCEL);

			Position += velocity;
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
