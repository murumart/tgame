using System;
using System.Collections.Generic;
using System.Linq;

public partial class ResourceType {

	protected int id;
	protected string name;


	protected ResourceType(int id, string name) {
		this.id = id;
		this.name = name;
	}

}

public struct ResourceBundle {

	public ResourceType Type;
	public int Amount;


	public ResourceBundle(ResourceType type, int amount) {
		this.Type = type;
		this.Amount = amount;
	}

}

public partial class ResourceStorage {

	readonly Dictionary<ResourceType, Amount> storageAmounts = new();


	public void IncreaseCapacity(ResourceType resourceType, int amount) {
		if (!storageAmounts.ContainsKey(resourceType)) storageAmounts[resourceType] = new();
		var old = storageAmounts[resourceType];
		storageAmounts[resourceType] = new(old.Capacity + amount);
	}

	public void ReduceCapacity(ResourceType resourceType, int amount) {
		Debug.Assert(storageAmounts.ContainsKey(resourceType), "cant reduce resource type that's not in storage!!");
		var old = storageAmounts[resourceType];
		var amtLimit = Math.Max(old.Capacity - amount, 0);
		storageAmounts[resourceType] = new(amtLimit, Math.Min(old.Count, amtLimit));
	}

	public int GetCapacity(ResourceType resourceType) {
		if (!storageAmounts.TryGetValue(resourceType, out Amount value)) return 0;
		return value.Capacity;
	}

}

public partial class ResourceStorage {

	public struct Amount {
		public int Count;
		public int Capacity;


		public Amount(int capacity, int count) {
			this.Capacity = capacity;
			this.Count = count;
		}

		public Amount(int capacity) {
			this.Capacity = capacity;
			this.Count = 0;
		}

	}

	public struct ResourceCapacity {
		public ResourceType Type;
		public int Capacity;


		public ResourceCapacity(ResourceType type, int capacity) {
			this.Type = type;
			this.Capacity = capacity;
		}

		public static bool operator >(ResourceCapacity a, ResourceCapacity b) {
			return a.Type == b.Type && a.Capacity > b.Capacity;
		}

		public static bool operator <(ResourceCapacity a, ResourceCapacity b) {
			return a.Type == b.Type && a.Capacity < b.Capacity;
		}

	}

}

public partial class ResourceType {

	public interface IResourceTypeData {

		public string Name { get; }

		public ResourceType GetResourceType(int id) {
			return new ResourceType(id, Name);
		}
	}


	public static partial class ResourceRegistry {

		static ResourceType[] resourceTypes;
		static Dictionary<IResourceTypeData, ResourceType> resourceTypesByData;


		public static void RegisterResources(IResourceTypeData[] resourceTypes) {
			resourceTypesByData = new();
			ResourceRegistry.resourceTypes = new ResourceType[resourceTypes.Length];
			for (int i = 0; i < resourceTypes.Length; i++) {
				var rt = resourceTypes[i].GetResourceType(i);
				ResourceRegistry.resourceTypes[i] = rt;
				resourceTypesByData[resourceTypes[i]] = rt;
			}
		}

		public static ResourceType GetResourceType(int id) {
			return resourceTypes[id];
		}

		public static ResourceType GetResourceType(IResourceTypeData resourceTypeData) {
			return resourceTypesByData[resourceTypeData];
		}

	}

}
