using System;
using System.Collections.Generic;
using Godot;
using static ResourceStorage;


public abstract partial class MapObject : ITimePassing {
	protected Vector2I position; public Vector2I Position { get => position; }

	public MapObject(Vector2I position) {
		this.position = position;
	}

	public virtual void PassTime(float hours) { }
}

public partial class Building : MapObject, ITimePassing {
	readonly IBuildingType type; public IBuildingType Type { get => type; }

	Population population; public ref Population Population => ref population;
	bool isConstructed = true; public bool IsConstructed { get => isConstructed; }

	protected Building(IBuildingType type, Vector2I position) : base(position) {
		this.type = type;
		this.population = new Population(type.GetPopulationCapacity());
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

		Building CreateBuildingObject(Vector2I position) {
			return new Building(this, position);
		}
	}
}




