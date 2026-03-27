using System.Collections.Generic;
using Godot;

namespace resources.game.resource_types;

[GlobalClass]
public partial class ResourceOrType : ResourceType {

	[Export] ResourceType This;
	[Export] ResourceType Or;


	public ResourceType[] Flatten() {
		var list = new List<ResourceType>();
		FlattenInternal(list);
		return list.ToArray();
	}

	private void FlattenInternal(List<ResourceType> types) {
		if (This is not ResourceOrType) types.Add(This);
		else (This as ResourceOrType).FlattenInternal(types);
		if (Or is not ResourceOrType) types.Add(Or);
		else (Or as ResourceOrType).FlattenInternal(types);
	}

}



