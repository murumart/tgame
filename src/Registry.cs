using System.Collections.Generic;
using System.Linq;
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

public abstract class AssetTypeRegistry<T> {

	protected T[] assetTypes;
	protected HashSet<T> assetTypesSet;

	protected bool initted = false;


	public virtual void RegisterAssets(T[] assets) {
		Debug.Assert(!initted, "What the hell are you doing!!! Cant init the registry multiple times.");
		assetTypes = new T[assets.Length];
		assetTypesSet = new();
		initted = true;
	}

	public T GetAsset(int id) {
		InitCheck();
		Debug.Assert(id >= 0 && id < assetTypes.Length, $"Asset index out of bounds ({id} vs {0}..{assetTypes.Length - 1})");
		return assetTypes[id];
	}

	public List<T> GetAssets() {
		InitCheck();
		return assetTypes.ToList();
	}

	private void InitCheck() => Debug.Assert(initted, "Please init the assets before trying to access them!");

}

public class ResourceTypeRegistry : AssetTypeRegistry<IResourceType> {

	public override void RegisterAssets(IResourceType[] resourceTypes) {
		base.RegisterAssets(resourceTypes);
		for (int i = 0; i < resourceTypes.Length; i++) {
			assetTypes[i] = resourceTypes[i];
			assetTypesSet.Add(resourceTypes[i]);
		}
	}

}

public class BuildingTypeRegistry : AssetTypeRegistry<IBuildingType> {

	public override void RegisterAssets(IBuildingType[] buildingTypes) {
		base.RegisterAssets(buildingTypes);
		for (int i = 0; i < buildingTypes.Length; i++) {
			assetTypes[i] = buildingTypes[i];
			assetTypesSet.Add(buildingTypes[i]);
		}
	}

}