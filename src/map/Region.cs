using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using scenes.ui;
using static ResourceSite;


public class Region {

	public event Action<Vector2I> MapObjectUpdatedAtEvent;
	public void NotifyMapObjectUpdateAt(Vector2I p) => MapObjectUpdatedAtEvent?.Invoke(p);

	public event Action<Vector2I> TileChangedAtEvent;

	public event Action<Region> NewNeighborGainedEvent;
	public event Action DisappearedEvent;

	public readonly int WorldIndex;

	public Vector2I WorldPosition { get; init; }
	readonly Dictionary<Vector2I, GroundTileType> groundTiles = new();
	readonly List<(Vector2I Position, byte FreeDirections)> freeEdgeTiles = new() { (Vector2I.Zero, 0b1111) };
	readonly Dictionary<Vector2I, (Region ToRight, Region ToLeft, Region Below, Region Above)> edges = new();
	public IEnumerable<Vector2I> GroundTilePositions => groundTiles.Keys;
	readonly HashSet<Region> neighbors = new(); public ICollection<Region> Neighbors => neighbors;

	public Faction LocalFaction { get; private set; }

	readonly Dictionary<Vector2I, MapObject> mapObjects = new();

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

	public int GetLandTileCount() => groundTiles.Values.Where(t => (t & GroundTileType.HasLand) != 0).Count();
	public int GetOceanTileCount() => groundTiles.Values.Where(t => (t & GroundTileType.HasLand) == 0).Count();

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

