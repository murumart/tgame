using Godot;
using scenes.autoload;
using System;

namespace scenes.region.view.buildings {

	[GlobalClass]
	public partial class BuildingView : Node2D {
		[Signal] public delegate void BuildingClickedEventHandler(BuildingView buildingView);

		// ui things
		[Export] CollisionObject2D clickDetector;
		// internal
		Building building; public Building Building { get => building; }
		bool initialised = false;

		public override void _Ready() {
			clickDetector.InputEvent += OnInputEvent;
			clickDetector.MouseEntered += OnMouseHoverOn;
			clickDetector.MouseExited += OnMouseHoverOff;
		}

		private void OnMouseHoverOff() {
			if (!initialised) return;
			UILayer.HideInfopanel();
		}

		private void OnMouseHoverOn() {
			if (!initialised) return;
			UILayer.DisplayInfopanel(this);
		}

		public void OnInputEvent(Node viewport, InputEvent evt, long shapeIdx) {
			if (!initialised) return;
			if (evt is InputEventMouseButton mouseEvent) {
				if (mouseEvent.IsPressed() && mouseEvent.ButtonIndex == MouseButton.Left) {
					EmitSignal(SignalName.BuildingClicked, this);
				}
			}
		}

		public void Initialise(Building building) {
			this.building = building;
			clickDetector.SetDeferred("input_pickable", true);
			initialised = true;
		}
	}

}
