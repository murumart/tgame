using Godot;
using System;
using System.Collections.Generic;


public partial class Building : ITimePassing {
	readonly IBuildingType type; public IBuildingType Type { get => type; }

	Vector2I position; public Vector2I Position { get => position; }
	Population population; public ref Population Population => ref population;
	bool isConstructed = true; public bool IsConstructed { get => isConstructed; }

	protected Building(IBuildingType type, Vector2I position) {
		this.type = type;
		this.position = position;
		this.population = new Population(type.GetPopulationCapacity());
	}

	public void PassTime(float hours) {
		// TODO move to the job..

	}
}

public partial class Building {

	public interface IBuildingType {
		string GetName();
		string GetScenePath();
		int GetPopulationCapacity();
		Building CreateBuildingObject(Vector2I position) {
			return new Building(this, position);
		}
	}
}




