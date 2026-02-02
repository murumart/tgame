using System;
using System.Collections;
using Godot;
using Godot.Collections;
using resources.game.resource_types;

namespace resources.game {

	[GlobalClass]
	public partial class ResourceGroup : Resource, IResourceGroup {

		[Export] Godot.Collections.Array<ResourceGroupValue> ResourceValues;
		System.Collections.Generic.OrderedDictionary<IResourceType, int> cached = null;

		public System.Collections.Generic.IDictionary<IResourceType, int> GroupValues {
			get {
				if (cached == null) {
					cached = new System.Collections.Generic.OrderedDictionary<IResourceType, int>();
					foreach (var kvp in ResourceValues) cached.Add(kvp.ResourceType, kvp.Value);
				}
				Debug.Assert(cached != null, "Cached dictionary was null?");

				return cached;
			}
		}

	}

}