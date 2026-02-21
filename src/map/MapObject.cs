using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.visual;


public abstract partial class MapObject {

	protected Vector2I position; public Vector2I GlobalPosition { get => position; }
	public abstract IMapObjectType Type { get; }

	public virtual IEnumerable<Job> GetAvailableJobs() => Type.GetPossibleJobs();

	protected MapObject(Vector2I globalPosition) {
		this.position = globalPosition;
	}

	public abstract void PassTime(TimeT minutes);

}

public partial class MapObject {

	public interface IMapObjectType : IAssetType {

		MapObject CreateMapObject(Vector2I globalPosition);
		IEnumerable<Job> GetPossibleJobs();
		bool IsPlacementAllowed(GroundTileType on);

	}

}

public partial class Building : MapObject {

	public event Action<Building> BuildingConstructed;

	readonly IBuildingType type;
	public override IBuildingType Type => type;

	public int Population;
	float constructionProgress; // in minutes
	public bool IsConstructed => IsConstructedProgress(constructionProgress);
	public bool IsConstructedProgress(float progress) => progress >= type.GetHoursToConstruct() * 60;

	public bool HasFurniture;


	protected Building(IBuildingType type, Vector2I globalPosition) : base(globalPosition) {
		this.type = type;
	}

	public void ProgressBuild(TimeT minutes, ConstructBuildingJob job) {
		Debug.Assert(!IsConstructed, "Don't ProgressBuild building that's ready...");
		constructionProgress += job.GetWorkTime(minutes);
		if (IsConstructed) {
			BuildingConstructed?.Invoke(this);
		}
	}

	public float GetBuildProgress() {
		return (constructionProgress / 60) / type.GetHoursToConstruct();
	}

	public uint GetHousingCapacity() => (uint)type.GetPopulationCapacity();

	public override void PassTime(TimeT minutes) { }

	public override IEnumerable<Job> GetAvailableJobs() {
		var jobs = base.GetAvailableJobs().ToList();
		if (!HasFurniture && GetHousingCapacity() > 0) jobs.Add(new AddFurnitureJob(this));
		return jobs;
	}

}

public partial class Building {

	public interface IBuildingType : IAssetType, IMapObjectType {

		public enum Special {
			None,
			Marketplace,
		}

		public Special GetSpecial();

		int GetPopulationCapacity();
		ResourceBundle[] GetResourceRequirements();
		float GetHoursToConstruct();
		string GetDescription();

		MapObject IMapObjectType.CreateMapObject(Vector2I position) {
			return new Building(this, position);
		}

		bool HasResourceRequirements() {
			var r = GetResourceRequirements();
			return r != null && r.Length > 0;
		}

		bool TakesTimeToConstruct() => GetHoursToConstruct() > 0;

		CraftJob[] GetCraftJobs();

	}

}

public partial class ResourceSite : MapObject {

	private readonly IResourceSiteType type;
	public override IResourceSiteType Type => type;

	readonly List<Well> mineWells;
	public List<Well> Wells => mineWells;

	public bool IsDepleted {
		get {
			foreach (var well in mineWells) if (well.HasBunches) return false;
			return true;
		}
	}


	protected ResourceSite(IResourceSiteType type, Vector2I position) : base(position) {
		mineWells = type.GetDefaultWells();
		this.type = type;
	}

	public override void PassTime(TimeT minutes) {
		foreach (var well in mineWells) {
			if (well.MinutesPerBunchRegen <= 0) continue;
			// TODO regenerate materials
		}
	}

	public override IEnumerable<Job> GetAvailableJobs() {
		var jobs = base.GetAvailableJobs().ToList();
		int wellix = 0;
		foreach (var well in Wells) {
			if (well.HasBunches) {
				jobs.Add(new GatherResourceJob(wellix, this));
			}
			wellix += 1;
		}
		return jobs;
	}

	// get a list of the resources. don't use these bundles in actual gameplay, only for display
	public void GetResourcesAvailableAtPristineNaturalStart(Dictionary<IResourceType, ResourceBundle> resourceDict) {
		foreach (var well in Wells) {
			var type = well.ResourceType;
			if (!resourceDict.ContainsKey(type)) resourceDict[type] = new(type, 0);
			resourceDict[type] = new(type, resourceDict[type].Amount + well.InitialBunches);
		}
	}

	public override string ToString() => $"ResourceSite(at {position})";

}

public partial class ResourceSite {

	public interface IResourceSiteType : IAssetType, IMapObjectType {

		List<Well> GetDefaultWells();

		MapObject IMapObjectType.CreateMapObject(Vector2I position) {
			return new ResourceSite(this, position);
		}

	}

	public class Well {

		public readonly IResourceType ResourceType;
		public readonly TimeT MinutesPerBunch;
		public readonly TimeT MinutesPerBunchRegen;
		public readonly int InitialBunches;

		public int Bunches { get; private set; }
		public bool HasBunches => Bunches > 0;

		public readonly Verb Production;


		public Well(
			IResourceType resourceType,
			int minutesPerBunch,
			int minutesPerBunchRegen,
			int initialBunches,
			Verb production
		) {
			Debug.Assert(resourceType != null, "Well resource type cannot be null");
			Debug.Assert(minutesPerBunch > 0);
			ResourceType = resourceType;
			MinutesPerBunch = minutesPerBunch;
			MinutesPerBunchRegen = minutesPerBunchRegen;
			InitialBunches = initialBunches;
			Debug.Assert(production != null, "Need valid Production to consturct Well object");
			Production = production;

			Bunches = initialBunches;
		}


		public void Deplete() {
			Debug.Assert(Bunches > 0, "Resource is empty, cannot deplete further");
			Bunches -= 1;
		}

		public void Regen() {
			Debug.Assert(Bunches < InitialBunches, "Resource capacity is full, cannot generate more resource");
		}

	}

}
