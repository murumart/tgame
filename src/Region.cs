using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;
using Jobs;
using scenes.region.view.buildings;
using IBuildingType = Building.IBuildingType;

public enum GroundTileType : short {
	VOID,
	GRASS,
}

public class Region : ITimePassing {

	readonly Dictionary<Vector2I, GroundTileType> groundTiles = new();
	public Dictionary<Vector2I, GroundTileType> GroundTiles { get => groundTiles; }
	readonly Dictionary<Vector2I, int> higherTiles = new();
	readonly Dictionary<Vector2I, Building> buildings = new();
	readonly List<Job> jobs = new();
	readonly Dictionary<Vector2I, Job> jobsByPosition = new();
	Population homelessPopulation; public ref Population HomelessPopulation { get => ref homelessPopulation; }

	public Region() {
		homelessPopulation = new Population(100);
		homelessPopulation.Pop = 10;
	}

	public Region(Dictionary<Vector2I, GroundTileType> groundTiles) : this() {
		this.groundTiles = groundTiles;
	}

	float remainderPeopleTransferTime = 0.0f;
	public void PassTime(float hours) {
		foreach (Job job in jobs) {
			job.PassTime(hours);
		}
		foreach (Building building in buildings.Values) {
			building.PassTime(hours);
		}
	}

	public static Region GetTestCircleRegion(int radius) { // debugging
		var tiles = new Dictionary<Vector2I, GroundTileType>();
		for (int i = -radius; i <= radius; i++) {
			for (int j = -radius; j <= radius; j++) {
				if (i * i + j * j <= radius * radius) {
					tiles[new Vector2I(i, j)] = GroundTileType.GRASS;
				}
			}
		}
		var reg = new Region(tiles);
		return reg;
	}

	public bool CanPlaceBuilding(IBuildingType type, Vector2I position) {
		return !buildings.ContainsKey(position);
	}

	public Building PlaceBuilding(IBuildingType type, Vector2I position) {
		var building = type.CreateBuildingObject(position);
		buildings[position] = building;
		if (type.GetPopulationCapacity() > 0) AddJob(position, new AbsorbFromHomelessPopulationJob(building, this));
		return building;
	}

	public int GetPopulationCount() {
		int count = homelessPopulation.Pop;
		foreach (var b in buildings.Values) {
			count += b.Population.Pop;
		}
		return count;
	}

	public void AddJob(Vector2I pos, Job job) {
		Debug.Assert(!jobsByPosition.ContainsKey(pos), $"Job at place {pos} exists ({job})");
		jobsByPosition[pos] = job;
		AddJob(job);
	}

	public void AddJob(Job job) {
		jobs.Add(job);
	}

}
