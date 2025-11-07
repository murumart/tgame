using System.Collections;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using resources.game.building_types;
using resources.game.resource_types;
using static Building;

namespace resources.game {

	[GlobalClass]
	public partial class DataStorage : Resource {

		private static readonly System.Collections.Generic.Dictionary<string, string> scenePaths = new();

		[Export] Array<ResourceType> resourceTypes;
		[Export] Array<BuildingType> buildingTypes;


		public void RegisterThings() {
			var resources = new IResourceType[resourceTypes.Count];
			for (int i = 0; i < resourceTypes.Count; i++) resources[i] = resourceTypes[i];

			var buildings = new IBuildingType[buildingTypes.Count];
			for (int i = 0; i < buildingTypes.Count; i++) buildings[i] = buildingTypes[i];

			foreach (var ass in buildingTypes) {
				var key = ((IBuildingType)ass).GetIdString();
				Debug.Assert(!scenePaths.ContainsKey(key), $"{key} already has scenePath {scenePaths.GetValueOrDefault(key)}");
				scenePaths[key] = ass.GetScenePath();
			}

			Registry.Register(resources, buildings);

			GD.Print("REGISTERED FOLLOWING BUILDINGS");
			foreach (var ass in Registry.Buildings.GetIdAssetPairs()) {
				GD.PrintT(ass.Key, ass.Value.Name, $"scenePath {scenePaths.GetValueOrDefault(ass.Key)}");
			}
			GD.Print("REGISTERED FOLLOWING RESOURCES");
			foreach (var ass in Registry.Resources.GetIdAssetPairs()) {
				GD.PrintT(ass.Key, ass.Value.Name);
			}

		}

		public static string GetScenePath(IAssetType assetType) {
			Debug.Assert(scenePaths.ContainsKey(assetType.GetIdString()), $"Scene path for asset {assetType} missing");
			return GetScenePath(assetType.GetIdString());
		}

		public static string GetScenePath(string key) {
			Debug.Assert(scenePaths.ContainsKey(key), $"Scene path for key {key} missing");
			return scenePaths[key];
		}

	}


}
