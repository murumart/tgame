using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using scenes.autoload;
using static ResourceSite;


public class Region {

	public event Action<Vector2I> MapObjectUpdatedAtEvent;
	public void NotifyMapObjectUpdateAt(Vector2I p) => MapObjectUpdatedAtEvent?.Invoke(p);

	public event Action<Vector2I> TileChangedAtEvent;
	public void NotifyTileChangedAt(Vector2I p) => TileChangedAtEvent?.Invoke(p);

	public readonly int WorldIndex;

	public Vector2I WorldPosition { get; init; }
	readonly Dictionary<Vector2I, GroundTileType> groundTiles = new();
	public IEnumerable<Vector2I> GroundTilePositions => groundTiles.Keys;
	public Faction LocalFaction { get; private set; }

	readonly Dictionary<Vector2I, MapObject> mapObjects = new();

	readonly HashSet<Region> neighbors = new(); public ICollection<Region> Neighbors => neighbors;

	public int LandTileCount => groundTiles.Values.Where(t => (t & GroundTileType.HasLand) != 0).Count();
	public int OceanTileCount => groundTiles.Values.Where(t => (t & GroundTileType.HasLand) == 0).Count();

	public readonly Field<ResourceBundle[]> NaturalResources;


	public Region(int index, Vector2I worldPosition, Dictionary<Vector2I, GroundTileType> groundTiles) {
		this.WorldIndex = index;
		WorldPosition = worldPosition;
		this.groundTiles = groundTiles;

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

	public GroundTileType GetGroundTile(Vector2I localpos) {
		var got = groundTiles.TryGetValue(localpos, out var value);
		Debug.Assert(got, "Don't have ground tile at this location");
		return value;
	}

	public bool GetGroundTile(Vector2I localpos, out GroundTileType gtile) {
		return groundTiles.TryGetValue(localpos, out gtile);
	}

	public bool GroundTileHas(Vector2I localpos, GroundTileType flags) {
		var tile = GetGroundTile(localpos);
		return (tile & flags) != 0;
	}

	public Span<KeyValuePair<Vector2I, GroundTileType>> GetGroundTiles() {
		return groundTiles.ToArray();
	}

	public bool AddNeighbor(Region neighbor) {
		Debug.Assert(neighbor is not null, "Don't add useless null neighbors");
		Debug.Assert(neighbor != this, "I am not my own neighbro");
		return neighbors.Add(neighbor);
	}

	public void SetLocalFaction(Faction regionFaction) {
		LocalFaction = regionFaction;
	}

	public bool HasBuildingSpace(Vector2I position) {
		return !mapObjects.ContainsKey(position) && groundTiles.ContainsKey(position);
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
		Debug.Assert(HasMapObject(tile), "Region has no map object at {tile}");
		return mapObjects[tile];
	}

	public void RemoveMapObject(Vector2I tile) {
		Debug.Assert(HasMapObject(tile), $"There is no map object to remove at {tile}");
		var ob = mapObjects[tile];
		mapObjects.Remove(tile);
		ob.Remove();
		NotifyMapObjectUpdateAt(tile);
		NaturalResources.Touch();
	}

	MapObject CreateMapObjectSpotAndPlace(MapObject.IMapObjectType type, Vector2I position) {
		Debug.Assert(!HasMapObject(position, out var m), $"there's already a mapobject {m} at position {position}");
		Debug.Assert(type.IsPlacementAllowed(groundTiles[position]), $"Can't place map object {type.AssetName} here ({position} on {groundTiles[position]})");
		var ob = type.CreateMapObject(WorldPosition + position, this);
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
		Debug.Assert(from.groundTiles.ContainsKey(fromCoordinate), $"Region to annex from doesn't own tile at {fromCoordinate}");
		var globalCoord = fromCoordinate + from.WorldPosition;
		var localCoord = globalCoord - WorldPosition;
		Debug.Assert(!groundTiles.ContainsKey(localCoord), $"Annexing region somehow already owns tile at {localCoord}");
		var tile = from.groundTiles[fromCoordinate];
		from.groundTiles.Remove(fromCoordinate);
		groundTiles[localCoord] = tile;
		GameMan.Game.Map.TileOwners[globalCoord] = this;
		if (from.HasMapObject(fromCoordinate)) {
			Debug.Assert(!HasMapObject(localCoord), "Somehow, I already have a map object where im trying to steal it from??");
			if (from.LocalFaction.GetJob(globalCoord, out var job)) {
				from.LocalFaction.RemoveJob(job);
			}
			if (from.LocalFaction.HasProblem(fromCoordinate)) {
				from.LocalFaction.RemoveProblem(fromCoordinate);
			}
			var mop = from.GetMapObject(fromCoordinate);
			from.mapObjects.Remove(fromCoordinate);
			from.NotifyMapObjectUpdateAt(fromCoordinate);
			from.NaturalResources.Touch();
			mapObjects[localCoord] = mop;
			mop.ChangedRegions(this);
			NotifyMapObjectUpdateAt(localCoord);
			NaturalResources.Touch();
		}
		from.NotifyTileChangedAt(fromCoordinate);
		NotifyTileChangedAt(localCoord);
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
						else if (GD.Randf() < 0.01f) rs.Add(new Vector2I(i, j), Registry.ResourceSitesS.FishingSpot);
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

	public static class GenerationAccessor {

		static readonly (Vector2I, byte)[] GrowDirs = { (Vector2I.Right, 0b1), (Vector2I.Left, 0b10), (Vector2I.Down, 0b100), (Vector2I.Up, 0b1000) };

		public static bool GrowAllRegionsOneStep(
			Region[] regions, Dictionary<Vector2I, Region> occupied,
			Dictionary<Region, List<(Vector2I, byte)>> freeEdgeTiles,
			World world,
			RandomNumberGenerator rng,
			int iterations = 10,
			bool sea = false
		) {
			var growthOccurred = false;

			for (int xxx = 0; xxx < iterations; xxx++) for (int i = 0; i < regions.Length; i++) {
					var region = regions[i];
					var freeEdges = freeEdgeTiles[region];
					var c = freeEdges.Count;
					for (int x = 0; x < c; x++) {
						var addKeys = new HashSet<Vector2I>(); // coordinates in region local space
						addKeys.Clear();
						for (int dirIx = 0; dirIx < 4; dirIx++) {
							var ix = rng.RandiRange(0, freeEdges.Count - 1);
							growthOccurred = GrowRegionInDirection(occupied, addKeys, freeEdges, ix, region, dirIx, world, GroundTileType.All) || growthOccurred;
							if (freeEdges.Count == 0) break;
						}
						foreach (var k in addKeys) {
							Debug.Assert(!region.groundTiles.ContainsKey(k), "region {region} already owns the local tile {k}");
							region.groundTiles.Add(k, world.GetTile(k.X + region.WorldPosition.X, k.Y + region.WorldPosition.Y));
						}
						if (freeEdges.Count == 0) break;
					}
				}
			if (!growthOccurred) {
				GD.Print("WorldGenerator::GrowAllRegionsOneStep : region growth filled up all space attainable");
			}
			return growthOccurred;
		}

		private static bool GrowRegionInDirection(
			Dictionary<Vector2I, Region> occupied, // global spcae
			HashSet<Vector2I> addKeys, // region space
			List<(Vector2I, byte)> freeEdgeTiles, // local space
			int tileIndex,
			Region region,
			int dirIx,
			World world,
			GroundTileType allowedTile
		) {
			var (vectorDirectionTryingToGrowIn, directionTryingToGrowIn) = GrowDirs[dirIx];

			int i = tileIndex;
			{
				var (localPos, directionsThatAreFree) = freeEdgeTiles[i];
				if ((directionTryingToGrowIn & directionsThatAreFree) == 0) return false;

				var moveLocal = localPos + vectorDirectionTryingToGrowIn;
				var moveGlobal = region.WorldPosition + moveLocal;
				var (neighbor, grew) = TryGrowRegionTo(region, moveGlobal, occupied, addKeys, world, allowedTile);
				if (neighbor != null && neighbor != region) {
					region.AddNeighbor(neighbor);
					neighbor.AddNeighbor(region);
				}

				directionsThatAreFree &= (byte)~directionTryingToGrowIn;
				if (grew) {
					byte opposite = directionTryingToGrowIn switch { 0b10 => 0b1, 0b01 => 0b10, 0b100 => 0b1000, 0b1000 => 0b100, _ => throw new NotImplementedException() };
					freeEdgeTiles.Add((moveLocal, (byte)(0b1111 & (byte)~opposite)));
				}

				if (directionsThatAreFree == 0) freeEdgeTiles.RemoveAt(i);
				else {
					freeEdgeTiles[i] = (localPos, directionsThatAreFree);
				}

				return grew;
			}
		}

		private static (Region, bool) TryGrowRegionTo(
			Region region,
			Vector2I where, // world space
			Dictionary<Vector2I, Region> occupied, // world space
			HashSet<Vector2I> addKeys, // region space
			World world,
			GroundTileType allowedTile
		) {
			occupied.TryGetValue(where, out Region there);
			var local = where - region.WorldPosition;
			//var tileAt = world.GetTile(where.X, where.Y);
			if (there == null && /*tileAt == allowedTile && */ !addKeys.Contains(local) && where.X >= 0 && where.X < world.Width && where.Y >= 0 && where.Y < world.Height) {
				Debug.Assert(!occupied.ContainsKey(where), "Tile I thought was good to grow onto is already planned to be used!!");
				addKeys.Add(local);
				Debug.Assert(!occupied.ContainsKey(where), "Tile I thought was good to grow onto is already occupied!!");
				occupied.Add(where, region);
				return (null, true);
			}
			return (there, false);
		}

	}

}
