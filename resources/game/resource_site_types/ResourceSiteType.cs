using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using static ResourceSite;

namespace resources.game.resource_site_types {

	[GlobalClass]
	public partial class ResourceSiteType : Resource, IResourceSiteType, IScenePathetic {

		[Export] string name;
		[Export] public string resourceTypeDescription;
		[Export] Array<ResourceWell> mineResources;
		[Export(PropertyHint.File, "*.tscn")] string ScenePath;

		public string AssetName => name;
		public string Name => name;

		public string AssetTypeName => "resource_site";


		public IEnumerable<Job> GetAvailableJobs() {
			List<MapObjectJob> jobs = new();
			jobs.Add(new GatherResourceJob(resourceTypeDescription));
			return jobs;
		}

		public List<Well> GetDefaultWells() {
			List<Well> list = new();
			foreach (var item in mineResources) list.Add(item.GetWell());
			return list;
		}

		public string GetScenePath() => ScenePath;
	}

}

