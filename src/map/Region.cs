using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;
using static ResourceSite;


public class Region {

	public event Action<Vector2I> MapObjectUpdatedAtEvent;
	public void NotifyMapObjectUpdateAt(Vector2I p) => MapObjectUpdatedAtEvent?.Invoke(p);

	public event Action<Vector2I> TileChangedAtEvent;
	public void NotifyTileChangedAt(Vector2I p) => TileChangedAtEvent?.Invoke(p);

	readonly int worldIndex;

	public Vector2I WorldPosition { get; init; }
	public readonly Dictionary<Vector2I, GroundTileType> GroundTiles = new();
	public Faction LocalFaction { get; private set; }

	readonly Dictionary<Vector2I, MapObject> mapObjects = new();

	readonly HashSet<Region> neighbors = new(); public ICollection<Region> Neighbors => neighbors;

	public int LandTileCount => GroundTiles.Values.Where(t => (t & GroundTileType.HasLand) != 0).Count();
	public int OceanTileCount => GroundTiles.Values.Where(t => (t & GroundTileType.HasLand) == 0).Count();

	public readonly Field<ResourceBundle[]> NaturalResources;


	public Region(int index, Vector2I worldPosition, Dictionary<Vector2I, GroundTileType> groundTiles) {
		this.worldIndex = index;
		WorldPosition = worldPosition;
		GroundTiles = groundTiles;

		NaturalResources = new(() => {
			var dict = new Dictionary<IResourceType, ResourceBundle>();

			foreach (var mo in this.mapObjects.Values) {
				if (mo is ResourceSite rs) {
					rs.GetResourcesAvailableAtPristineNaturalStart(dict);
				}
			}

			return dict.Values.ToArray();
		});
	}

	public void PassTime(TimeT minutes) {
		foreach (MapObject ob in mapObjects.Values) {
			ob.PassTime(minutes);
		}
		LocalFaction.PassTime(minutes);
	}

	public bool AddNeighbor(Region neighbor) {
		Debug.Assert(neighbor != null, "Don't add useless null neighbors");
		Debug.Assert(neighbor != this, "I am not my own neighbro");
		return neighbors.Add(neighbor);
	}

	public void SetLocalFaction(Faction regionFaction) {
		LocalFaction = regionFaction;
	}

	public bool HasBuildingSpace(Vector2I position) {
		return !mapObjects.ContainsKey(position) && GroundTiles.ContainsKey(position);
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

	public MapObject GetMapObject(Vector2I tile) {
		Debug.Assert(HasMapObject(tile), $"Region has no map object at {tile}");
		return mapObjects[tile];
	}

	public void RemoveMapObject(Vector2I tile) {
		Debug.Assert(HasMapObject(tile), $"There is no map object to remove at {tile}");
		mapObjects.Remove(tile);
		NotifyMapObjectUpdateAt(tile);
		NaturalResources.Touch();
	}

	MapObject CreateMapObjectSpotAndPlace(MapObject.IMapObjectType type, Vector2I position) {
		Debug.Assert(!HasMapObject(position, out var m), $"there's already a mapobject {m} at position {position}");
		Debug.Assert(type.IsPlacementAllowed(GroundTiles[position]), $"Can't place map object {type.AssetName} here ({position} on {GroundTiles[position]})");
		var ob = type.CreateMapObject(WorldPosition + position);
		mapObjects[position] = ob;
		NaturalResources.Touch();
		NotifyMapObjectUpdateAt(position);
		return ob;
	}

	void AddMapObjectToSpot(MapObject mapObject, Vector2I position) {
		Debug.Assert(!HasMapObject(position, out var m), $"there's already a mapobject {m} at position {position}");
		mapObjects[position] = mapObject;
		NaturalResources.Touch();
		NotifyMapObjectUpdateAt(position);
	}

	public Building CreateBuildingSpotAndPlace(Building.IBuildingType type, Vector2I position) {
		var building = (Building)CreateMapObjectSpotAndPlace(type, position);
		return building;
	}

	public ResourceSite CreateResourceSiteAndPlace(ResourceSite.IResourceSiteType type, Vector2I position) {
		var resourceSite = (ResourceSite)CreateMapObjectSpotAndPlace(type, position);
		return resourceSite;
	}

	public void AnnexTile(Region from, Vector2I fromCoordinate) {
		Debug.Assert(from.GroundTiles.ContainsKey(fromCoordinate), $"Region to annex from doesn't own tile at {fromCoordinate}");
		var localCoord = fromCoordinate + from.WorldPosition - WorldPosition;
		Debug.Assert(!GroundTiles.ContainsKey(localCoord), $"Annexing region somehow already owns tile at {localCoord}");
		var tile = from.GroundTiles[fromCoordinate];
		from.GroundTiles.Remove(fromCoordinate);
		GroundTiles[localCoord] = tile;
		from.NotifyTileChangedAt(fromCoordinate);
		NotifyTileChangedAt(localCoord);
		if (from.HasMapObject(fromCoordinate)) {
			Debug.Assert(!HasMapObject(localCoord), "Somehow, I already have a map object where im trying to steal it from??");
			var mop = from.GetMapObject(fromCoordinate);
			from.RemoveMapObject(fromCoordinate);
			AddMapObjectToSpot(mop, localCoord);
		}
	}

	public float GetPotentialFoodFirstMonth() {
		float f = 0;
		float minutesInMonth = GameTime.Months(1);
		foreach (var mapObject in mapObjects.Values) if (mapObject is ResourceSite rs) foreach (var w in rs.Wells) {
			bool isFood = Registry.ResourcesS.FoodValues.TryGetValue(w.ResourceType, out int value);
			if (!isFood) continue;
			float bunchesInMonth = minutesInMonth / w.MinutesPerBunch;
			f += value * Mathf.Min(bunchesInMonth, w.Bunches);
		}
		return f;
	}

	public override string ToString() {
		return $"Reg{WorldPosition}";
	}

	public static Region GetTestCircleRegion(int index, int radius, Vector2I center) { // debugging
		var tiles = new Dictionary<Vector2I, GroundTileType>();
		var rs = new Dictionary<Vector2I, IResourceSiteType>();
		for (int i = -radius; i <= radius; i++) {
			for (int j = -radius; j <= radius; j++) {
				if (i * i + j * j <= radius * radius) {
					tiles[new Vector2I(i, j)] = GroundTileType.HasLand | GroundTileType.HasVeg;

					if (i != 0 || j != 0) {
						if (GD.Randf() < 0.01f) rs.Add(new Vector2I(i, j), Registry.ResourceSitesS.Rock);
						else if (GD.Randf() < 0.07f) rs.Add(new Vector2I(i, j), Registry.ResourceSitesS.BroadleafWoods);
						else if (GD.Randf() < 0.01f) rs.Add(new Vector2I(i, j), Registry.ResourceSitesS.ClayPit);
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
