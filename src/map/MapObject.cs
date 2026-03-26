using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.visual;


public abstract partial class MapObject {

	protected Vector2I position; public Vector2I GlobalPosition { get => position; }
	public abstract IMapObjectType Type { get; }
	protected Region region;

	public bool Removed { get; private set; }

	public virtual IEnumerable<Job> GetAvailableJobs() => Type.GetPossibleJobs();

	protected MapObject(Vector2I globalPosition, Region region) {
		this.position = globalPosition;
		this.region = region;
	}

	public abstract void PassTime(TimeT minutes);

	public void Remove() => Removed = true;

}

public partial class MapObject {

	public interface IMapObjectType : IAssetType {

		MapObject CreateMapObject(Vector2I globalPosition, Region region);
		IEnumerable<Job> GetPossibleJobs();
		bool IsPlacementAllowed(GroundTileType on);

		MapObject CreateDummyMapObject() => this.CreateMapObject(Vector2I.Zero, null);
	
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


	protected Building(IBuildingType type, Vector2I globalPosition, Region region) : base(globalPosition, region) {
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
		if (type.GetSpecial() == IBuildingType.Special.Quarry) {
			GroundTileType ground = region.GetGroundTile(GlobalPosition - region.WorldPosition);
			Debug.Assert((ground & GroundTileType.HasLand) != 0);
			if ((ground & GroundTileType.HasSand) != 0) {}
		}
		jobs.Add(new DemolishMapObjectJob());
		return jobs;
	}

}

public partial class Building {

	public interface IBuildingType : IAssetType, IMapObjectType {

		public enum Special {
			None,
			Marketplace,
			Quarry,
		}

		public Special GetSpecial();

		int GetPopulationCapacity();
		ResourceBundle[] GetConstructionResources();
		float GetHoursToConstruct();
		string GetDescription();
		int GetBuiltLimit();

		MapObject IMapObjectType.CreateMapObject(Vector2I position, Region region) {
			return new Building(this, position, region);
		}

		bool HasResourceRequirements() {
			var r = GetConstructionResources();
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


	protected ResourceSite(IResourceSiteType type, Vector2I position, Region region) : base(position, region) {
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
		jobs.Add(new DemolishMapObjectJob());
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

		MapObject IMapObjectType.CreateMapObject(Vector2I position, Region region) {
			return new ResourceSite(this, position, region);
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
