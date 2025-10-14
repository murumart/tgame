using Godot;
using System;

namespace scenes.region.view.buildings {
	[GlobalClass]
	public partial class BuildingType : Resource {
		[Export] public string Name;
		[Export] public int PopulationCapacity;
		[Export(PropertyHint.File, "*.tscn")] public string ScenePath;

		public BuildingType() {
			GD.Print(ScenePath);
		}
	}
}
