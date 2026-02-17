using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static Building;
using static MapObject;
using static ResourceSite;

public static class Registry {

	public static AssetTypeRegistry<IResourceType> Resources { get; private set; }
	public static AssetTypeRegistry<IBuildingType> Buildings { get; private set; }
	public static AssetTypeRegistry<IResourceSiteType> ResourceSites { get; private set; }


	public static void Register(
		IResourceType[] resourceTypes,
		IBuildingType[] buildingTypes,
		IResourceSiteType[] resourceSiteTypes,
		IAssetGroup<IResourceType, int> foodValues
	) {
		Resources = new();
		Buildings = new();
		ResourceSites = new();

		Resources.RegisterAssets(resourceTypes);
		Buildings.RegisterAssets(buildingTypes);
		ResourceSites.RegisterAssets(resourceSiteTypes);

		ResourcesS.RegisterFoodValues(foodValues);

		ProductionNet.Generate();
	}

	public static class ResourcesS {

		public static readonly IResourceType Logs = Resources.GetAsset("logs");
		public static readonly IResourceType Lumber = Resources.GetAsset("lumber");
		public static readonly IResourceType Rocks = Resources.GetAsset("rock");
		public static readonly IResourceType Clay = Resources.GetAsset("clay");
		public static readonly IResourceType Bricks = Resources.GetAsset("bricks");

		public static readonly IResourceType Fruit = Resources.GetAsset("fruit");
		public static readonly IResourceType Fish = Resources.GetAsset("fish");
		public static readonly IResourceType Grain = Resources.GetAsset("grain");
		public static readonly IResourceType Flour = Resources.GetAsset("flour");
		public static readonly IResourceType Bread = Resources.GetAsset("bread");

		public static readonly IResourceType[] ResourceTypes = [
			Logs, Rocks, Clay, Bricks, Fruit, Fish, Grain, Flour, Bread,
		];

		public static IAssetGroup<IResourceType, int> FoodValues { get; private set; }

		public static void RegisterFoodValues(IAssetGroup<IResourceType, int> fv) {
			Debug.Assert(FoodValues == null, "Food values already set");
			FoodValues = fv;
		}

	}

	public static class BuildingsS {

		public static readonly IBuildingType LogCabin = Buildings.GetAsset("log_cabin");
		public static readonly IBuildingType Housing = Buildings.GetAsset("housing");
		public static readonly IBuildingType BrickHousing = Buildings.GetAsset("brick_housing");

		public static readonly IBuildingType Kiln = Buildings.GetAsset("kiln");
		public static readonly IBuildingType Marketplace = Buildings.GetAsset("marketplace");
		public static readonly IBuildingType GrainField = Buildings.GetAsset("grain_field");
		public static readonly IBuildingType Windmill = Buildings.GetAsset("windmill");
		public static readonly IBuildingType Bakery = Buildings.GetAsset("bakery");

		public static readonly IBuildingType[] HousingBuildings = [LogCabin, Housing, BrickHousing];
		public static readonly IBuildingType[] CraftingBuildings = [GrainField, Windmill, Bakery, Kiln];

	}

	public static class ResourceSitesS {

		public static readonly IResourceSiteType BroadleafWoods = ResourceSites.GetAsset("broadleaf_woods");
		public static readonly IResourceSiteType ConiferWoods = ResourceSites.GetAsset("conifer_woods");
		public static readonly IResourceSiteType SavannaTrees = ResourceSites.GetAsset("savanna_trees");
		public static readonly IResourceSiteType RainforestTrees = ResourceSites.GetAsset("rainforest_trees");
		public static readonly IResourceSiteType FishingSpot = ResourceSites.GetAsset("fishing_spot");
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

public static class ProductionNet {

	public class LocationNode(IMapObjectType type) {

		public readonly IMapObjectType Type = type;

		public override string ToString() => $"Location({Type.AssetName})";

	}

	public class ResourceSiteNode(IResourceSiteType resourceSiteType, ProductionNode[] productions) : LocationNode(resourceSiteType) {

		public readonly IResourceSiteType ResourceSite = resourceSiteType;
		public readonly ProductionNode[] Productions = productions;

		public override string ToString() => $"ResourceSite({ResourceSite.AssetName}, productions=[{string.Join(", ", (object[])Productions)}])";

	}

	public class BuildingNode(IBuildingType building, ProductionNode[] productions, (ResourceNode, int)[] sourceMaterials) : LocationNode(building) {

		public readonly IBuildingType Building = building;
		public readonly ProductionNode[] Productions = productions;
		public readonly (ResourceNode, int)[] SourceMaterials = sourceMaterials;

		public override string ToString() => $"Building(building={Building.AssetName}, productions=[{string.Join(", ", (object[])Productions)}], sourceMaterials=[{string.Join(", ", SourceMaterials)}]";

	}

	public class ProductionNode((ResourceNode, int)[] consumed, (ResourceNode, int)[] retrieved) {

		public readonly (ResourceNode, int)[] Consumed = consumed;
		public readonly (ResourceNode, int)[] Retrieved = retrieved;
		public LocationNode MadeAt = null;

		public override string ToString() => $"PR(in=[{string.Join(", ", Consumed)}], out=[{string.Join(", ", Retrieved)}])";

	}

	public class ResourceNode(IResourceType resource) {

