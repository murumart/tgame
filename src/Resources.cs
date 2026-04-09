using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static ResourceStorage;

public interface IResourceType : IAssetType {

	string IAssetType.AssetTypeName => "resource";

}

public struct ResourceBundle {

	readonly public IResourceType Type;
	public int Amount;


	public ResourceBundle(IResourceType type, int amount) {
		Debug.Assert(type != null, "Resource bundle type cannot be null");
		Debug.Assert(amount >= 0, $"Resource amount cannot be negative (got {amount})");
		//if (amount == 0) GD.PushWarning($"Made bundle with 0 amount"); // this happens in Job::AddToStorage because 0-amounts are empty and skipped
		this.Type = type;
		this.Amount = amount;
	}

	public readonly ResourceBundle Multiply(int coef) => new(Type, Amount * coef);
	public readonly ResourceBundle Divide(int coef) => new(Type, Mathf.Max(1, Amount / coef));

	public override readonly string ToString() => $"{Type?.AssetName ?? "NULL"} x {Amount}";

}

public struct ResourceConsumer {

	readonly public IResourceType[] Types;
	public int Amount;


	public ResourceConsumer(IResourceType type, int amount) {
		Debug.Assert(type != null, "Resource bundle type cannot be null");
		Debug.Assert(amount >= 0, $"Resource amount cannot be negative (got {amount})");
		this.Types = [type];
		this.Amount = amount;
	}

	public ResourceConsumer(IResourceType[] types, int amount) {
		Debug.Assert(types != null, "Resource bundle types cannot be null");
		Debug.Assert(amount >= 0, $"Resource amount cannot be negative (got {amount})");
		Debug.Assert(types.Length != 0, "Need to have at least one type in ResourceConsumer");
		this.Types = types;
		this.Amount = amount;
	}

	public override readonly string ToString() => $"{string.Join(" or ", Types.Select(t => $"{t}"))} x {Amount}";

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

	public bool HasEnough(IResourceType type, int amount) {
		if (!storageAmounts.TryGetValue(type, out InStorage stored)) return false;
		return amount <= stored.Amount;
	}

	public bool HasEnough(ResourceConsumer resource) {
		foreach (var type in resource.Types) {
			if (!storageAmounts.TryGetValue(type, out InStorage stored)) continue;
			if (resource.Amount <= stored.Amount) return true;
		}
		return false;
	}

	public bool HasEnough(IEnumerable<ResourceConsumer> resources) {
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

	public ResourceBundle SubtractResource(IResourceType type, int amount) {
		Debug.Assert(storageAmounts.ContainsKey(type), "cant subtract resource type that's not in storage!!");
		Debug.Assert(amount > 0, "Need positive amount to subtract from resources");
		storageAmounts.TryGetValue(type, out InStorage stored);
		Debug.Assert(stored.Amount >= amount, $"Can't subtract more than have in storage ({stored.Amount} - {amount})");
		storageAmounts[type] = stored.Sub(new(type, amount));
		ItemAmount -= amount;
		Debug.Assert(ItemAmount >= 0, "Item amount in storage went negative?? What the hell..");
		if (storageAmounts[type].Amount == 0) storageAmounts.Remove(type);
		return new(type, amount);
	}

	public ResourceBundle SubtractResource(ResourceConsumer consumer) {
		foreach (var type in consumer.Types) {
			if (HasEnough(type, consumer.Amount)) {
				var bundle = new ResourceBundle(type, consumer.Amount);
				SubtractResource(bundle);
				return bundle;
			}
		}
		Debug.Assert(false, "Subtracting resource failed: dind't find it");
		throw new Exception("gh");
	}

	public ResourceBundle[] SubtractResources(IEnumerable<ResourceConsumer> resources) {
		var ret = new ResourceBundle[resources.Count()];
		int i = 0;
		foreach (var reduct in resources) {
			ret[i++] = SubtractResource(reduct);
		}
		return ret;
	}

	public void SubtractResource(ResourceBundle resource) {
		SubtractResource(resource.Type, resource.Amount);
	}

	public void SubtractResources(IEnumerable<ResourceBundle> resources) {
		foreach (var bundle in resources) {
			SubtractResource(bundle);
		}
	}

	public void TransferResource(ResourceStorage target, ResourceConsumer consumer) {
		var bundle = SubtractResource(consumer);
		target.AddResource(bundle);
	}

	public void TransferResource(ResourceStorage target, ResourceBundle resource) {
		SubtractResource(resource);
		target.AddResource(resource);
	}
	
	public void TransferResources(ResourceStorage target, IEnumerable<ResourceConsumer> resources) {
		foreach (var res in resources) {
			TransferResource(target, res);
		}
	}

	public void TransferResources(ResourceStorage target, IEnumerable<ResourceBundle> resources) {
		foreach (var res in resources) {
			TransferResource(target, res);
		}
	}

	public ResourceBundle GetTransfer(ResourceBundle gives) {
		SubtractResource(gives);
		return gives;
	}

	public void AbsorbFrom(ResourceStorage resources) {
		foreach (var (t, v) in resources.storageAmounts) {
			AddResource(new(t, v.Amount));
		}
		resources.storageAmounts.Clear();
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

		public static implicit operator int(InStorage t) => t.Amount;
		public static implicit operator InStorage(int t) => new(t);
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

public interface IAssetGroup<Res, Val> : IEnumerable<KeyValuePair<Res, Val>>, IEnumerable where Res : IAssetType {

	public IDictionary<Res, Val> GroupValues { get; }


	public bool TryGetValue(Res ass, out Val val) {
		return GroupValues.TryGetValue(ass, out val);
	}

	public Val GetValue(Res ass) {
		var valin = TryGetValue(ass, out Val val);
		Debug.Assert(valin, $"There's no value for the item {ass.AssetTypeName} in the group");
		return val;
	}

	IEnumerator<KeyValuePair<Res, Val>> IEnumerable<KeyValuePair<Res, Val>>.GetEnumerator() {
		Debug.Assert(GroupValues != null, "GroupValues shouldn't be null");
		return GroupValues.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		Debug.Assert(GroupValues != null, "GroupValues shouldn't be null");
		return GroupValues.GetEnumerator();
	}

	public Val this[Res ass] {
		get => GetValue(ass);
	}

}




