using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static ResourceStorage;


public abstract partial class MapObject {

	protected Vector2I position; public Vector2I Position { get => position; }
	public abstract IMapObjectType Type { get; }


	protected MapObject(Vector2I position) {
		this.position = position;
	}

	public abstract void PassTime(TimeT minutes);

}

public partial class MapObject {

	public interface IMapObjectType {

		MapObject CreateMapObject(Vector2I position);
		IEnumerable<Job> GetAvailableJobs();

	}

}

public partial class Building : MapObject {

	readonly IBuildingType type; public override IBuildingType Type { get => type; }

	public Group Population;
	float constructionProgress; // in minutes
	public bool IsConstructed => constructionProgress >= type.GetHoursToConstruct() * 60;
	public ConstructBuildingJob ConstructionJob;


	protected Building(IBuildingType type, Vector2I position) : base(position) {
		this.type = type;
		this.Population = new(type.GetPopulationCapacity());
	}

	public void ProgressBuild(TimeT minutes, ConstructBuildingJob job) {
		Debug.Assert(!IsConstructed, "Don't ProgressBuild building that's ready...");
		constructionProgress += job.GetWorkTime(minutes);
	}

	public float GetBuildProgress() {
		return (constructionProgress / 60) / type.GetHoursToConstruct();
	}

	public override void PassTime(TimeT minutes) { }

}

public partial class Building {

	public interface IBuildingType : IAssetType, IMapObjectType {

		int GetPopulationCapacity();
		//ResourceCapacity[] GetResourceCapacities();
		ResourceBundle[] GetResourceRequirements();
		float GetHoursToConstruct();

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