		public readonly IResourceType Resource = resource;
		public Dictionary<LocationNode, HashSet<ProductionNode>> RetrievedFrom = new();
		public Dictionary<LocationNode, HashSet<ProductionNode>> ConsumedIn = new();
		public HashSet<BuildingNode> ConsumedConstructing = new();

		public override string ToString() => $"RN({Resource.AssetName})";

	}

	static bool generated = false;

	public static readonly Dictionary<IResourceType, ResourceNode> Resources = new();
	public static readonly Dictionary<IMapObjectType, LocationNode> Locations = new();
	public static readonly Dictionary<ResourceNode, ProductionNode> ProductionsByConsumption = new();
	public static readonly Dictionary<ResourceNode, ProductionNode> ProductionsByRetrieval = new();

	public static void Generate() {
		Debug.Assert(!generated, "Doon't generate stuff when iot's donadalsd");
		foreach (var res in Registry.Resources.GetAssets()) Resources[res] = new(res);

		foreach (var building in Registry.Buildings.GetAssets()) {
			if (building.GetCraftJobs().Length == 0) continue;
			var productions = new List<ProductionNode>();
			foreach (var job in building.GetCraftJobs()) {
				var consumed = job.Inputs?.Select(i => (Resources[i.Type], i.Amount)).ToArray() ?? [];
				var retrieved = job.Outputs?.Select(i => (Resources[i.Type], i.Amount)).ToArray() ?? [];
				var production = new ProductionNode(consumed, retrieved);
				productions.Add(production);
			}
			var buildingmaterials = building.GetResourceRequirements().Select(r => (Resources[r.Type], r.Amount));
			var node = new BuildingNode(
				building,
				productions.ToArray(),
				buildingmaterials.ToArray()
			);
			foreach (var (t, _) in buildingmaterials) {
				t.ConsumedConstructing.Add(node);
			}
			foreach (var prod in productions) {
				prod.MadeAt = node;
				foreach (var (ret, _) in prod.Retrieved) {
					if (!ret.RetrievedFrom.TryGetValue(node, out var rf)) {
						rf = new();
						ret.RetrievedFrom[node] = rf;
					}
					rf.Add(prod);
				}
				foreach (var (con, _) in prod.Consumed) {
					if (!con.ConsumedIn.TryGetValue(node, out var ci)) {
						ci = new();
						con.ConsumedIn[node] = ci;
					}
					ci.Add(prod);
				}
			}
			Locations[building] = node;
		}
		foreach (var ressite in Registry.ResourceSites.GetAssets()) {
			List<ProductionNode> productions = ressite.GetDefaultWells().Select(w => new ProductionNode([], [(Resources[w.ResourceType], 1)])).ToList();
			var node = new ResourceSiteNode(
				ressite,
				productions.ToArray()
			);
			foreach (var prod in productions) {
				prod.MadeAt = node;
				foreach (var (ret, _) in prod.Retrieved) {
					if (!ret.RetrievedFrom.TryGetValue(node, out var rf)) {
						rf = new();
						ret.RetrievedFrom[node] = rf;
					}
					rf.Add(prod);
				}
			}
			Locations[ressite] = node;
		}
		generated = true;
	}

	public static void PrintLocations() {
		foreach (var res in Locations.Values) {
			GD.Print(res);
		}
	}

	public static void PrintSources() {
		void PrintProduction(ProductionNode p, int indent) {
			if (p.Consumed.Length == 0) return;
			GD.Print(new string(' ', indent) + " Production sources ");
			foreach (var (src, _) in p.Consumed) PrintSource(src, indent + 2);
		}

		HashSet<ResourceNode> printed = new();
		void PrintSource(ResourceNode node, int indent) {
			if (printed.Contains(node) && indent == 0) return;
			GD.Print(new string(' ', indent) + node);
			if (printed.Contains(node)) return;
			printed.Add(node);
			foreach (var (k, v) in node.RetrievedFrom) {
				GD.Print(new string(' ', indent + 2) + " Retrieved from " + k.Type.AssetName);
				foreach (var vl in v) PrintProduction(vl, indent + 2);
			}
		}

		foreach (var rn in Resources.Values) PrintSource(rn, 0);
	}

	public static void PrintConsumers() {
		void PrintProduction(ProductionNode p, int indent) {
			if (p.Retrieved.Length == 0) return;
			GD.Print(new string(' ', indent) + " Production consumers ");
			foreach (var (src, _) in p.Retrieved) PrintConsumers(src, indent + 2);
		}

		HashSet<ResourceNode> printed = new();
		void PrintConsumers(ResourceNode node, int indent) {
			if (printed.Contains(node) && indent == 0) return;
			GD.Print(new string(' ', indent) + node);
			if (printed.Contains(node)) return;
			printed.Add(node);
			foreach (var (k, v) in node.ConsumedIn) {
				GD.Print(new string(' ', indent + 2) + " consumed in " + k.Type.AssetName);
				foreach (var vl in v) PrintProduction(vl, indent + 2);
			}
			foreach (var vl in node.ConsumedConstructing) {
				GD.Print(new string(' ', indent + 2) + "constructing " + vl.Type.AssetName);
			}
		}

		foreach (var rn in Resources.Values) PrintConsumers(rn, 0);
	}

}
