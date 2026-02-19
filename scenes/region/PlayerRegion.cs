using System.Collections.Generic;
using Godot;
using resources.game.building_types;
using scenes.autoload;
using scenes.region.ui;
using static Building;
using static Document;

namespace scenes.region {

	public partial class PlayerRegion : Node {

		[Export] UI ui;
		[Export] RegionCamera camera;
		[Export] RegionDisplay regionDisplay;
		[Export] Node otherDisplaysParent;

		Region region;
		Faction faction;
		FactionActions actions;

		public override void _Ready() {
			region = GameMan.Singleton.Game.PlayRegion;
			faction = region.LocalFaction;
			actions = new(region, faction);

			ui.MapClickEvent += MapClick;
			ui.RequestBuildEvent += OnUIBuildingPlaceRequested;
			ui.GetBuildingTypesEvent += GetBuildingTypes;
			ui.GetResourcesEvent += actions.GetResourceStorage;
			ui.GetCanBuildEvent += actions.CanBuild;
			ui.GetTimeStringEvent += GetDateTimeString;
			ui.GetMapObjectJobEvent += actions.GetMapObjectsJob;
			ui.AddJobRequestedEvent += actions.AddJob;
			ui.GetMaxFreeWorkersEvent += GetJobMaxWorkers;
			ui.ChangeJobWorkerCountEvent += actions.ChangeJobWorkerCount;
			ui.DeleteJobEvent += actions.RemoveJob;
			ui.GetFoodAndUsageEvent += actions.GetFoodAndUsage;


			GameMan.Singleton.Game.Time.TimePassedEvent += PassTime;
			GameMan.Singleton.Game.Time.HourPassedEvent += HourlyUpdate;

			ui.GetFactionEvent += GetFaction;
			ui.GetBriefcaseEvent += GetBriefcase;
			faction.ContractFailedEvent += OnRegionMandateFailed;
			faction.Population.ApprovalDroppedToZero += OnApprovalZeroed;

			region.MapObjectUpdatedAtEvent += OnRegionMapObjectUpdated;
			camera.Region = region;

			ui.TileSelectedEvent += regionDisplay.OnTileSelected;
			ui.TileDeselectedEvent += regionDisplay.OnTileDeselected;

			ui.SetupResourceDisplay();

			Callable.From(() => {
				regionDisplay.LoadRegion(region, 0);

				// show also neighboring regions and neighbors' neighbors
				foreach (var neighbor in region.Neighbors) {
					var rdisp = RegionDisplay.Instantiate();
					otherDisplaysParent.AddChild(rdisp);
					rdisp.Modulate = new Color(0.3f, 0.3f, 0.3f).Lerp(neighbor.LocalFaction.Color, 0.05f);
					rdisp.Position = Tilemaps.TilePosToWorldPos(neighbor.WorldPosition - region.WorldPosition) - Tilemaps.TILE_SIZE / 2;
					rdisp.LoadRegion(neighbor, 1);
				}
				foreach (var neighbor in GameMan.Singleton.Game.Map.GetRegions()) {
					if (neighbor == region || region.Neighbors.Contains(neighbor)) continue;
					var rdisp = RegionDisplay.Instantiate();
					otherDisplaysParent.AddChild(rdisp);
					rdisp.Modulate = new Color(0.1f, 0.1f, 0.1f).Lerp(neighbor.LocalFaction.Color, 0.1f);
					rdisp.Position = Tilemaps.TilePosToWorldPos(neighbor.WorldPosition - region.WorldPosition) - Tilemaps.TILE_SIZE / 2;
					rdisp.LoadRegion(neighbor, 2);
				}

				if (faction.HasOwningFaction()) {
					var owner = faction.GetOwningFaction();
					ui.Announce($"Here, on the margins of the imperial lands of {owner.Name}, "
						+ $"a fresh administration over the populace of {faction.Name} takes feet. You are at its head and have the power -- and the responsibility -- to "
						+ $"lead its people toward brightness and prosperity, while serving the fickle needs of your masters in {owner.Name}. "
						+ "But the regions around you are not asleep -- they, too, buzz with excitement over possible riches in this fresh, untainted Earth. "
						+ "Nature is there to serve you! Go forth and claim, acquire, secure, in this Fevered World.",
						title: $"The Story of the Colony of {faction.Name}"
			   		);
				}
				ui.Announce("What brought you here will not bring you much longer forward -- food supplies are dwindling.\n\n"
					+ "Procure something to eat for your expectant people. Nature, thankfully, can provide fruit and fish, if you just spend the effort to look.",
					title: "Note Well"
				);
			}).CallDeferred();

			// DEBUG add assets
			//foreach (var r in Registry.Resources.GetAssets()) {
			//		GD.Print("RegionMan::_Ready : adding resource ", r);
			//		faction.Resources.AddResource(new(r, 50));
			//}
			UILayer.DebugDisplay(() => {
				return $"hunger: {faction.Population.Hunger}, growing: {faction.Population.OngrowingPopulation}";
			});
			UILayer.DebugDisplay(() => $"mousepos: {regionDisplay.GetLocalMousePosition()}");
		}

