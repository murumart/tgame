using Godot;
using scenes.region.view.buildings;
using System;
using System.Collections.Generic;


public class Building {
	BuildingType type; public BuildingType Type { get => type; }

	int population; public int Population { get => population; }
	float constructionProgress; public float ConstructionProgress { get => constructionProgress; }

	public Building(BuildingType type) {
		this.type = type;
	}
}

public static class BuildingRegistry {

	static Dictionary<BuildingType, Building> buildingsByType;

	public static Building GetBuildingFromType(BuildingType type) {
		return buildingsByType[type];
	}
}

