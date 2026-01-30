using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static ResourceStorage;


public abstract partial class MapObject {

	protected Vector2I position; public Vector2I GlobalPosition { get => position; }
	public abstract IMapObjectType Type { get; }


	protected MapObject(Vector2I globalPosition) {
		this.position = globalPosition;
	}

	public virtual void OnAdded(Region ctxRegion) { }
	public virtual void OnRemoved(Region ctxRegion) { }

	public abstract void PassTime(TimeT minutes);

}

public partial class MapObject {

	public interface IMapObjectType {

		MapObject CreateMapObject(Vector2I globalPosition);
		IEnumerable<Job> GetAvailableJobs();

	}

}

public partial class Building : MapObject {

	readonly IBuildingType type; public override IBuildingType Type { get => type; }

	public int Population;
	float constructionProgress; // in minutes
	public bool IsConstructed => constructionProgress >= type.GetHoursToConstruct() * 60;
	public ConstructBuildingJob ConstructionJob;

	Region region;


	protected Building(IBuildingType type, Vector2I globalPosition) : base(globalPosition) {
		this.type = type;
	}

	public override void OnAdded(Region ctxRegion) {
		region = ctxRegion;
	}

	public override void OnRemoved(Region ctxRegion) {
		if (IsConstructed) {
			region.LocalFaction.Population.Unhouse(-GetHousingCapacity());
			region.LocalFaction.Population.ChangeHousingCapacity(-GetHousingCapacity());
		}
	}

	public void ProgressBuild(TimeT minutes, ConstructBuildingJob job) {
		Debug.Assert(!IsConstructed, "Don't ProgressBuild building that's ready...");
		constructionProgress += job.GetWorkTime(minutes);
		if (IsConstructed) {
			region.LocalFaction.Population.ChangeHousingCapacity(GetHousingCapacity());
			region.LocalFaction.Population.House(GetHousingCapacity());
		}
	}

	public float GetBuildProgress() {
		return (constructionProgress / 60) / type.GetHoursToConstruct();
	}

	public int GetHousingCapacity() => type.GetPopulationCapacity();

	public override void PassTime(TimeT minutes) { }

}

public partial class Building {

	public interface IBuildingType : IAssetType, IMapObjectType {

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
	public List<Well> MineWells => mineWells;

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

	// get a list of the resources. don't use these bundles in actual gameplay, only for display
	public void GetResourcesAvailableAtPristineNaturalStart(Dictionary<IResourceType, ResourceBundle> resourceDict) {
		foreach (var well in MineWells) {
			var type = well.ResourceType;
			if (!resourceDict.ContainsKey(type)) resourceDict[type] = new(type, 0);
			resourceDict[type] = new(type, resourceDict[type].Amount + well.InitialBunches * well.BunchSize);
		}
	}

}

public partial class ResourceSite {

	public interface IResourceSiteType : IAssetType, IMapObjectType {

		List<Well> GetDefaultWells();

		MapObject IMapObjectType.CreateMapObject(Vector2I position) {
			return new ResourceSite(this, position);
		}

	}

	public class Well {

		public IResourceType ResourceType;
		public TimeT MinutesPerBunch;
		public int BunchSize;
		public TimeT MinutesPerBunchRegen;
		public int InitialBunches;

		public int Bunches { get; private set; }
		public bool HasBunches => Bunches > 0;


		public Well(
			IResourceType resourceType,
			int minutesPerBunch,
			int bunchSize,
			int minutesPerBunchRegen,
			int initialBunches
		) {
			ResourceType = resourceType;
			MinutesPerBunch = minutesPerBunch;
			BunchSize = bunchSize;
			MinutesPerBunchRegen = minutesPerBunchRegen;
			InitialBunches = initialBunches;

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




