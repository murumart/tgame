using System;
using System.Collections.Generic;
using Godot;
using static ResourceStorage;


public partial class Building : MapObject, ITimePassing {

	readonly IBuildingType type; public IBuildingType Type { get => type; }

	Population population; public ref Population Population => ref population;
	float constructionProgress;
	public bool IsConstructed { get => constructionProgress >= type.GetHoursToConstruct(); }
	public ConstructBuildingJob ConstructionJob;


	protected Building(IBuildingType type, Vector2I position) : base(position) {
		this.type = type;
		this.population = new Population(type.GetPopulationCapacity());
	}

	public void ProgressBuild(float amount) {
		Debug.Assert(!IsConstructed, "Don't ProgressBuild building that's ready...");
		constructionProgress += amount;
	}

	public override void PassTime(float hours) { }

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

	}

}




