using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using resources.game;

[GlobalClass]
public partial class ResourceSiteGenerationParametersCollection : Resource {

	[Export] Array<ResourceSiteGenerationParameters> parameters;
	public ResourceSiteGenerationParameters[] Parameters => parameters.ToArray();
    public int Count => parameters.Count;

    
    public ResourceSiteGenerationParameters this[int index] {
        get => parameters[index];
    }

}
