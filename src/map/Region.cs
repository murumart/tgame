using System;
using System.Collections.Generic;
using Godot;
using static ResourceSite;


public class Region : ITimePassing {

	public event Action<Vector2I> MapObjectUpdatedAtEvent;

	public Vector2I WorldPosition { get; init; }
	readonly Dictionary<Vector2I, GroundTileType> groundTiles = new();
	public Dictionary<Vector2I, GroundTileType> GroundTiles { get => groundTiles; }
	readonly Dictionary<Vector2I, int> higherTiles = new();

	readonly Dictionary<Vector2I, MapObject> mapObjects = new();

	readonly HashSet<Region> neighbors = new();

	public Color Color { get; init; } // used for displaying


	public Region(Vector2I worldPosition, Dictionary<Vector2I, GroundTileType> groundTiles) {
		WorldPosition = worldPosition;
		this.groundTiles = groundTiles;
		this.Color = new Color(GD.Randf(), GD.Randf(), GD.Randf()).Lightened(0.25f);
	}

	public void PassTime(TimeT minutes) {
		foreach (MapObject ob in mapObjects.Values) {
			ob.PassTime(minutes);
		}
	}

	public static Region GetTestCircleRegion(int radius, Vector2I center) { // debugging
		var tiles = new Dictionary<Vector2I, GroundTileType>();
		var rs = new Dictionary<Vector2I, IResourceSiteType>();
		for (int i = -radius; i <= radius; i++) {
			for (int j = -radius; j <= radius; j++) {
				if (i * i + j * j <= radius * radius) {
					tiles[new Vector2I(i, j)] = GroundTileType.GRASS;

					if (i != 0 || j != 0) {
						if (GD.Randf() < 0.01f) rs.Add(new Vector2I(i, j), Registry.ResourceSites.GetAsset("boulder"));
						else if (GD.Randf() < 0.07f) rs.Add(new Vector2I(i, j), Registry.ResourceSites.GetAsset("trees"));
					}
				}
			}
		}
		var reg = new Region(center, tiles);
		foreach (var kvp in rs) {
			reg.CreateResourceSiteAndPlace(kvp.Value, kvp.Key);
		}
		return reg;
	}

	public bool AddNeighbor(Region neighbor) {
		return neighbors.Add(neighbor);
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

}
