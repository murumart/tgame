using Godot;
using System;

namespace scenes.region.view.buildings {

	[GlobalClass]
	public partial class BuildingView : Node2D {
		[Export] public BuildingType BuildingType;
		Building building;

		public void Initialise(Building building) {
			this.building = building;
		}
	}

}
