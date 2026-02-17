using Godot;
using resources.game.resource_types;
using resources.visual;

namespace resources.game.resource_site_types {

	[GlobalClass]
	public partial class ResourceWell : Resource {

		[Export] ResourceType resourceType;
		[Export] int minutesPerBunch;
		[Export] int minutesPerBunchRegen;
		[Export] int initialBunches;
		[Export] Verb Production;


		public ResourceSite.Well GetWell() {
			return new(resourceType, minutesPerBunch, minutesPerBunchRegen, initialBunches, IsInstanceValid(Production) ? Production : Verb.Make("gather", "gathering"));

		}

	}

}
