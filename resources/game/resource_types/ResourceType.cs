using System;
using Godot;

namespace resources.game.resource_types {

	[GlobalClass]
	public partial class ResourceType : Resource, IResourceType {



		[Export] string name;
		public string AssetName => name;

	}

}

