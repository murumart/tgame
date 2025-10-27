using System;
using System.Collections;
using System.Collections.Generic;
using static ResourceStorage;

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

public partial class ResourceStorage : IEnumerable<KeyValuePair<IResourceType, InStorage>> {

	readonly Dictionary<IResourceType, InStorage> storageAmounts = new();


	public void IncreaseCapacity(IResourceType resourceType, int amount) {
		if (!storageAmounts.ContainsKey(resourceType)) storageAmounts[resourceType] = new();
		var old = storageAmounts[resourceType];
		storageAmounts[resourceType] = new(old.Capacity + amount);
	}

	public void ReduceCapacity(IResourceType resourceType, int amount) {
		Debug.Assert(storageAmounts.ContainsKey(resourceType), "cant reduce resource type that's not in storage!!");
		var old = storageAmounts[resourceType];
		var amtLimit = Math.Max(old.Capacity - amount, 0);
		storageAmounts[resourceType] = new(amtLimit, Math.Min(old.Amount, amtLimit));
	}

	public int GetCapacity(IResourceType resourceType) {
		if (!storageAmounts.TryGetValue(resourceType, out InStorage stored)) return 0;
		return stored.Capacity;
	}

	public bool HasEnough(ResourceBundle resource) {
		if (!storageAmounts.TryGetValue(resource.Type, out InStorage stored)) return false;
		return resource.Amount <= stored.Amount;
	}

	public bool HasAll(ICollection<ResourceBundle> resources) {
		foreach (var r in resources) {
			if (!HasEnough(r)) return false;
		}
		return true;
	}

	public bool CanAdd(ResourceBundle resource) {
		storageAmounts.TryGetValue(resource.Type, out InStorage stored);
		return stored.Amount + resource.Amount <= stored.Capacity;
	}

	public void AddResource(ResourceBundle resource) {
		Debug.Assert(CanAdd(resource), "These resources dont fit here.................................");
		storageAmounts.TryGetValue(resource.Type, out InStorage stored);
		storageAmounts[resource.Type] = stored.Add(resource);
	}

	public void SubtractResource(ResourceBundle resource) {
		Debug.Assert(storageAmounts.ContainsKey(resource.Type), "cant subtract resource type that's not in storage!!");
		storageAmounts.TryGetValue(resource.Type, out InStorage stored);
		storageAmounts[resource.Type] = stored.Sub(resource);
	}

	// enumerating over the object

	public IEnumerator<KeyValuePair<IResourceType, InStorage>> GetEnumerator() {
		return storageAmounts.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

}

public partial class ResourceStorage {

	public struct InStorage {
		public int Amount = 0;
		public int Capacity = 0;


		public InStorage(int capacity, int count) {
			this.Capacity = capacity;
			this.Amount = count;
		}

		public InStorage(int capacity) {
			this.Capacity = capacity;
			this.Amount = 0;
		}

		public InStorage Add(ResourceBundle resource) {
			return new InStorage(Capacity, Amount + resource.Amount);
		}

		public InStorage Sub(ResourceBundle resource) {
			return new InStorage(Capacity, Amount - resource.Amount);
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





