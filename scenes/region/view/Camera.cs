using Godot;
using scenes.autoload;
using System;

namespace scenes.region.view {

	public partial class Camera : Camera2D {

		readonly Vector2I TILE_SIZE = new(64, 32);

		const float SPEED = 360.0f;
		const float ACCEL = 60.0f;
		const float DECEL = 20.0f;

		[Export] public Node2D cursor;
		[Export] public TileMapLayer regionTiles;

		Vector2 velocity = new();
		float zoomSize = 1.0f;

		public override void _Ready() {
			Ui ui = (Ui)GetNode("Ui");
			RemoveChild(ui);
			UiLayer.Instance.AddChild(ui);
		}

		public override void _UnhandledInput(InputEvent evt) {
			if (evt is InputEventMouseButton) {
				MouseButton btnIx = (evt as InputEventMouseButton).ButtonIndex;
				if (btnIx == MouseButton.WheelUp) {
					zoomSize = Mathf.Min(zoomSize + 0.1f * zoomSize, 8.0f);
				} else if (btnIx == MouseButton.WheelDown) {
					zoomSize = Mathf.Max(zoomSize - 0.1f * zoomSize, 0.25f);
				}
				if (btnIx != MouseButton.None) Zoom = new Vector2(zoomSize, zoomSize);
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

		private void MouseHighlight() {
			var lmp = regionTiles.GetLocalMousePosition();
			var tp = regionTiles.LocalToMap(lmp);
			cursor.GlobalPosition = TilePosToWorldPos(tp);
		}

		private Vector2 TilePosToWorldPos(Vector2I tilePos) {
			var halfTs = TILE_SIZE / 2;
			var tilecenter = new Vector2(tilePos.X, tilePos.Y / 2) * TILE_SIZE + halfTs;
			if (tilePos.Y % 2 != 0) {
				tilecenter.X += halfTs.X;
				tilecenter.Y += (tilePos.Y > 0) ? halfTs.Y : -halfTs.Y;
			}
			return tilecenter;
		}
	}

}