	public bool GetEdge(Vector2I pos, out (Region Right, Region Left, Region Below, Region Above) edge) {
		return edges.TryGetValue(pos, out edge);
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

	public void AnnexTile(Region from, Vector2I fromCoordinate, Dictionary<Vector2I, Region> TileOwners, bool bulkop = false) {
		Debug.Assert(from.groundTiles.ContainsKey(fromCoordinate), $"Region to annex from doesn't own tile at {fromCoordinate}");
		var globalCoord = fromCoordinate + from.WorldPosition;
		var localCoord = globalCoord - WorldPosition;
		Debug.Assert(!groundTiles.ContainsKey(localCoord), $"Annexing region somehow already owns tile at {localCoord}");
		var tile = from.groundTiles[fromCoordinate];
		from.groundTiles.Remove(fromCoordinate);
		groundTiles[localCoord] = tile;
		TileOwners[globalCoord] = this;
		if (!bulkop) {
			// a bit lazy to regenerate edges like this
			GenerationAccessor.RebuildEdge(this, TileOwners);
			GenerationAccessor.RebuildEdge(from, TileOwners);
		}
		if (from.HasMapObject(fromCoordinate)) {
			Debug.Assert(!HasMapObject(localCoord), "Somehow, I already have a map object where im trying to steal it from??");
			if (from.LocalFaction.GetJob(globalCoord, out var job)) {
				from.LocalFaction.RemoveJob(job);
			}
			if (from.LocalFaction.HasProblem(fromCoordinate)) {
				from.LocalFaction.RemoveProblem(fromCoordinate);
			}
			// could have been removed if was in progress consturction job
			if (from.HasMapObject(fromCoordinate)) {
				var mop = from.GetMapObject(fromCoordinate);
				from.mapObjects.Remove(fromCoordinate);
				from.NotifyMapObjectUpdateAt(fromCoordinate);
				from.NaturalResources.Touch();
				from.LocalFaction.OnMapObjectRemoved(mop);
				mapObjects[localCoord] = mop;
				mop.ChangedRegions(this);
				NotifyMapObjectUpdateAt(localCoord);
				NaturalResources.Touch();
				LocalFaction.OnMapObjectAdded(mop);
			}
		}
		if (!bulkop) { // only thing currently connected is the RegionDisplay really slow rebuild method
			from.TileChangedAtEvent?.Invoke(fromCoordinate);
			TileChangedAtEvent?.Invoke(localCoord);
		}
		// check if the tile change exposed new neighbors
		for (int x = -1; x <= 1; x += 2) {
			for (int y = -1; y <= 1; y += 2) {
				var othercheck = globalCoord + new Vector2I(x, y);
				var has = TileOwners.TryGetValue(othercheck, out var reg);
				if (!has || reg == this) continue;
				if (!Neighbors.Contains(reg)) {
					// found a new neighbord!
					AddNeighbor(reg);
					reg.AddNeighbor(this);
					NewNeighborGainedEvent?.Invoke(reg);
					reg.NewNeighborGainedEvent?.Invoke(this);
				}
			}
		}
	}

	public void AnnexAll(Region from, Dictionary<Vector2I, Region> TileOwners) {
		LocalFaction.Absorb(from.LocalFaction);
		foreach (var t in from.groundTiles.Keys) {
			AnnexTile(from, t, TileOwners, bulkop: true);
		}
		GenerationAccessor.RebuildEdge(this, TileOwners);
		GenerationAccessor.RebuildEdge(from, TileOwners);
		TileChangedAtEvent?.Invoke(Vector2I.Zero); // only thing connected is currently ReginoDisplay, this should be fixed better style.
		from.DisappearedEvent?.Invoke();
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
		return $"Reg({WorldPosition},({LocalFaction}))";
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

	public static RegionDisplayHighlightDisplayFunc GetEdgesHighlightFunction(Dictionary<Vector2I, Region> tileOwners) {
		return (s, gp, lp, dis) => {
			if (!tileOwners.TryGetValue(gp, out var reg)) {
				s.Modulate = new(Palette.Dark, 0.2f);
				return;
			}
			if (reg.edges.TryGetValue(gp - reg.WorldPosition, out var e)) {
				s.Modulate = new(reg.LocalFaction.Color, 0.2f);
				return;
			}
			s.Modulate = new(Palette.BrownRust, 0.2f);
		};
	}

	public static class GenerationAccessor {

		enum DirectionBits : byte {
			Right = 0b1,
			Left  = 0b10,
			Down  = 0b100,
			Up    = 0b1000,
		}

		static readonly (Vector2I Direction, DirectionBits ByteRep)[] GrowDirs = [
			(Vector2I.Right, DirectionBits.Right),
			(Vector2I.Left, DirectionBits.Left),
			(Vector2I.Down, DirectionBits.Down),
			(Vector2I.Up, DirectionBits.Up),
  		];

		public static bool GrowAllRegionsOneStep(
			Region[] regions, Dictionary<Vector2I, Region> occupied,
			World world,
			RandomNumberGenerator rng,
			int iterations = 10,
			bool sea = false
		) {
			var growthOccurred = false;

			for (int xxx = 0; xxx < iterations; xxx++) foreach (var region in regions) {
					var c = region.freeEdgeTiles.Count;
					for (int x = 0; x < c; x++) {
						var addKeys = new HashSet<Vector2I>(); // coordinates in region local space
						addKeys.Clear();
						for (int dirIx = 0; dirIx < 4; dirIx++) {
							var ix = rng.RandiRange(0, region.freeEdgeTiles.Count - 1);
							growthOccurred = GrowRegionInDirection(occupied, addKeys, ix, region, dirIx, world, GroundTileType.All) || growthOccurred;
							if (region.freeEdgeTiles.Count == 0) break;
						}
						foreach (var k in addKeys) {
							Debug.Assert(!region.groundTiles.ContainsKey(k), "region {region} already owns the local tile {k}");
							region.groundTiles.Add(k, world.GetTile(k.X + region.WorldPosition.X, k.Y + region.WorldPosition.Y));
						}
						if (region.freeEdgeTiles.Count == 0) break;
					}
				}
			if (!growthOccurred) {
				GD.Print("Region::GenerationAccessor::GrowAllRegionsOneStep : region growth filled up all space attainable");
			}
			return growthOccurred;
		}

		private static bool GrowRegionInDirection(
			Dictionary<Vector2I, Region> occupied, // global spcae
			HashSet<Vector2I> addKeys, // region space
			int tileIndex,
			Region region,
			int dirIx,
			World world,
			GroundTileType allowedTile
		) {
			var (vectorDirectionTryingToGrowIn, directionTryingToGrowIn) = GrowDirs[dirIx];

			var (localPos, directionsThatAreFree) = region.freeEdgeTiles[tileIndex];
			if (((byte)directionTryingToGrowIn & directionsThatAreFree) == 0) return false;

			var moveLocal = localPos + vectorDirectionTryingToGrowIn;
			var moveGlobal = region.WorldPosition + moveLocal;
			var (neighbor, grew) = TryGrowRegionTo(region, moveGlobal, occupied, addKeys, world, allowedTile);
			if (neighbor != null && neighbor != region) {
				region.AddNeighbor(neighbor);
				neighbor.AddNeighbor(region);
			}

			directionsThatAreFree &= (byte)~directionTryingToGrowIn;
			if (grew) {
				DirectionBits opposite = directionTryingToGrowIn switch {
					DirectionBits.Left => DirectionBits.Right,
					DirectionBits.Right => DirectionBits.Left,
					DirectionBits.Down => DirectionBits.Up,
					DirectionBits.Up => DirectionBits.Down,
					_ => throw new NotImplementedException(),
				};
				region.freeEdgeTiles.Add((moveLocal, (byte)(0b1111 & (byte)~opposite)));
			} else {
				// region edge detection could be done smartly probably TODO
				//bool edged = region.edges.TryGetValue(localPos, out var e);
				//if (!edged) {
				//	e = (null, null, null, null);
				//}
				//switch (directionTryingToGrowIn) {
				//	case DirectionBits.Right: e = (neighbor, e.ToLeft, e.Below, e.Above); break;
				//	case DirectionBits.Left: e = (e.ToRight, neighbor, e.Below, e.Above); break;
				//	case DirectionBits.Down: e = (e.ToRight, e.ToLeft, neighbor, e.Above); break;
				//	case DirectionBits.Up: e = (e.ToRight, e.ToLeft, e.Below, neighbor); break;
				//}
				//region.edges[localPos] = e;
			}

			if (directionsThatAreFree == 0) region.freeEdgeTiles.RemoveAt(tileIndex);
			else {
				region.freeEdgeTiles[tileIndex] = (localPos, directionsThatAreFree);
			}

			return grew;
		}

		private static (Region Neighbor, bool Grew) TryGrowRegionTo(
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

		public static void BuildEdges(Span<Region> regions, Dictionary<Vector2I, Region> TileOwners) {
			// edge detection that's sucks, probably could do this during worlgen instead
			// (Region ToRight, Region ToLeft, Region Below, Region Above)
			Region[] neighborsoftile = new Region[4];
			foreach (Region region in regions) {
				foreach (var pos in region.groundTiles.Keys) {
					for (int i = 0; i < 4; i++) {
						neighborsoftile[i] = TileOwners.GetValueOrDefault(pos + region.WorldPosition + GrowDirs[i].Direction, null);
					}
					if (neighborsoftile.Any(a => a is null || a != region)) {
						region.edges.Add(pos, (neighborsoftile[0], neighborsoftile[1], neighborsoftile[2], neighborsoftile[3]));
					}
				}
			}
		}

		public static void RebuildEdge(Region region, Dictionary<Vector2I, Region> TileOwners) {
			Region[] neighborsoftile = new Region[4];
			region.edges.Clear();
			foreach (var pos in region.groundTiles.Keys) {
				for (int i = 0; i < 4; i++) {
					neighborsoftile[i] = TileOwners.GetValueOrDefault(pos + region.WorldPosition + GrowDirs[i].Direction, null);
				}
				if (neighborsoftile.Any(a => a is null || a != region)) {
					region.edges[pos] = (neighborsoftile[0], neighborsoftile[1], neighborsoftile[2], neighborsoftile[3]);
				}
			}
		}

	}

}
