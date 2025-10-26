using Godot;
using Godot.Collections;
using scenes.map;
using static Building;
using static ResourceStorage;

namespace scenes.region.buildings {

	[GlobalClass]
	public partial class BuildingType : Resource, IBuildingType {

		[Export] string Name;
		[Export] int PopulationCapacity;
		[Export] Dictionary<ResourceType, int> ResourceCapacities;
		[Export] Dictionary<ResourceType, int> ResourceCosts;
		[Export(PropertyHint.File, "*.tscn")] string ScenePath;


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
				arr[i++] = new ResourceCapacity(pair.Key, pair.Value);
			}
			return arr;
		}

		public ResourceBundle[] GetResourceRequirements() {
			var arr = new ResourceBundle[ResourceCapacities.Count];
			int i = 0;
			foreach (var pair in ResourceCapacities) {
				arr[i++] = new ResourceBundle(pair.Key, pair.Value);
			}
			return arr;
		}

	}
	
}
