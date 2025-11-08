using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using resources.game.building_types;
using resources.game.resource_site_types;
using resources.game.resource_types;
using static Building;
using static ResourceSite;

namespace resources.game {

	[GlobalClass]
	public partial class DataStorage : Resource {

		private static readonly System.Collections.Generic.Dictionary<string, string> scenePaths = new();

		[Export] Array<ResourceType> resourceTypes;
		[Export] Array<BuildingType> buildingTypes;
		[Export] Array<ResourceSiteType> resourceSiteTypes;


		public void RegisterThings() {
			var resources = new IResourceType[resourceTypes.Count];
			for (int i = 0; i < resourceTypes.Count; i++) resources[i] = resourceTypes[i];

			var buildings = new IBuildingType[buildingTypes.Count];
			for (int i = 0; i < buildingTypes.Count; i++) buildings[i] = buildingTypes[i];
			RegisterScenePaths<BuildingType>(buildingTypes);

			var mines = new IResourceSiteType[resourceSiteTypes.Count];
			for (int i = 0; i < resourceSiteTypes.Count; i++) mines[i] = resourceSiteTypes[i];
			RegisterScenePaths<ResourceSiteType>(resourceSiteTypes);

			Registry.Register(resources, buildings, mines);

			GD.Print("REGISTERED FOLLOWING BUILDINGS");
			foreach (var ass in Registry.Buildings.GetIdAssetPairs()) {
				GD.PrintT(ass.Key, ass.Value.Name, $"scenePath {scenePaths.GetValueOrDefault(ass.Key)}");
			}
			GD.Print("REGISTERED FOLLOWING RESOURCES");
			foreach (var ass in Registry.Resources.GetIdAssetPairs()) {
				GD.PrintT(ass.Key, ass.Value.Name);
			}
			GD.Print("REGISTERED FOLLOWING RESOURCE SITES");
			foreach (var ass in Registry.ResourceSites.GetIdAssetPairs()) {
				GD.PrintT(ass.Key, ass.Value.Name);
			}

		}

		static void RegisterScenePaths<U>(IEnumerable<U> scenics) where U : IAssetType, IScenePathetic {
			foreach (var ass in scenics) {
				var key = ass.GetIdString();
				Debug.Assert(!scenePaths.ContainsKey(key), $"{key} already has scenePath {scenePaths.GetValueOrDefault(key)}");
				Debug.Assert(ass.GetScenePath() != null, $"{ass} has no scene path!");
				scenePaths[key] = ass.GetScenePath();
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

public interface IScenePathetic {

	string GetScenePath();

}
