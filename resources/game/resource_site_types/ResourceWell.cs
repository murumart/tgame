using Godot;
using resources.game.resource_types;

namespace resources.game.resource_site_types {

	[GlobalClass]
	public partial class ResourceWell : Resource {

		[Export] ResourceType resourceType;
		[Export] int minutesPerBunch;
		[Export] int bunchSize;
		[Export] int minutesPerBunchRegen;
		[Export] int initialBunches;

	}

}
