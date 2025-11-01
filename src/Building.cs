using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static ResourceStorage;


public partial class Building : MapObject, ITimePassing {

	readonly IBuildingType type; public IBuildingType Type { get => type; }

	Population population; public ref Population Population => ref population;
	uint constructionProgress; // in minutes
	public bool IsConstructed { get => constructionProgress >= type.GetHoursToConstruct() * 60; }
	public ConstructBuildingJob ConstructionJob;


	protected Building(IBuildingType type, Vector2I position) : base(position) {
		this.type = type;
		this.population = new Population(type.GetPopulationCapacity());
	}

	public void ProgressBuild(TimeT minutes) {
		Debug.Assert(!IsConstructed, "Don't ProgressBuild building that's ready...");
		constructionProgress += (uint)minutes;
	}

	public override void PassTime(TimeT minutes) { }

}

public partial class Building {

	public interface IBuildingType {

		string GetName();
		string GetScenePath();
		int GetPopulationCapacity();
		ResourceCapacity[] GetResourceCapacities();
		ResourceBundle[] GetResourceRequirements();
		float GetHoursToConstruct();

		Building CreateBuildingObject(Vector2I position) {
			return new Building(this, position);
		}

		bool HasResourceRequirements() {
			var r = GetResourceRequirements();
			return r != null && r.Length > 0;
		}

		bool TakesTimeToConstruct() => GetHoursToConstruct() > 0;


	}

}




