using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class Region : RefCounted {

	Dictionary<Vector3I, int> groundTiles = new();
	Dictionary<Vector3I, int> higherTiles = new();
	Dictionary<Vector3I, Building> buildings = new();


	
}
