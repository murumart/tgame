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

			faction.JobRemovedEvent += j => {
				if (j is ConstructBuildingJob cj && cj.Building.IsConstructed) ui.Notifications.Notify($"The {cj.Building.Type.AssetName} has been constructed.");
				if (j is GatherResourceJob gj && !gj.Well.HasBunches) ui.Notifications.Notify($"The {gj.Site.Type.AssetName} has been depleted of {gj.Well.ResourceType.AssetName}.");
			};
			foreach (var neighbor in region.Neighbors) {
				neighbor.LocalFaction.Population.PopulationDroppedToZero += () => {
					ui.Notifications.Notify($"Communication ceases from our neighbor {neighbor.LocalFaction.Name}.");
				};
			}

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
					rdisp.Modulate = new Color(0.1f, 0.1f, 0.1f).Lerp(neighbor.LocalFaction.Color.Lightened(0.5f), 0.1f);
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

			// my beautiful story.
			TimeT hour = timeInMinutes / GameTime.MINUTES_PER_HOUR;
			switch (hour) {
				case 8:
					ui.Notifications.Notify("Your neighbors are coming alive.");
					break;
				case 9:
					ui.Notifications.Notify("(Press the X to dismiss these.) (But pay attention, too.)");
					break;
				case 48:
					if (faction.Population.HousedCount > 5) {
						ui.Notifications.Notify("It's good that you've started building homes for your people. People who feel safe at their own home will consider procreating.");
					} else {
						ui.Notifications.Notify("Your people really prefer to live in homes. Even log cabins suffice. With no homeliness, no new generation can be born...");
					}
					break;
				case 82:
					ui.Notifications.Notify("The marketplace is a building used to trade with your neighboring regions. Who knows, maybe they have something you really want...?");
					break;
				default: break;
			}
			if (hour >= GameTime.HOURS_PER_DAY * GameTime.DAYS_PER_WEEK * GameTime.WEEKS_PER_MONTH && !ui.GameIsOver) {
				int satisfactionLevel = (int)(faction.Population.Approval * 3);
				ui.Announce("Congratulations, although it might still be early, for you have survived only one month, but one month "
					+ "nonetheless full of time.\n\n"
					+ ((satisfactionLevel == 0)
						? "And even if your successes thus far have been few, this Earth surely will provide ample opportunities for you to return...?"
						: (satisfactionLevel == 1)
							? "Your people grumble not much, but some grumbling is just and warranted by the way this world's hardships leave not behind you and your kin."
							: (satisfactionLevel == 2)
								? "There was beauty to your leadership, and grace, but also a required solid fist to squeeze out of the Earth what is rightfully ours. "
								+ "And well have you squeezed, for we are happy."
								: "What can even be said about what you have just accomplished?")
					+ "\n\nWe will part ways here, for now, but with keeping in mind the fanciful ways of time, and chance, we might surely hear tales of your accomplishments again."
					+ "\n\nYou are done here.",
					title: "You Survived a Month",
					callback: () => {
						ui.Notifications.Notify("You have succeeded.");
					}
				);
				GameOver();
			}
		}

		void OnRegionMapObjectUpdated(Vector2I tile) { }

		void OnRegionMandateFailed(Document doc) {
			GD.Print("RegionMan::OnRegionMandateFailed : MY MANDATE FAILED:::::: DAMN");
			GetTree().ChangeSceneToFile("res://scenes/game_over.tscn");
		}

		void OnApprovalZeroed() {
			if (ui.GameIsOver) return;
			faction.Population.ApprovalDroppedToZero -= OnApprovalZeroed;
			ui.Announce("Your policies, good intentioned as they were (ha), still were not enough for your people,"
				+ " and now they will not listen to you any more. Hopefully you have your bags packed and train booked,"
				+ " because leaving some space for whom you used to answer to now might be the best for your continued health.",
				title: "Approval Dropped to Zero",
				callback: () => {
					ui.Notifications.Notify("You have failed.");
				}
			);
			GameOver();
		}

		void GameOver() {
			ui.MapClickEvent -= MapClick;
			GameMan.Singleton.Game.AIPlaysInPlayerRegion = true;
			ui.GameOver();
		}

		// get information (for UI)

		public uint GetJobMaxWorkers() => faction.GetFreeWorkers();

		public string GetTimeString() => $"{GameMan.Singleton.Game.Time.GetDayHour():00}:{GameMan.Singleton.Game.Time.GetHourMinute():00}";
		public string GetDateTimeString() => $"{GetTimeString()} {GameMan.Singleton.Game.Time.GetMonthDay():00}/{(int)(GameMan.Singleton.Game.Time.GetMonth() + 1):00}";

		// UI action invokes

		Faction GetFaction() => faction;

		Briefcase GetBriefcase() => faction.Briefcase;

	}

}
