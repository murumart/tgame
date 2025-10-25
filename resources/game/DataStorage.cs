using Godot;
using Godot.Collections;
using static Building;

namespace scenes {

	[GlobalClass]
	public partial class DataStorage : Resource {

		[Export] Array<map.ResourceType> resourceTypes;
		[Export] Array<region.buildings.BuildingType> buildingTypes;


		public void RegisterThings() {
			var resources = new IResourceType[resourceTypes.Count];
			for (int i = 0; i < resourceTypes.Count; i++) resources[i] = resourceTypes[i];
			var buildings = new IBuildingType[buildingTypes.Count];
			for (int i = 0; i < buildingTypes.Count; i++) buildings[i] = buildingTypes[i];
			Registry.Register(resources, buildings);
		}

	}


}
