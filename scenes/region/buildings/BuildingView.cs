using Godot;
using scenes.autoload;

namespace scenes.region.buildings {

	[GlobalClass]
	public partial class BuildingView : MapObjectView {

		// ui things
		[Export] CollisionObject2D clickDetector;
		// internal
		Building building; public Building Building { get => building; }
		bool initialised = false;


		public override void _Ready() {
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

		public void Initialise(Building building) {
			this.building = building;
			clickDetector.SetDeferred("input_pickable", true);
			initialised = true;
		}

	}

}
