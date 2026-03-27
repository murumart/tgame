using System.Collections.Generic;
using System.Text;
using Godot;
using resources.game.building_types;
using scenes.autoload;
using scenes.region.ui;
using scenes.ui;
using static Building;
using static Document;

namespace scenes.region;

public partial class PlayerRegion : Node {

	[Export] UI ui;
	[Export] RegionCamera camera;
	[Export] RegionDisplay regionDisplay;
	[Export] Node otherDisplaysParent;

	Region region;
	Faction faction;
	FactionActions actions;

	public override void _Ready() {
		region = GameMan.Game.PlayRegion;
		faction = region.LocalFaction;
		actions = new(region, faction);

		ui.MapClickEvent += MapClick;
		ui.RequestBuildEvent += OnUIBuildingPlaceRequested;
		ui.GetBuildingTypesEvent += GetBuildingTypes;
		ui.GetResourcesEvent += actions.GetResourceStorage;
		ui.GetHasBuildingMaterialsEvent += faction.HasBuildingMaterials;
		ui.GetCanBuildEvent += actions.CanPlaceBuilding;
		ui.GetTimeStringEvent += GetDateTimeString;
		ui.GetJobsEvent += faction.GetJobs;
		ui.GetMapObjectJobEvent += actions.GetMapObjectsJob;
		ui.AddJobRequestedEvent += actions.AddJob;
		ui.AddProblemJobRequestedEvent += actions.AddJob;
		ui.GetMaxFreeWorkersEvent += GetJobMaxWorkers;
		ui.ChangeJobWorkerCountEvent += actions.ChangeJobWorkerCount;
		ui.RemoveJobEvent += actions.RemoveJob;
		ui.GetFoodAndUsageEvent += actions.GetFoodAndUsage;

		faction.JobRemovedEvent += j => {
			if (j is ConstructBuildingJob cj && cj.Building.IsConstructed) ui.Notifications.Notify($"The {cj.Building.Type.AssetName} has been constructed.", timeLimit: 10f);
			if (j is DemolishMapObjectJob dj && dj.GetProgressEstimate() >= 1f) ui.Notifications.Notify($"The {dj.Object.Type.AssetName} has been demolished.", timeLimit: 10f);
			if (j is GatherResourceJob gj && !gj.Well.HasBunches) ui.Notifications.Notify($"The {gj.Site.Type.AssetName} has been depleted of {gj.Well.ResourceType.AssetName}.", timeLimit: 15f);
		};
		faction.MyTradeOfferAcceptedEvent += (o, u) => ui.Notifications.Notify($"{o.Recipient.Name} accepted a trade offer from us. ({o.GetOutputDescription(faction, u)})", timeLimit: 20f);
		faction.MyTradeOfferRejectedEvent += o => ui.Notifications.Notify($"{o.Recipient.Name} rejected a trade offer from us...", timeLimit: 10f);
		foreach (var neighbor in region.Neighbors) {
			if (neighbor.LocalFaction.IsWild) continue;
			neighbor.LocalFaction.Population.PopulationDroppedToZero += () => {
				ui.Notifications.Notify($"Communication ceases from our neighbor {neighbor.LocalFaction.Name}.");
			};
		}

		GameMan.Game.Time.TimePassedEvent += PassTime;
		GameMan.Game.Time.HourPassedEvent += HourlyUpdate;

		ui.GetFactionActionsEvent += GetFactionActions;
		ui.GetBriefcaseEvent += GetBriefcase;
		faction.ContractFailedEvent += OnRegionMandateFailed;
		faction.Population.ApprovalDroppedToZero += OnApprovalZeroed;
		faction.ProblemAddedEvent += ui.Notifications.Notify;
		faction.ProblemSolvedEvent += ui.Notifications.ProblemStopped;
		faction.ProblemUnsolvedEvent += ui.Notifications.ProblemStopped;

		region.MapObjectUpdatedAtEvent += OnRegionMapObjectUpdated;
		camera.Region = region;

		ui.TileSelectedEvent += regionDisplay.OnTileSelected;
		ui.TileDeselectedEvent += regionDisplay.OnTileDeselected;

		ui.SetupResourceDisplay();

		Callable.From(() => {
			regionDisplay.LoadRegion(region, 0, camera);

			// show also neighboring regions and neighbors' neighbors
			foreach (var neighbor in region.Neighbors) {
				var rdisp = RegionDisplay.Instantiate();
				otherDisplaysParent.AddChild(rdisp);
				rdisp.Modulate = new Color(0.3f, 0.3f, 0.3f).Lerp(neighbor.LocalFaction.Color, 0.05f);
				rdisp.Position = Tilemaps.TilePosToWorldPos(neighbor.WorldPosition - region.WorldPosition) - Tilemaps.TILE_SIZE / 2;
				rdisp.LoadRegion(neighbor, 1, camera);
			}
			foreach (var neighbor in GameMan.Game.Map.GetRegions()) {
				if (neighbor == region || region.Neighbors.Contains(neighbor)) continue;
				var rdisp = RegionDisplay.Instantiate();
				otherDisplaysParent.AddChild(rdisp);
				rdisp.Modulate = new Color(0.1f, 0.1f, 0.1f).Lerp(neighbor.LocalFaction.Color.Lightened(0.5f), 0.1f);
				rdisp.Position = Tilemaps.TilePosToWorldPos(neighbor.WorldPosition - region.WorldPosition) - Tilemaps.TILE_SIZE / 2;
				rdisp.LoadRegion(neighbor, 2, camera);
			}

			ui.Announce("What brought you here will not bring you much longer forward -- food supplies are dwindling.\n\n"
				+ "Procure something to eat for your expectant people. Nature, thankfully, can provide fruit and fish, if you just spend the effort to look.",
				title: "Note Well"
			);
		}).CallDeferred();

		UILayer.DebugDisplay(() => {
			return $"hunger: {faction.Population.Hunger}, growing: {faction.Population.OngrowingPopulation}";
		});
		UILayer.DebugDisplay(() => {
			var sb = new StringBuilder();
			var ai = (GamerAI)GameMan.Game.GetRegionAI(region);
			sb.Append("Resource wants:\n");
			foreach (var (food, want) in ai.ResourceWants) {
				sb.Append(food.AssetName).Append('\t').Append(want).Append('\n');
			}
			return sb.ToString();
		});
		UILayer.DebugDisplay(() => $"mousepos: {regionDisplay.GetLocalMousePosition()}");
		UILayer.DebugDisplay(() => $"trouble: {GameMan.Game.TroubleMaker.DelayUntilPlayerAction:F2}");
		UILayer.DebugDisplay(() => "Last Action Info:\n" + GameMan.Game.GetRegionAI(region).profiling.LastActionInfo());
		UILayer.DebugDisplay(() => "Last Decision Info:\n" + GameMan.Game.GetRegionAI(region).profiling.LastDecisionInfo());
	}

