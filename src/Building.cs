using Godot;
using scenes.region.view.buildings;
using System;
using System.Collections.Generic;


public class Building : ITimePassing {
	BuildingType type; public BuildingType Type { get => type; }

	Vector2I position; public Vector2I Position { get => position; }
	Population population; public ref Population Population => ref population;
	bool isConstructed = true; public bool IsConstructed { get => isConstructed; }

	public Building(BuildingType type, Vector2I position) {
		this.type = type;
		this.position = position;
		this.population = new Population(type.PopulationCapacity);
	}

	public void PassTime(float hours) {
		// TODO move to the job..

	}
}

public static class BuildingRegistry {

	static Dictionary<BuildingType, Building> buildingsByType;

	public static Building GetBuildingFromType(BuildingType type) {
		return buildingsByType[type];
	}
}

