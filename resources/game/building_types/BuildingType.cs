using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using resouces.game.building_types.crafting;
using resources.game.resource_types;
using static Building;

namespace resources.game.building_types {

	[GlobalClass]
	public partial class BuildingType : Resource, IBuildingType, IScenePathetic {

		[Export] string name;
		public string AssetName => name;
		[Export(PropertyHint.MultilineText)] string description;

		string IAssetType.AssetTypeName => "building";

		[Export] int PopulationCapacity;
		//[Export] Godot.Collections.Dictionary<ResourceType, int> ResourceCapacities;
		[Export] Godot.Collections.Dictionary<ResourceType, int> ResourceCosts;
		[Export] Godot.Collections.Array<CraftingJobDef> CraftingJobs;
		[Export] float HoursToConstruct = 1f;
		[Export] string[] AvailableJobClassNames = Array.Empty<string>();
		[Export(PropertyHint.File, "*.tscn")] string ScenePath;


		public string GetScenePath() => ScenePath;

		public int GetPopulationCapacity() {
			Debug.Assert(PopulationCapacity >= 0, "Population capacity can't be negative");
			return PopulationCapacity;
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

		public IEnumerable<Job> GetPossibleJobs() {
			List<Job> jobs = new();
			if (CraftingJobs != null && CraftingJobs.Count != 0) {
				jobs.AddRange(GetCraftJobs());
			}
			foreach (string typename in AvailableJobClassNames) {
				Type type = Type.GetType(typename);
				Job job = (Job)Activator.CreateInstance(type);
				jobs.Add(job);
			}
			return jobs;
		}

		public CraftJob[] GetCraftJobs() {
			if (CraftingJobs.Count == 0) return Array.Empty<CraftJob>();
			return CraftingJobs.Select(def => def.GetJob()).ToArray();
		}

		public string GetDescription() => description;
	}

}