		public override void _UnhandledKeyInput(InputEvent @event) {
			var evt = @event as InputEventKey;
			if (evt.Pressed && evt.Keycode == Key.Key0) {
				GD.Print("PlayerRegion::_UnhandledKeyInput : DEBUG: moving back to world scene");
				GetTree().ChangeSceneToFile("res://scenes/map/world_man.tscn");
			}
		}

		public override void _Notification(int what) { // teardown
			if (what == NotificationPredelete) {
				ui.MapClickEvent -= MapClick;
				ui.GetFactionEvent -= GetFaction;
				ui.GetBuildingTypesEvent -= GetBuildingTypes;
				ui.RequestBuildEvent -= OnUIBuildingPlaceRequested;
				ui.GetResourcesEvent -= actions.GetResourceStorage;
				ui.GetCanBuildEvent -= actions.CanBuild;
				ui.GetTimeStringEvent -= GetDateTimeString;
				ui.GetMapObjectJobEvent -= actions.GetMapObjectsJob;
				ui.AddJobRequestedEvent -= actions.AddJob;
				ui.GetMaxFreeWorkersEvent -= GetJobMaxWorkers;
				ui.ChangeJobWorkerCountEvent -= actions.ChangeJobWorkerCount;
				ui.GetBriefcaseEvent -= GetBriefcase;
				ui.GetFoodAndUsageEvent -= actions.GetFoodAndUsage;

				region.MapObjectUpdatedAtEvent -= OnRegionMapObjectUpdated;
				faction.ContractFailedEvent -= OnRegionMandateFailed;
				faction.Population.ApprovalDroppedToZero -= OnApprovalZeroed;

				GameMan.Singleton.Game.Time.HourPassedEvent -= HourlyUpdate;
				GameMan.Singleton.Game.Time.TimePassedEvent -= PassTime;
				LocalAI.Profile.EndProfiling();

				ui.QueueFree();
			}
		}

		// map clicks

		void MapClick(Vector2I tile) {
			if (faction.HasBuilding(tile)) {
				ui.OnBuildingClicked(faction.GetBuilding(tile));
				ui.TileSelected(tile);
			} else if (region.HasMapObject(tile, out MapObject mop) && mop is ResourceSite resourceSite) {
				ui.OnResourceSiteClicked(resourceSite);
				ui.TileSelected(tile);
			} else if (!region.GroundTiles.ContainsKey(tile)) {
				// DEBUG annex
				//foreach (var ne in region.Neighbors) {
				//	var thereCoord = tile + region.WorldPosition - ne.WorldPosition;
				//	if (ne.GroundTiles.ContainsKey(thereCoord)) {
				//		region.AnnexTile(ne, thereCoord);
				//	}
				//}
			}
		}

		// building

		private void OnUIBuildingPlaceRequested(IBuildingType type, Vector2I tilePosition) {
			if (!actions.CanPlaceBuilding(type, tilePosition)) return;
			Debug.Assert(actions.CanPlaceBuilding(type, tilePosition), $"Building {type} cannot be placed at {tilePosition} despite the UI's wish to do so");
			if (actions.CanPlaceBuilding(type, tilePosition)) {
				actions.PlaceBuilding(type, tilePosition);
			}
		}

		public List<BuildingType> GetBuildingTypes() {
			var list = new List<BuildingType>();
			foreach (var b in Registry.Buildings.GetAssets()) list.Add((BuildingType)b);
			return list;
		}

		// notifications

		void PassTime(TimeT minutes) { }

		void HourlyUpdate(TimeT timeInMinutes) {
			ui.HourlyUpdate(timeInMinutes);
		}

		void OnRegionMapObjectUpdated(Vector2I tile) { }

		void OnRegionMandateFailed(Document doc) {
			GD.Print("RegionMan::OnRegionMandateFailed : MY MANDATE FAILED:::::: DAMN");
			GetTree().ChangeSceneToFile("res://scenes/game_over.tscn");
		}

		void OnApprovalZeroed() {
			faction.Population.ApprovalDroppedToZero -= OnApprovalZeroed;
			ui.Announce("Your policies, good intentioned as they were (ha), still were not enough for your people,"
				+ " and now they will not listen to you any more. Hopefully you have your bags packed and train booked,"
				+ " because leaving some space for whom you used to answer to now might be the best for your continued health.",
				title: "Approval Dropped to Zero",
				callback: () => {
					ui.MapClickEvent -= MapClick;
					ui.GameOver();
				}
			);
		}

		// get information (for UI)

		public uint GetJobMaxWorkers() => faction.GetFreeWorkers();

		public string GetTimeString() => $"{GameMan.Singleton.Game.Time.GetDayHour():00}:{GameMan.Singleton.Game.Time.GetHourMinute():00}";
		public string GetDateTimeString() => $"{GetTimeString()} {GameMan.Singleton.Game.Time.GetMonthDay():00}/{GameMan.Singleton.Game.Time.GetMonth() + 1:00}";

		// UI action invokes

		Faction GetFaction() => faction;

		Briefcase GetBriefcase() => faction.Briefcase;

	}

}
