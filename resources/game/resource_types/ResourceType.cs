using System;
using Godot;

namespace resources.game.resource_types;

[GlobalClass]
public partial class ResourceType : Resource, IResourceType {

	[Export] string name;
	[Export] public Texture2D Icon;
	string assetIDString;
	public string AssetName => name;

	public string AssetIDString {
		get {
			assetIDString ??= name.ToSnakeCase();
			return assetIDString;
		}
	}

	public override string ToString() => $"[img]{Icon.ResourcePath}[/img] {name}";

}



