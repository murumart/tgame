using Godot;
using System;
using IBuildingType = Building.IBuildingType;

namespace scenes.region.buildings {
	[GlobalClass]
	public partial class BuildingType : Resource, IBuildingType {
		[Export] string Name;
		[Export] int PopulationCapacity;
		[Export(PropertyHint.File, "*.tscn")] string ScenePath;

		public BuildingType() {
			GD.Print(ScenePath);
		}

		public string GetScenePath() {
			return ScenePath;
		}

		public int GetPopulationCapacity() {
			return PopulationCapacity;
		}

		public new string GetName() { // hiding Resource.GetName ??
			return Name;
		}
	}
}
