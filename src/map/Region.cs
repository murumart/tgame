using System;
using System.Collections.Generic;
using Godot;
using static ResourceSite;


public class Region {

	public event Action<Vector2I> MapObjectUpdatedAtEvent;

	readonly int worldIndex;

	public Vector2I WorldPosition { get; init; }
	public readonly Dictionary<Vector2I, GroundTileType> GroundTiles = new();
	public RegionFaction LocalFaction { get; private set; }

	readonly Dictionary<Vector2I, MapObject> mapObjects = new();

	readonly HashSet<Region> neighbors = new();

	public Color Color { get; init; } // used for displaying


	public Region(int index, Vector2I worldPosition, Dictionary<Vector2I, GroundTileType> groundTiles) {
		this.worldIndex = index;
		WorldPosition = worldPosition;
		GroundTiles = groundTiles;
		this.Color = Color.FromHsv(GD.Randf(), (float)GD.RandRange(0.75, 1.0), 1.0f);
	}

	public void PassTime(TimeT minutes) {
		foreach (MapObject ob in mapObjects.Values) {
			ob.PassTime(minutes);
		}
	}

	public bool AddNeighbor(Region neighbor) {
		return neighbors.Add(neighbor);
	}

	public void SetLocalFaction(RegionFaction regionFaction) {
		LocalFaction = regionFaction;
	}

	public bool CanPlaceBuilding(Vector2I position) {
		return !mapObjects.ContainsKey(position);
	}

	public IEnumerable<MapObject> GetMapObjects() {
		return mapObjects.Values;
	}

	public bool HasMapObject(Vector2I tile) {
		return mapObjects.ContainsKey(tile);
	}

	public bool HasMapObject(Vector2I tile, out MapObject mapObject) {
		return mapObjects.TryGetValue(tile, out mapObject);
	}

	public void RemoveMapObject(Vector2I tile) {
		Debug.Assert(HasMapObject(tile), $"There is no map object to remove at {tile}");
		mapObjects.Remove(tile);
		NotifyMapObjectUpdatedAt(tile);
	}

	MapObject CreateMapObjectSpotAndPlace(MapObject.IMapObjectType type, Vector2I position) {
		Debug.Assert(!HasMapObject(position, out var m), $"there's already a mapobject {m} at position {position}");
		var ob = type.CreateMapObject(position);
		mapObjects[position] = ob;
		return ob;
	}

	public Building CreateBuildingSpotAndPlace(Building.IBuildingType type, Vector2I position) {
		var building = (Building)CreateMapObjectSpotAndPlace(type, position);
		return building;
	}

	public ResourceSite CreateResourceSiteAndPlace(ResourceSite.IResourceSiteType type, Vector2I position) {
		var resourceSite = (ResourceSite)CreateMapObjectSpotAndPlace(type, position);
		return resourceSite;
	}

	public void NotifyMapObjectUpdatedAt(Vector2I at) => MapObjectUpdatedAtEvent?.Invoke(at);

	public override string ToString() {
		return $"Reg{WorldPosition}";
	}

	public static Region GetTestCircleRegion(int index, int radius, Vector2I center) { // debugging
		var tiles = new Dictionary<Vector2I, GroundTileType>();
		var rs = new Dictionary<Vector2I, IResourceSiteType>();
		for (int i = -radius; i <= radius; i++) {
			for (int j = -radius; j <= radius; j++) {
				if (i * i + j * j <= radius * radius) {
					tiles[new Vector2I(i, j)] = GroundTileType.Grass;

					if (i != 0 || j != 0) {
						if (GD.Randf() < 0.01f) rs.Add(new Vector2I(i, j), Registry.ResourceSites.GetAsset("boulder"));
						else if (GD.Randf() < 0.07f) rs.Add(new Vector2I(i, j), Registry.ResourceSites.GetAsset("trees"));
					}
				}
			}
		}
		var reg = new Region(index, center, tiles);
		foreach (var kvp in rs) {
			reg.CreateResourceSiteAndPlace(kvp.Value, kvp.Key);
		}
		return reg;
	}

}
