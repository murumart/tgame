using System.Collections.Generic;
using System.Linq;
using Godot;
using static Building;

public static class Registry {

	static ResourceTypeRegistry resourceTypeRegistry;
	static BuildingTypeRegistry buildingTypeRegistry;

	public static ResourceTypeRegistry Resources { get => resourceTypeRegistry; }
	public static BuildingTypeRegistry Buildings { get => buildingTypeRegistry; }


	public static void Register(IResourceType[] resourceTypes, IBuildingType[] buildingTypes) {
		resourceTypeRegistry ??= new();
		buildingTypeRegistry ??= new();

		Resources.RegisterAssets(resourceTypes);
		Buildings.RegisterAssets(buildingTypes);
	}
}

public abstract class AssetTypeRegistry<T> where T : IAssetType {

	protected T[] assetTypes;
	protected HashSet<T> assetTypesSet;
	protected IDictionary<string, T> assetDictionary;

	protected bool initted = false;


	public virtual void RegisterAssets(T[] assets) {
		Debug.Assert(!initted, "What the hell are you doing!!! Cant init the registry multiple times.");
		assetTypes = new T[assets.Length];
		assetTypesSet = new();
		assetDictionary = new Dictionary<string, T>();
		initted = true;
	}

	public T GetAsset(int id) {
		InitCheck();
		Debug.Assert(id >= 0 && id < assetTypes.Length, $"Asset index out of bounds ({id} vs {0}..{assetTypes.Length - 1})");
		return assetTypes[id];
	}

	public T GetAsset(string id) {
		bool had = assetDictionary.TryGetValue(id, out T value);
		Debug.Assert(had, $"Asset '{id}' not found");
		return value;
	}

	public List<T> GetAssets() {
		InitCheck();
		return assetTypes.ToList();
	}

	public IEnumerable<KeyValuePair<string, T>> GetIdAssetPairs() {
		InitCheck();
		return assetDictionary.AsEnumerable<KeyValuePair<string, T>>();
	}

	private void InitCheck() => Debug.Assert(initted, "Please init the assets before trying to access them!");

}

public class ResourceTypeRegistry : AssetTypeRegistry<IResourceType> {

	public override void RegisterAssets(IResourceType[] resourceTypes) {
		base.RegisterAssets(resourceTypes);
		for (int i = 0; i < resourceTypes.Length; i++) {
			var asset = resourceTypes[i];
			assetTypes[i] = asset;
			assetTypesSet.Add(asset);
			string key = asset.GetIdString();
			Debug.Assert(!assetDictionary.ContainsKey(key), $"ID KEY `{key}` ALREADY EXISTS");
			assetDictionary[asset.GetIdString()] = asset;
		}
	}

}

public class BuildingTypeRegistry : AssetTypeRegistry<IBuildingType> {

	public override void RegisterAssets(IBuildingType[] buildingTypes) {
		base.RegisterAssets(buildingTypes);
		for (int i = 0; i < buildingTypes.Length; i++) {
			var asset = buildingTypes[i];
			assetTypes[i] = asset;
			assetTypesSet.Add(asset);
			string key = asset.GetIdString();
			Debug.Assert(!assetDictionary.ContainsKey(key), $"ID KEY `{key}` ALREADY EXISTS");
			assetDictionary[asset.GetIdString()] = asset;
		}
	}

}

public interface IAssetType {

	string Name { get; }
	string AssetTypeName { get; }

	string GetIdString() {
		return (AssetTypeName + ":" + Name).ToSnakeCase();
	}

}
