using System;
using Godot;
using static ResourceType;

namespace scenes.map {

	[GlobalClass]
	public partial class ResourceType : Resource, IResourceTypeData {

		[Export] string name;
		public string Name => name;

	}

}