	public override void _UnhandledKeyInput(InputEvent @event) {
		var evt = @event as InputEventKey;
		if (evt.Pressed && evt.Keycode == Key.Key0) {
			GD.Print("PlayerRegion::_UnhandledKeyInput : DEBUG: moving back to world scene");
			GameMan.SceneTransition("res://scenes/map/world_man.tscn");
		}
		if (evt.Pressed && evt.Keycode == Key.Key9) {
			GameMan.Game.AIPlaysInPlayerRegion = !GameMan.Game.AIPlaysInPlayerRegion;
			GD.Print("PlayerRegion::_UnhandledKeyInput : DEBUG: ai plays in player region is " + GameMan.Game.AIPlaysInPlayerRegion);
		}
	}

	public override void _Notification(int what) { // teardown
		if (what == NotificationPredelete) {
			ui.MapClickEvent -= MapClick;
			ui.GetFactionActionsEvent -= GetFactionActions;
			ui.GetBuildingTypesEvent -= GetBuildingTypes;
			ui.RequestBuildEvent -= OnUIBuildingPlaceRequested;
			ui.GetResourcesEvent -= actions.GetResourceStorage;
			ui.GetHasBuildingMaterialsEvent -= faction.HasBuildingMaterials;
			ui.GetCanBuildEvent -= actions.CanPlaceBuilding;
			ui.GetTimeStringEvent -= GetDateTimeString;
			ui.GetMapObjectJobEvent -= actions.GetMapObjectsJob;
			ui.GetJobsEvent -= faction.GetJobs;
			ui.AddJobRequestedEvent -= actions.AddJob;
			ui.AddProblemJobRequestedEvent -= actions.AddJob;
			ui.GetMaxFreeWorkersEvent -= GetJobMaxWorkers;
			ui.ChangeJobWorkerCountEvent -= actions.ChangeJobWorkerCount;
			ui.GetBriefcaseEvent -= GetBriefcase;
			ui.GetFoodAndUsageEvent -= actions.GetFoodAndUsage;

			region.MapObjectUpdatedAtEvent -= OnRegionMapObjectUpdated;
			faction.ContractFailedEvent -= OnRegionMandateFailed;
			faction.Population.ApprovalDroppedToZero -= OnApprovalZeroed;
			faction.ProblemAddedEvent -= ui.Notifications.Notify;
			faction.ProblemSolvedEvent -= ui.Notifications.ProblemStopped;
			faction.ProblemUnsolvedEvent -= ui.Notifications.ProblemStopped;

			GameMan.Game.Time.HourPassedEvent -= HourlyUpdate;
			GameMan.Game.Time.TimePassedEvent -= PassTime;
			LocalAI.Profiling.EndProfiling();

			ui.QueueFree();
		}
	}

