using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static Building;
using static ResourceSite;

public static class Registry {

	public static AssetTypeRegistry<IResourceType> Resources { get; private set; }
	public static AssetTypeRegistry<IBuildingType> Buildings { get; private set; }
	public static AssetTypeRegistry<IResourceSiteType> ResourceSites { get; private set; }


	public static void Register(IResourceType[] resourceTypes, IBuildingType[] buildingTypes, IResourceSiteType[] resourceSiteTypes) {
		Resources = new();
		Buildings = new();
		ResourceSites = new();

		Resources.RegisterAssets(resourceTypes);
		Buildings.RegisterAssets(buildingTypes);
		ResourceSites.RegisterAssets(resourceSiteTypes);
	}

}

public class AssetTypeRegistry<T> where T : IAssetType {

	protected T[] assetTypes;
	protected HashSet<T> assetTypesSet;
	protected IDictionary<string, T> assetDictionary;

	protected bool initted = false;


	public virtual void RegisterAssets(T[] assets) {
		Debug.Assert(!initted, "What the hell are you doing!!! Cant init the registry multiple times.");
		assetTypes = new T[assets.Length];
		assetTypesSet = new();
		assetDictionary = new Dictionary<string, T>();

		for (int i = 0; i < assets.Length; i++) {
			var asset = assets[i];
			assetTypes[i] = asset;
			assetTypesSet.Add(asset);
			string key = asset.GetIdString();
			Debug.Assert(!assetDictionary.ContainsKey(key), $"ID KEY `{key}` ALREADY EXISTS");
			assetDictionary[asset.GetIdString()] = asset;
		}

		initted = true;
	}

	public T GetAsset(int id) {
		InitCheck();
		Debug.Assert(id >= 0 && id < assetTypes.Length, $"Asset index out of bounds ({id} vs {0}..{assetTypes.Length - 1})");
		return assetTypes[id];
	}

	public T GetAsset(string id) {
		InitCheck();
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

public interface IAssetType {

	string AssetName { get; }
	string AssetTypeName { get; }

	string GetIdString() {
		Debug.Assert(AssetName != null, $"Asset name is empty! (object: {this}, assettypename: {AssetTypeName})");
		return (AssetName).ToSnakeCase();
	}

}

// Stores a value and a function for calculating that value.
// Don't have to recompute the value each time
public class Field<T> {

	// DEBUG
	static int dirtyAccesses = 0; static int touches = 0;

	bool dirty = true;
	T value;
	public T Value {
		get {
			if (dirty) {
				dirtyAccesses++;
				value = getval();
				dirty = false;
			}
			return value;
		}
	}
	readonly Func<T> getval;

	public Field(Func<T> get) { this.getval = get; }
	public void Touch() {
		dirty = true;
		touches++;
	}

}
