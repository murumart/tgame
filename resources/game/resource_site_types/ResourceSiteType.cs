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

		public string Name => name;

		public string AssetTypeName => "resource_site";

		public List<Well> GetDefaultWells() {
			throw new NotImplementedException();
		}

		public string GetScenePath() => ScenePath;
	}

}

