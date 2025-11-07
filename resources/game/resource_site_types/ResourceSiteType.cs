using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using static ResourceSite;

namespace resources.game.resource_site_types {

	[GlobalClass]
	public partial class ResourceSiteType : Resource, IResourceSiteType {

		[Export] Array<ResourceWell> mineResources;
		[Export(PropertyHint.File, "*.tscn")] string ScenePath;

		public string Name => throw new NotImplementedException();

		public string AssetTypeName => throw new NotImplementedException();

		public List<Well> GetDefaultWells() {
			throw new NotImplementedException();
		}
	}

}

