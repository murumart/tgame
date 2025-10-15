using Godot;
using scenes.region.view.buildings;
using System;
using System.Collections.Generic;


public class Building : ITimePassing {
	BuildingType type; public BuildingType Type { get => type; }

	Vector2I position; public Vector2I Position { get => position; }
	int population; public int Population { get => population; }
	float constructionProgress; public float ConstructionProgress { get => constructionProgress; }

	public Building(BuildingType type, Vector2I position) {
		this.type = type;
		this.position = position;
	}

	public void PassTime(float secs) {
		if (constructionProgress < 1.0) {
			constructionProgress += secs * 0.1f; // take 10 seconds to construct a building
		}
	}
}

public static class BuildingRegistry {

	static Dictionary<BuildingType, Building> buildingsByType;

	public static Building GetBuildingFromType(BuildingType type) {
		return buildingsByType[type];
	}
}

