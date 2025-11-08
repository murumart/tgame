using System;
using System.Collections.Generic;
using Godot;
using static Building;
using static ResourceStorage;
using resources.game.resource_types;

namespace resources.game.building_types {

	[GlobalClass]
	public partial class BuildingType : Resource, IBuildingType, IScenePathetic {

		[Export] string name;
		public string Name => name;

		string IAssetType.AssetTypeName => "building";

		[Export] int PopulationCapacity;
		[Export] Godot.Collections.Dictionary<ResourceType, int> ResourceCapacities;
		[Export] Godot.Collections.Dictionary<ResourceType, int> ResourceCosts;
		[Export] float HoursToConstruct = 1f;
		[Export] string[] AvailableJobClassNames = Array.Empty<string>();
		[Export(PropertyHint.File, "*.tscn")] string ScenePath;


		public string GetScenePath() => ScenePath;

		public int GetPopulationCapacity() {
			return PopulationCapacity;
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
			var arr = new ResourceBundle[ResourceCosts.Count];
			int i = 0;
			foreach (var pair in ResourceCosts) {
				arr[i++] = new ResourceBundle(pair.Key, pair.Value);
			}
			return arr;
		}

		public float GetHoursToConstruct() {
			return HoursToConstruct;
		}

		public IEnumerable<Job> GetAvailableJobs() {
			List<Job> jobs = new();
			foreach (string typename in AvailableJobClassNames) {
				Type type = Type.GetType(typename);
				Job job = (Job)Activator.CreateInstance(type);
				jobs.Add(job);
			}
			return jobs;
		}

	}

}
