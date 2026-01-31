using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.game;
using resources.game.building_types;
using scenes.autoload;
using scenes.region.buildings;
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

		// debug
		LocalAI ai;


		public override void _Ready() {
			region = GameMan.Singleton.Game.PlayRegion;
			faction = region.LocalFaction;
			actions = new(region, faction);
			// debug
			ai = new(actions);

			ui.MapClickEvent += MapClick;
			ui.RequestBuildEvent += OnUIBuildingPlaceRequested;
			ui.GetBuildingTypesEvent += GetBuildingTypes;
			ui.GetResourcesEvent += actions.GetResourceStorage;
			ui.GetCanBuildEvent += actions.CanBuild;
			ui.GetTimeStringEvent += GetDateTimeString;
			ui.GetMapObjectJobsEvent += actions.GetMapObjectJobs;
			ui.AddJobRequestedEvent += actions.AddJob;
			ui.GetMaxFreeWorkersEvent += GetJobMaxWorkers;
			ui.ChangeJobWorkerCountEvent += actions.ChangeJobWorkerCount;
			ui.DeleteJobEvent += actions.RemoveJob;

			GameMan.Singleton.Game.Time.HourPassedEvent += HourlyUpdate;

			ui.GetFactionEvent += GetFaction;
			ui.GetBriefcaseEvent += GetBriefcase;
			faction.ContractFailedEvent += OnRegionMandateFailed;

			region.MapObjectUpdatedAtEvent += OnRegionMapObjectUpdated;

			ui.PauseRequestedEvent += UiTogglePause;
			ui.GameSpeedChangeRequestedEvent += UiChangeGameSpeed;

			camera.Region = region;

			if (GameMan.Singleton.Game.Time.Minutes == 0) GameMan.Singleton.Game.PassTime(60 * 7); // start game at 7:00

			regionDisplay.LoadRegion(region);

			// show also neighboring regions and neighbors' neighbors
			HashSet<Region> secondLevel = new();
			foreach (var neighbor in region.Neighbors) {
				var rdisp = RegionDisplay.Instantiate();
				otherDisplaysParent.AddChild(rdisp);
				rdisp.Modulate = new Color(0.3f, 0.3f, 0.3f).Lerp(neighbor.Color, 0.05f);
				rdisp.Position = Tilemaps.TilePosToWorldPos(neighbor.WorldPosition - region.WorldPosition) - Tilemaps.TILE_SIZE / 2;
				rdisp.LoadRegion(neighbor);
				foreach (var n in neighbor.Neighbors) if (n != region && !region.Neighbors.Contains(n)) secondLevel.Add(n);
			}
			foreach (var neighbor in secondLevel) {
				var rdisp = RegionDisplay.Instantiate();
				otherDisplaysParent.AddChild(rdisp);
				rdisp.Modulate = new Color(0.1f, 0.1f, 0.1f).Lerp(region.Color, 0.1f);
				rdisp.Position = Tilemaps.TilePosToWorldPos(neighbor.WorldPosition - region.WorldPosition) - Tilemaps.TILE_SIZE / 2;
				rdisp.LoadRegion(neighbor);
			}
			secondLevel = null;

			// DEBUG add assets
			//foreach (var r in Registry.Resources.GetAssets()) {
			//		GD.Print("RegionMan::_Ready : adding resource ", r);
			//		faction.Resources.AddResource(new(r, 50));
			//}
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
				ui.PauseRequestedEvent -= UiTogglePause;
				ui.GameSpeedChangeRequestedEvent -= UiChangeGameSpeed;
				ui.GetMapObjectJobsEvent -= actions.GetMapObjectJobs;
				ui.AddJobRequestedEvent -= actions.AddJob;
				ui.GetMaxFreeWorkersEvent -= GetJobMaxWorkers;
				ui.ChangeJobWorkerCountEvent -= actions.ChangeJobWorkerCount;
				ui.GetBriefcaseEvent -= GetBriefcase;

				region.MapObjectUpdatedAtEvent -= OnRegionMapObjectUpdated;
				faction.ContractFailedEvent -= OnRegionMandateFailed;

				GameMan.Singleton.Game.Time.HourPassedEvent -= HourlyUpdate;
				LocalAI.Profile.EndProfiling();

				ui.QueueFree();
			}
		}

		// map clicks

		void MapClick(Vector2I tile) {
			if (faction.HasBuilding(tile)) {
				ui.OnBuildingClicked(faction.GetBuilding(tile));
			} else if (region.HasMapObject(tile, out MapObject mop) && mop is ResourceSite resourceSite) {
				ui.OnResourceSiteClicked(resourceSite);
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

		public void PlaceBuilding(IBuildingType type, Vector2I tilepos) {
			var buildin = actions.PlaceBuilding(type, tilepos);
		}

		private void OnUIBuildingPlaceRequested(IBuildingType type, Vector2I tilePosition) {
			Debug.Assert(actions.CanPlaceBuilding(type, tilePosition), $"Building {type} cannot be placed at {tilePosition} despite the UI's wish to do so");
			if (actions.CanPlaceBuilding(type, tilePosition)) {
				PlaceBuilding(type, tilePosition);
			}
		}

		public List<BuildingType> GetBuildingTypes() {
			var list = new List<BuildingType>();
			foreach (var b in Registry.Buildings.GetAssets()) list.Add((BuildingType)b);
			return list;
		}

		// notifications

		void HourlyUpdate(TimeT timeInMinutes) {
			if (timeInMinutes >= 60 * 8) ai.Update(timeInMinutes); // debug
			ui.HourlyUpdate(timeInMinutes);
		}

		void OnRegionMapObjectUpdated(Vector2I tile) { }

		void OnRegionMandateFailed(Document doc) {
			GD.Print("RegionMan::OnRegionMandateFailed : MY MANDATE FAILED:::::: DAMN");
			GetTree().ChangeSceneToFile("res://scenes/game_over.tscn");
		}

		// get information (for UI)



		public uint GetJobMaxWorkers() => faction.GetFreeWorkers();

		public string GetTimeString() => $"{GameMan.Singleton.Game.Time.GetDayHour():00}:{GameMan.Singleton.Game.Time.GetHourMinute():00}";
		public string GetDateTimeString() => $"{GetTimeString()} {GameMan.Singleton.Game.Time.GetMonthDay():00}/{GameMan.Singleton.Game.Time.GetMonth() + 1:00}";

		// UI action invokes

		public void UiChangeGameSpeed(float by) {
			GameMan.Singleton.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, by);
		}

		public bool UiTogglePause() {
			GameMan.Singleton.TogglePause();
			return GameMan.Singleton.IsPaused;
		}

		Faction GetFaction() => faction;

		Briefcase GetBriefcase() => faction.Briefcase;

	}

}
