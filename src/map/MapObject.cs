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

		IEnumerable<Job> GetAvailableJobs();

	}

}

public partial class Building : MapObject {

	readonly IBuildingType type; public override IBuildingType Type { get => type; }

	Population population; public ref Population Population => ref population;
	float constructionProgress; // in minutes
	public bool IsConstructed => constructionProgress >= type.GetHoursToConstruct() * 60;
	public ConstructBuildingJob ConstructionJob;


	protected Building(IBuildingType type, Vector2I position) : base(position) {
		this.type = type;
		this.population = new Population(type.GetPopulationCapacity());
	}

	public void ProgressBuild(TimeT minutes, IConstructBuildingJob job) {
		Debug.Assert(!IsConstructed, "Don't ProgressBuild building that's ready...");
		constructionProgress += (uint)minutes * job.GetProgressPerMinute();
	}

	public float GetBuildProgress() {
		return (constructionProgress / 60) / type.GetHoursToConstruct();
	}

	public override void PassTime(TimeT minutes) { }

}

public partial class Building {

	public interface IBuildingType : IAssetType, IMapObjectType {

		int GetPopulationCapacity();
		ResourceCapacity[] GetResourceCapacities();
		ResourceBundle[] GetResourceRequirements();
		float GetHoursToConstruct();

		Building CreateMapObject(Vector2I position) {
			return new Building(this, position);
		}

		bool HasResourceRequirements() {
			var r = GetResourceRequirements();
			return r != null && r.Length > 0;
		}

		bool TakesTimeToConstruct() => GetHoursToConstruct() > 0;

	}

}

public partial class ResourceSite : MapObject {

	private readonly IResourceSiteType type;
	public override IResourceSiteType Type => type;

	readonly List<Well> mineWells;
	public List<Well> MineWells => mineWells;


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

		ResourceSite CreateMapObject(Vector2I position) {
			return new ResourceSite(this, position);
		}

	}

	public class Well {

		public IResourceType ResourceType;
		public int MinutesPerBunch;
		public int BunchSize;
		public int MinutesPerBunchRegen;
		public int InitialBunches;

		int bunches;


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

			bunches = initialBunches;
		}

		public void Deplete() {
			Debug.Assert(bunches > 0, "Resource is empty, cannot deplete further");
			bunches -= 1;
		}

		public void Regen() {
			Debug.Assert(bunches < InitialBunches, "Resource capacity is full, cannot generate more resource");
		}

	}

}




