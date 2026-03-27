using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.game.building_types.crafting;
using resources.game.resource_types;
using static Building;
using static Building.IBuildingType;

namespace resources.game.building_types {

	[GlobalClass]
	public partial class BuildingType : Resource, IBuildingType, IScenePathetic {

		[Export] string name;
		public string AssetName => name;
		[Export(PropertyHint.MultilineText)] string description;

		string IAssetType.AssetTypeName => "building";

		string assetIDString;
		public string AssetIDString {
			get {
				assetIDString ??= name.ToSnakeCase();
				return assetIDString;
			}
		}

		[Export(PropertyHint.Range, "0,99,or_greater")] int PopulationCapacity;
		//[Export] Godot.Collections.Dictionary<ResourceType, int> ResourceCapacities;
		[Export] Godot.Collections.Dictionary<ResourceType, int> ResourceCosts;
		[Export] Godot.Collections.Array<CraftingJobDef> CraftingJobs;
		[Export(PropertyHint.Range, "0.5,100,or_greater")] float HoursToConstruct = 1f;
		[Export(PropertyHint.Range, "0,100")] int MaxBuilt;
		[Export] Special special;
		[Export] string[] AvailableJobClassNames = Array.Empty<string>();
		[Export(PropertyHint.Flags)] GroundTileType AllowedGroundTiles = GroundTileType.HasLand;
		[Export(PropertyHint.File, "*.tscn")] string ScenePath;


		public string GetScenePath() => ScenePath;
		public void SetScenePath(string scenepath) => ScenePath = scenepath;

		public Special GetSpecial() => special;

		public int GetPopulationCapacity() {
			Debug.Assert(PopulationCapacity >= 0, "Population capacity can't be negative");
			return PopulationCapacity;
		}

		ResourceConsumer[] constructionResources = null;
		public ResourceConsumer[] GetConstructionResources() {
			if (constructionResources is null) {
				constructionResources = new ResourceConsumer[ResourceCosts.Count];
				int i = 0;
				foreach (var (resource, amount) in ResourceCosts) {
					if (resource is ResourceOrType rortype) constructionResources[i++] = new ResourceConsumer(rortype.Flatten(), amount);
					else constructionResources[i++] = new ResourceConsumer(resource, amount);
				}
				GD.Print($"BuildingType::GetConstructionResources() : resources: of {name} ", string.Join(", ", constructionResources));
			}
			return constructionResources;
		}

		public float GetHoursToConstruct() {
			Debug.Assert(HoursToConstruct >= 0f);
			return HoursToConstruct;
		}

		public int GetBuiltLimit() {
			Debug.Assert(MaxBuilt >= 0);
			return MaxBuilt;
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

		public bool IsPlacementAllowed(GroundTileType on) {
			//System.Console.WriteLine($"{AllowedGroundTiles} & {on} = {AllowedGroundTiles & on}");
			return (AllowedGroundTiles & on) == AllowedGroundTiles;
		}
	}

}