	// map clicks

	void MapClick(Vector2I tile) {
		if (faction.HasProblem(tile)) {
			ui.OnProblemClicked(faction.GetProblem(tile));
			ui.TileSelected(tile);
		} else if (faction.HasBuilding(tile)) {
			ui.OnBuildingClicked(faction.GetBuilding(tile));
			ui.TileSelected(tile);
		} else if (region.HasMapObject(tile, out MapObject mop) && mop is ResourceSite resourceSite) {
			ui.OnResourceSiteClicked(resourceSite);
			ui.TileSelected(tile);
		} else if (!region.GetGroundTile(tile, out _)) {
			// DEBUG annex
			foreach (var ne in region.Neighbors) {
				var thereCoord = tile + region.WorldPosition - ne.WorldPosition;
				if (ne.GetGroundTile(thereCoord, out _)) {
					region.AnnexTile(ne, thereCoord);
				}
			}
		}
	}

	// building

	private bool OnUIBuildingPlaceRequested(IBuildingType type, Vector2I tilePosition) {
		if (!actions.CanPlaceBuilding(type, tilePosition)) return false;
		Debug.Assert(actions.CanPlaceBuilding(type, tilePosition), $"Building {type} cannot be placed at {tilePosition} despite the UI's wish to do so");
		if (actions.CanPlaceBuilding(type, tilePosition)) {
			actions.PlaceBuilding(type, tilePosition);
			return true;
		}
		return false;
	}

	public List<BuildingType> GetBuildingTypes() {
		var list = new List<BuildingType>();
		foreach (var b in Registry.Buildings.GetAssets()) list.Add((BuildingType)b);
		return list;
	}

	// notifications

	void PassTime(TimeT minutes) { }

	Notification foodWarningNotification = null;

	void HourlyUpdate(TimeT timeInMinutes) {
		ui.HourlyUpdate(timeInMinutes);

		if (faction.Population.ArePeopleStarving && foodWarningNotification == null) {
			foodWarningNotification = ui.Notifications.Notify(
				"Starvation! Scramble to find something to feed your people!",
				gradientColors: (new Color(Palette.BrownRust, 0.0f), Palette.BrownRust),
				isDismissable: false,
				isPulsing: true
			);
		} else if (!faction.Population.ArePeopleStarving && foodWarningNotification != null && !foodWarningNotification.IsDismissing) {
			foodWarningNotification.Dismiss();
			foodWarningNotification = null;
		}

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
		//if (hour >= GameTime.HOURS_PER_DAY * GameTime.DAYS_PER_WEEK * GameTime.WEEKS_PER_MONTH && !ui.GameIsOver) {
		//	int satisfactionLevel = (int)(faction.Population.Approval * 3);
		//	ui.Announce("Congratulations, although it might still be early, for you have survived only one month, but one month "
		//		+ "nonetheless full of time.\n\n"
		//		+ ((satisfactionLevel == 0)
		//			? "And even if your successes thus far have been few, this Earth surely will provide ample opportunities for you to return...?"
		//			: (satisfactionLevel == 1)
		//				? "Your people grumble not much, but some grumbling is just and warranted by the way this world's hardships leave not behind you and your kin."
		//				: (satisfactionLevel == 2)
		//					? "There was beauty to your leadership, and grace, but also a required solid fist to squeeze out of the Earth what is rightfully ours. "
		//					+ "And well have you squeezed, for we are happy."
		//					: "What can even be said about what you have just accomplished?")
		//		+ "\n\nWe will part ways here, for now, but with keeping in mind the fanciful ways of time, and chance, we might surely hear tales of your accomplishments again."
		//		+ "\n\nYou are done here.",
		//		title: "You Survived a Month",
		//		callback: () => {
		//			ui.Notifications.Notify("You have succeeded.");
		//		}
		//	);
		//	GameOver();
		//}
	}

	void OnRegionMapObjectUpdated(Vector2I tile) { }

	void OnRegionMandateFailed(Document doc) {
		GD.Print("RegionMan::OnRegionMandateFailed : MY MANDATE FAILED:::::: DAMN");
		GameMan.SceneTransition("res://scenes/game_over.tscn");
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
		GameMan.Game.AIPlaysInPlayerRegion = true;
		ui.GameOver();
	}

	// get information (for UI)

	public uint GetJobMaxWorkers() => faction.GetFreeWorkers();

	public string GetTimeString() => $"{GameMan.Game.Time.GetDayHour():00}:{GameMan.Game.Time.GetHourMinute():00}";
	public string GetDateTimeString() => $"{GetTimeString()} {(int)GameMan.Game.Time.GetMonthDay():00}/{(int)(GameMan.Game.Time.GetMonth() + 1):00}";

	// UI action invokes

	FactionActions GetFactionActions() => actions;

	Briefcase GetBriefcase() => faction.Briefcase;

}

