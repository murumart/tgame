using System;
using Godot;

namespace scenes.map {

	[GlobalClass]
	public partial class ResourceType : Resource, IResourceType {

		[Export] string name;
		public string Name => name;

	}

}

