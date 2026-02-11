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


	public static void Register(
		IResourceType[] resourceTypes,
		IBuildingType[] buildingTypes,
		IResourceSiteType[] resourceSiteTypes,
		IResourceGroup foodValues
	) {
		Resources = new();
		Buildings = new();
		ResourceSites = new();

		Resources.RegisterAssets(resourceTypes);
		Buildings.RegisterAssets(buildingTypes);
		ResourceSites.RegisterAssets(resourceSiteTypes);

		ResourcesS.RegisterFoodValues(foodValues);
	}

	public static class ResourcesS {

		public static readonly IResourceType Logs = Resources.GetAsset("logs");
		public static readonly IResourceType Rocks = Resources.GetAsset("rock");
		public static readonly IResourceType Clay = Resources.GetAsset("clay");

		public static readonly IResourceType Fruit = Resources.GetAsset("fruit");
		public static readonly IResourceType Fish = Resources.GetAsset("fish");
		public static readonly IResourceType Bread = Resources.GetAsset("bread");

		public static readonly IResourceType[] ResourceTypes = [
			Logs, Rocks, Clay, Fruit, Fish, Bread,
		];

		public static IResourceGroup FoodValues { get; private set; }

		public static void RegisterFoodValues(IResourceGroup fv) {
			Debug.Assert(FoodValues == null, "Food values already set");
			FoodValues = fv;
		}

	}

	public static class BuildingsS {

		public static readonly IBuildingType LogCabin = Buildings.GetAsset("log_cabin");
		public static readonly IBuildingType Housing = Buildings.GetAsset("housing");
		public static readonly IBuildingType BrickHousing = Buildings.GetAsset("brick_housing");

		public static readonly IBuildingType Marketplace = Buildings.GetAsset("marketplace");

		public static readonly IBuildingType[] HousingBuildings = [LogCabin, Housing, BrickHousing];

	}

	public static class ResourceSitesS {

		public static readonly IResourceSiteType BroadleafWoods = ResourceSites.GetAsset("broadleaf_woods");
		public static readonly IResourceSiteType ConiferWoods = ResourceSites.GetAsset("conifer_woods");
		public static readonly IResourceSiteType SavannaTrees = ResourceSites.GetAsset("savanna_trees");
		public static readonly IResourceSiteType RainforestTrees = ResourceSites.GetAsset("rainforest_trees");
		public static readonly IResourceSiteType Rock = ResourceSites.GetAsset("rock");
		public static readonly IResourceSiteType Rubble = ResourceSites.GetAsset("rubble");
		public static readonly IResourceSiteType ClayPit = ResourceSites.GetAsset("clay_pit");

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
