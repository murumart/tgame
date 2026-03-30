using System;
using Godot;
using scenes.autoload;

public partial class Camera : Camera2D {

	[Export] public bool CanMoveWithKeyboard = true;

	public event Action<Vector2I> MovedMouseEvent;
	public event Action<Vector2I> ClickedMouseEvent;
	public event Action ZoomChanged;

	[Export] float SPEED = 360.0f;
	[Export] float ACCEL = 34.0f;
	[Export] float DECEL = 14.0f;

	bool returnMousePosition = false;
	Vector2 lastMousePosition;
	Vector2 velocity = new();
	float zoomSize = 1.0f;

	public override void _Ready() {
		UILayer.DebugDisplay(() => $"dragging: {dragging}, start: {draggingStartPos}");
	}

	public override void _Process(double delta) {
		if (CanMoveWithKeyboard) Movement((float)delta);
		var oldzoom = Zoom;
		Zoom = new Vector2(zoomSize, zoomSize);
		if (oldzoom != Zoom) {
			ZoomChanged?.Invoke();
		}
		Position = GetScreenCenterPosition();
	}

	public override void _UnhandledInput(InputEvent evt) {
		if (evt is InputEventMouseButton bEvent) {
			MouseButtonInput(bEvent);
		} else if (evt is InputEventKey kEvent) {
			if (kEvent.Keycode == Key.Space) {
				if (kEvent.IsReleased() && dragging) StopDragging();
				else if (kEvent.IsPressed() && !dragging) {
					StartDragging(true);
				}
			}
		} else if (evt is InputEventMouseMotion mEvent) {
			if (dragging) {
				Input.SetDefaultCursorShape(Input.CursorShape.Drag);
				var wPos = mEvent.Position;
				//var differ = (draggingStartPos - wPos) / zoomSize;
				var differ = -mEvent.Relative / zoomSize;
				Position = draggingStartCamPos + differ;
				lastMousePosition = mEvent.Position;
				draggingStartCamPos = Position;
				MovedMouseEvent?.Invoke((Vector2I)mEvent.Position);
			} else Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
		}
	}

	protected virtual void ScrollInput(bool up) { }

	protected virtual void ControlScrollInput(bool up) {
		if (up) ZoomIn();
		else ZoomOut();
	}

	public void ZoomIn(float amt = 0.1f) => zoomSize = Mathf.Min(zoomSize + amt * zoomSize, 8.0f);
	public void ZoomOut(float amt = 0.1f) => zoomSize = Mathf.Max(zoomSize - amt * zoomSize, 0.1f);
	public void ZoomReset() => zoomSize = 1f;

	public void StartDragging(bool returnMousePosition = false) {
		this.returnMousePosition = returnMousePosition;
		dragging = true;
		draggingStartPos = GetViewport().GetMousePosition();
		draggingStartCamPos = Position;
		if (returnMousePosition) DisplayServer.MouseSetMode(DisplayServer.MouseMode.Captured);
	}

	public void StopDragging() {
		dragging = false;
		DisplayServer.MouseSetMode(DisplayServer.MouseMode.Visible);
		if (draggingStartPos != Vector2.Zero && returnMousePosition) GetViewport().WarpMouse((Vector2I)draggingStartPos);
	}

	protected bool dragging = false;
	Vector2 draggingStartPos;
	Vector2 draggingStartCamPos;
	protected virtual bool MouseButtonInput(InputEventMouseButton evt) {

		bool consumed = false;
		if (evt.ButtonIndex == MouseButton.WheelUp) {
			if (evt.IsCommandOrControlPressed()) ControlScrollInput(true);
			else ScrollInput(true);
			consumed = true;
		} else if (evt.ButtonIndex == MouseButton.WheelDown) {
			if (evt.IsCommandOrControlPressed()) ControlScrollInput(false);
			else ScrollInput(false);
			consumed = true;
		} else if (evt.ButtonIndex == MouseButton.Left && evt.Pressed && !dragging) {
			var wPos = GetCanvasTransform().AffineInverse() * evt.Position;
			ClickedMouseEvent?.Invoke((Vector2I)wPos);
			return true;
		} else if (evt.ButtonIndex == MouseButton.Middle) {
			if (evt.Pressed) {
				StartDragging();
			} else {
				StopDragging();
			}
		}
		if (consumed) GetWindow().SetInputAsHandled();
		return consumed;

	}

	public Vector2 GetMousePos() {
		return GetCanvasTransform().AffineInverse() * GetViewport().GetMousePosition();
	}

	private void Movement(float delta) {
		float speed = SPEED / zoomSize;
		if (Input.IsKeyPressed(Key.Shift)) {
			velocity = Vector2.Zero;
			speed *= 0.5f;
		}

		Vector2 dir = Input.GetVector("left", "right", "up", "down");
		if (dir.X != 0.0f) velocity.X = Mathf.MoveToward(velocity.X, dir.X * speed * delta, delta * (ACCEL / zoomSize));
		else velocity.X = Mathf.MoveToward(velocity.X, 0f, Mathf.Abs(delta * velocity.X) * DECEL);
		if (dir.Y != 0.0f) velocity.Y = Mathf.MoveToward(velocity.Y, dir.Y * speed * delta, delta * (ACCEL / zoomSize));
		else velocity.Y = Mathf.MoveToward(velocity.Y, 0f, Mathf.Abs(delta * velocity.Y) * DECEL);

		Vector2 newPos = Position;
		newPos.X += velocity.X;
		newPos.Y += velocity.Y;

		Position = newPos;
		//Position = Position.Clamp(new Vector2(LimitLeft, LimitTop), new Vector2(LimitRight, LimitBottom));
	}

}
