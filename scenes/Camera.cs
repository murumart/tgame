using System;
using Godot;

public partial class Camera : Camera2D {

	public event Action<Vector2I> ClickedMouseEvent;

	const float SPEED = 360.0f;
	const float ACCEL = 60.0f;
	const float DECEL = 20.0f;

	Vector2 velocity = new();
	float zoomSize = 1.0f;


	public override void _Process(double delta) {
		Movement((float)delta);
	}

	public override void _UnhandledInput(InputEvent evt) {
		if (evt is InputEventMouseButton bEvent) {
			MouseButtonInput(bEvent);
		} else if (evt is InputEventMouseMotion mEvent) {
			if (dragging) {
				var wPos = mEvent.Position;
				var differ = (draggingStartPos - wPos) / zoomSize;
				Position = draggingStartCamPos + differ;
			}
		}
	}

	protected virtual void ScrollInput(bool up) {
		if (up) zoomSize = Mathf.Min(zoomSize + 0.1f * zoomSize, 8.0f);
		else zoomSize = Mathf.Max(zoomSize - 0.1f * zoomSize, 0.25f);
	}

	bool dragging = false;
	Vector2 draggingStartPos;
	Vector2 draggingStartCamPos;
	protected virtual bool MouseButtonInput(InputEventMouseButton evt) {

		bool consumed = false;
		if (evt.ButtonIndex == MouseButton.WheelUp) {
			ScrollInput(true);
			consumed = true;
		} else if (evt.ButtonIndex == MouseButton.WheelDown) {
			ScrollInput(false);
			consumed = true;
		} else if (evt.ButtonIndex == MouseButton.Left && evt.Pressed) {
			var wPos = GetCanvasTransform().AffineInverse() * evt.Position;
			ClickedMouseEvent?.Invoke((Vector2I)wPos);
			return true;
		} else if (evt.ButtonIndex == MouseButton.Middle) {
			if (evt.Pressed) {
				dragging = true;
				draggingStartPos = evt.Position;
				draggingStartCamPos = Position;
			} else {
				dragging = false;
			}
		}
		Zoom = new Vector2(zoomSize, zoomSize);
		if (consumed) GetWindow().SetInputAsHandled();
		return consumed;

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

}