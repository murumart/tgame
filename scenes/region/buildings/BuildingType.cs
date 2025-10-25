using System;
using Godot;
using Godot.Collections;
using static ResourceStorage;
using static ResourceType;
using IBuildingType = Building.IBuildingType;

namespace scenes.region.buildings {
	[GlobalClass]
	public partial class BuildingType : Resource, IBuildingType {
		[Export] string Name;
		[Export] int PopulationCapacity;
		[Export] Dictionary<int, int> ResourceCapacities;
		[Export] Dictionary<int, int> ResourceCosts;
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

		public ResourceCapacity[] GetResourceCapacities() {
			var arr = new ResourceCapacity[ResourceCapacities.Count];
			int i = 0;
			foreach (var pair in ResourceCapacities) {
				arr[i++] = new ResourceCapacity(ResourceRegistry.GetResourceType(pair.Key), pair.Value);
			}
			return arr;
		}

		public ResourceBundle[] GetResourceRequirements() {
			var arr = new ResourceBundle[ResourceCapacities.Count];
			int i = 0;
			foreach (var pair in ResourceCapacities) {
				arr[i++] = new ResourceBundle(ResourceRegistry.GetResourceType(pair.Key), pair.Value);
			}
			return arr;
		}
	}
}
