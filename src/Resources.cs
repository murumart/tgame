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
		Debug.Assert(amount >= 0, $"Resource amount cannot be negative (got {amount})");
		this.Type = type;
		this.Amount = amount;
	}

	public override readonly string ToString() => $"{Type.AssetName} x {Amount}";

}

public partial class ResourceStorage : IEnumerable<KeyValuePair<IResourceType, InStorage>> {

	readonly Dictionary<IResourceType, InStorage> storageAmounts = new();
	public int ItemAmount { get; private set; }
	//public int ItemCapacity { get; private set; }


	public int GetCount(IResourceType item) {
		if (!storageAmounts.TryGetValue(item, out var v)) return 0;
		Debug.Assert(v.Amount >= 0, $"Got negative item count ({v.Amount})");
		return v.Amount;
	}

	public bool HasEnough(ResourceBundle resource) {
		if (!storageAmounts.TryGetValue(resource.Type, out InStorage stored)) return false;
		return resource.Amount <= stored.Amount;
	}

	public bool HasEnoughAll(IEnumerable<ResourceBundle> resources) {
		foreach (var r in resources) {
			if (!HasEnough(r)) return false;
		}
		return true;
	}

	public bool CanAdd(int _amount) => true;

	public bool CanAdd(ResourceBundle resource) => CanAdd(resource.Amount);

	public void AddResource(ResourceBundle resource) {
		Debug.Assert(CanAdd(resource), "These resources don't fit here");
		storageAmounts.TryGetValue(resource.Type, out InStorage stored);
		storageAmounts[resource.Type] = stored.Add(resource);
		ItemAmount += resource.Amount;
	}

	public void AddResources(IEnumerable<ResourceBundle> resources) {
		foreach (var bundle in resources) {
			AddResource(bundle);
		}
	}

	public void SubtractResource(ResourceBundle resource) {
		Debug.Assert(storageAmounts.ContainsKey(resource.Type), "cant subtract resource type that's not in storage!!");
		storageAmounts.TryGetValue(resource.Type, out InStorage stored);
		storageAmounts[resource.Type] = stored.Sub(resource);
		ItemAmount -= resource.Amount;
		Debug.Assert(ItemAmount >= 0, "Item amount in storage went negative?? What the hell..");
		if (storageAmounts[resource.Type].Amount == 0) storageAmounts.Remove(resource.Type);
	}

	public void SubtractResources(IEnumerable<ResourceBundle> resources) {
		foreach (var bundle in resources) {
			SubtractResource(bundle);
		}
	}

	public void TransferResources(ResourceStorage target, ResourceBundle resources) {
		SubtractResource(resources);
		target.AddResource(resources);
	}

	public void TransferResources(ResourceStorage target, IEnumerable<ResourceBundle> resources) {
		foreach (var res in resources) {
			TransferResources(target, res);
		}
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
			Debug.Assert(count >= 0, "Storage count can't be negative");
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

		public readonly override string ToString() {
			return $"{Amount}";
		}
	}

	public struct ResourceCapacity {

		public IResourceType Type;
		public int Capacity;


		public ResourceCapacity(IResourceType type, int capacity) {
			Debug.Assert(capacity >= 0, "Resource Capacity cannot be negative");
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





