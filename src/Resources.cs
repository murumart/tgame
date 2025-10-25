using System;
using System.Collections.Generic;
using System.Linq;

public interface IResourceType {

	public string Name { get; }

}

public struct ResourceBundle {

	public IResourceType Type;
	public int Amount;


	public ResourceBundle(IResourceType type, int amount) {
		this.Type = type;
		this.Amount = amount;
	}

}

public partial class ResourceStorage {

	readonly Dictionary<IResourceType, Amount> storageAmounts = new();


	public void IncreaseCapacity(IResourceType resourceType, int amount) {
		if (!storageAmounts.ContainsKey(resourceType)) storageAmounts[resourceType] = new();
		var old = storageAmounts[resourceType];
		storageAmounts[resourceType] = new(old.Capacity + amount);
	}

	public void ReduceCapacity(IResourceType resourceType, int amount) {
		Debug.Assert(storageAmounts.ContainsKey(resourceType), "cant reduce resource type that's not in storage!!");
		var old = storageAmounts[resourceType];
		var amtLimit = Math.Max(old.Capacity - amount, 0);
		storageAmounts[resourceType] = new(amtLimit, Math.Min(old.Count, amtLimit));
	}

	public int GetCapacity(IResourceType resourceType) {
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
		public IResourceType Type;
		public int Capacity;


		public ResourceCapacity(IResourceType type, int capacity) {
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





