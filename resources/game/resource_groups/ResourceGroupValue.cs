using Godot;
using resources.game.resource_types;

namespace resources.game {

	[GlobalClass]
	public partial class ResourceGroupValue : Resource {

		[Export] public ResourceType ResourceType;
		[Export] public int Value;

	}

}