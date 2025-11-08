using System;
using System.Collections;
using System.Collections.Generic;
using static ResourceStorage;

public interface IResourceType : IAssetType {

	string IAssetType.AssetTypeName => "resource";

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
	public int ItemAmount { get; private set; }
	public int ItemCapacity { get; private set; }


	public void IncreaseCapacity(int amount) {
		ItemCapacity += amount;
	}

	public void ReduceCapacity(int amount) {
		ItemCapacity -= amount;
	}

	public bool HasEnough(ResourceBundle resource) {
		if (!storageAmounts.TryGetValue(resource.Type, out InStorage stored)) return false;
		return resource.Amount <= stored.Amount;
	}

	public bool HasEnoughAll(ICollection<ResourceBundle> resources) {
		foreach (var r in resources) {
			if (!HasEnough(r)) return false;
		}
		return true;
	}

	public bool CanAdd(int amount) => amount + ItemAmount <= ItemCapacity;

	public bool CanAdd(ResourceBundle resource) => CanAdd(resource.Amount);

	public void AddResource(ResourceBundle resource) {
		Debug.Assert(CanAdd(resource), "These resources dont fit here.................................");
		storageAmounts.TryGetValue(resource.Type, out InStorage stored);
		storageAmounts[resource.Type] = stored.Add(resource);
		ItemAmount += resource.Amount;
	}

	public void SubtractResource(ResourceBundle resource) {
		Debug.Assert(storageAmounts.ContainsKey(resource.Type), "cant subtract resource type that's not in storage!!");
		storageAmounts.TryGetValue(resource.Type, out InStorage stored);
		storageAmounts[resource.Type] = stored.Sub(resource);
		ItemAmount -= resource.Amount;
		Debug.Assert(ItemAmount > 0, "Item amount in storage went negative?? What the hell..");
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


		public InStorage(int count) {
			this.Amount = count;
		}

		public InStorage() {
			this.Amount = 0;
		}

		public readonly InStorage Add(ResourceBundle resource) {
			return new InStorage(Amount + resource.Amount);
		}

		public readonly InStorage Sub(ResourceBundle resource) {
			return new InStorage(Amount - resource.Amount);
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





