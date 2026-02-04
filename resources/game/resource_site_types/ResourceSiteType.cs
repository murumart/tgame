using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using static ResourceSite;

namespace resources.game.resource_site_types {

	[GlobalClass]
	public partial class ResourceSiteType : Resource, IResourceSiteType, IScenePathetic {

		[Export] string name;
		[Export] Array<ResourceWell> mineResources;
		[Export(PropertyHint.File, "*.tscn")] string ScenePath;

		public string AssetName => name;
		public string Name => name;

		public string AssetTypeName => "resource_site";


		public IEnumerable<Job> GetAvailableJobs() {
			List<MapObjectJob> jobs = new();
			for (int i = 0; i < mineResources.Count; i++) jobs.Add(new GatherResourceJob(i, GetJobDescription(i)));
			return jobs;
		}

		public List<Well> GetDefaultWells() {
			List<Well> list = new();
			foreach (var item in mineResources) list.Add(item.GetWell());
			return list;
		}

		public string GetJobDescription(int wellIx) => mineResources[wellIx].GetWell().ResourceType.AssetName;

		public string GetScenePath() => ScenePath;
	}

}

