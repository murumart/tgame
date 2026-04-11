using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using resources.game.building_types;
using scenes.autoload;
using scenes.map.ui;
using scenes.ui;
using static Document;
using IBuildingType = Building.IBuildingType;


namespace scenes.region.ui;

public partial class UI : Control {

	// one big script to rule all region ui interactions

	public event Action<Vector2I> MapClickEvent;

	public event Func<IBuildingType, Vector2I, bool> RequestBuildEvent;
	public event Func<IBuildingType, bool> GetHasBuildingMaterialsEvent;
	public event Func<IBuildingType, Vector2I, bool> GetCanBuildEvent;
	public event Func<ResourceStorage> GetResourcesEvent;
	public event Func<FactionActions> GetFactionActionsEvent;
	public event Func<(float, float)> GetFoodAndUsageEvent;
	public event Action<Vector2I> TileSelectedEvent;
	public event Action TileDeselectedEvent;

	public event Func<MapObject, Job> GetMapObjectJobEvent;
	public event Func<IEnumerable<Job>> GetJobsEvent;
	public event Action<MapObject, MapObjectJob> AddJobRequestedEvent;
	public event Action<Problem, SolveProblemJob> AddProblemJobRequestedEvent;
	public event Func<uint> GetMaxFreeWorkersEvent;
	public event Action<Job, int> ChangeJobWorkerCountEvent;
	public event Action<Job> RemoveJobEvent;

	public event Func<Briefcase> GetBriefcaseEvent;

	public event Func<string> GetTimeStringEvent;
	public event Func<List<BuildingType>> GetBuildingTypesEvent;

	public enum State {
		Idle,
		ChoosingBuild,
		PlacingBuild,
		MapObjectMenu,
		AgreementsMenu,
		JobsMenu,
		TradeMenu,
		WorldMenu,
		WarMenu,
	}

	public enum Tab : int {
		None = -1, // because TabContainer -1 = none selected
		Build,
		Documents,
		Jobs,
		Trade,
		World,
		War,
	}

	public bool GameIsOver { get; private set; }

	// camera
	[Export] public RegionCamera Camera;

	// bottom bar buttons
	[Export] public Button buildButton;
	[Export] public Button agreementsButton;
	[Export] public Button jobsButton;
	[Export] public Button tradeButton;
	[Export] public Button worldButton;
	[Export] public Button warButton;

	// bottom bar menus menus
	[Export] TabContainer menuTabs;
	[Export] BuildingList buildingList;
	[Export] DocumentsDisplay documentsDisplay;
	[Export] JobsList jobsList;
	[Export] TradeInfoPanel tradeInfoPanel;
	[Export] WorldUI worldUI;
	[Export] WarInfoPanel warInfoPanel;

	// right
	[Export] public MapObjectMenu mopjectMenu;

	// top bar
	[Export] ResourceDisplay resourceDisplay;
	[Export] public Panel pauseDisplayPanel;

	// top bar bottom
	[Export] ControlButtons controlButtons;

	[Export] public RichTextLabel resourceLabel;

	// announcement box
	[Export] Button announcementOkayButton;
	[Export] RichTextLabel announcementText;
	[Export] Label announcementTitle;
	[Export] Control announcementParent;

	[Export] public Notifications Notifications;

	public bool AnnouncementActive { get => announcementParent.Visible; }

	// internal

	State _state;
	public State state {
		get => _state;
		set {
			var old = _state;
			_state = value;
			OnStateChanged(old, value);
			//Debug.PrintWithStack("UI: state changed to", value);
		}
	}

	// overrides and connections

	public override void _Ready() {
		buildButton.Pressed += () => OnTabButtonPressed(Tab.Build, State.ChoosingBuild);
		agreementsButton.Pressed += () => OnTabButtonPressed(Tab.Documents, State.AgreementsMenu);
		jobsButton.Pressed += () => OnTabButtonPressed(Tab.Jobs, State.JobsMenu);
		tradeButton.Pressed += () => OnTabButtonPressed(Tab.Trade, State.TradeMenu);
		worldButton.Pressed += () => OnTabButtonPressed(Tab.World, State.WorldMenu);
		warButton.Pressed += () => OnTabButtonPressed(Tab.War, State.WarMenu);

		OptionsMenu.VisibilityToggled += b => {
			if (b && !GameMan.IsPaused) {
				wasPausedBefore = true;
				GameMan.TogglePause();
			} else if (!b && !wasPausedBefore && GameMan.IsPaused) {
				GameMan.TogglePause();
			}
		};

		controlButtons.Camera = Camera;

		announcementOkayButton.Pressed += AnnouncementOkayPressed;

		Reset();
	}

	public override void _Process(double delta) {
		UpdateDisplays(); // todo move this to something that doesn't happen every frame... if it becomes a bottleneck
	}

	public override void _UnhandledInput(InputEvent @event) {
		base._UnhandledInput(@event);
		if (@event.IsActionPressed("escape")) {
			Escape();
		}
		if (@event is InputEventKey k && k.Pressed) {
			if (k.Keycode == Key.Key6) {
				if (controlButtons.TimeSpeedAlteringDisabled) return;
				GameMan.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, 1200);
				controlButtons.UpdateDisplays();
			}
		}
	}

	void OnTabButtonPressed(Tab which, State matchingState) {
		if (menuTabs.CurrentTab != (int)which) {
			state = matchingState;
			SelectTab(which);
		} else {
			state = State.Idle;
			SelectTab(Tab.None);
		}
	}

	// menu activites

	public void SelectTab(Tab which) {
		switch (which) {
			case Tab.None:
				buildingList.Reset();
				break;
			case Tab.Build:
				buildingList.Update();
				buildingList.Show();
				break;
			case Tab.Documents:
				documentsDisplay.Display(GetBriefcase());
				break;
			case Tab.Jobs:
				jobsList.Display();
				break;
			case Tab.Trade: {
					var fac = GetFactionActions();
					var pm = fac.GetProcessMarketJob();
					if (pm is null) {
						tradeInfoPanel.DisplayNoMarket();
					} else tradeInfoPanel.Display(fac.Faction, pm.TradeOffers);
				}
				break;
			case Tab.World:
				worldUI.DisplayWorld(GameMan.Game.Map.World, GameMan.Game);
				worldUI.DrawRegions();
				worldUI.SelectRegion(GetFactionActions().Region);
				break;
			case Tab.War:
				warInfoPanel.Display(GetFactionActions().Faction);
				break;
		}
		menuTabs.CurrentTab = (int)which;
	}

	bool _announcePaused = false;

	readonly struct AnnounceData(string text, string title, Action callback) {

		public readonly string Text = text;
		public readonly string Title = title;
		public readonly Action Callback = callback;

	}

	readonly Queue<AnnounceData> queuedAnnouncements = new();

	public void Announce(string text, Action callback = null, string title = "Announcement") {
		var data = new AnnounceData(text, title, callback);
		if (AnnouncementActive) queuedAnnouncements.Enqueue(data);
		else Announce(data);
	}

	void Announce(AnnounceData announceData) {
		announcementText.Text = announceData.Text;
		announcementTitle.Text = announceData.Title;
		announcementParent.Show();
		if (announceData.Callback != null) {
			announcementOkayButton.Connect(Button.SignalName.Pressed, Callable.From(announceData.Callback), (uint)ConnectFlags.OneShot);
		}
		if (!GameMan.IsPaused) {
			GameMan.TogglePause();
			_announcePaused = true;
		}
	}

	void HideAnnounce() {
		announcementParent.Hide();
		if (_announcePaused) {
			GameMan.TogglePause();
			_announcePaused = false;
		}
	}

	void AnnouncementOkayPressed() {
		HideAnnounce();
		if (queuedAnnouncements.Count != 0) Announce(queuedAnnouncements.Dequeue());
	}

	// display

	public void SetupResourceDisplay() {
		var fac = GetFactionActions().Faction;
		resourceDisplay.Display(c => (c as Label).Text = "faction: " + fac.Name);
		resourceDisplay.Display(c => {
			float monthlyChange = fac.Population.GetApprovalMonthlyChange();
			(c as ApprovalMeter).Display(fac.Population.Approval, monthlyChange);
		},
			GD.Load<PackedScene>("res://scenes/region/ui/approval_meter.tscn").Instantiate<ApprovalMeter>(),
			() => {
				var sb = new StringBuilder();
				sb.Append("your approval rate. when it drops to zero, you lose. change in approval is per month.\n");
				var reasons = fac.Population.GetApprovalMonthlyChangeReasons();
				foreach (var r in reasons) {
					if (r.Item1 == 0f) continue;
					sb.Append(r.Item2).Append('\t').Append($"{(r.Item1 >= 0f ? "+" : "")}{r.Item1 * 100:0}%\n");
				}
				return sb.ToString();
			});
		resourceDisplay.Display(c => {
			if (fac.GetPopulationCount() == 0) (c as Label).Text = "no one lives here.";
			(c as Label).Text = $"population: {fac.GetPopulationCount()} "
				+ $"({fac.HomelessPopulation} homeless, "
				+ $"{fac.Population.EmployedCount} employed)"
			;
		}, () => {
			float birthincreaseperminute = (fac.Population.GetYearlyBirths()) / GameTime.Years(1);
			float progresstogrow = 1f - fac.Population.OngrowingPopulation;
			float minutesbeforebirth = progresstogrow / birthincreaseperminute;
			string timetext = GameTime.GetFancyTimeString((TimeT)minutesbeforebirth);
			return "house and feed your people.\n"
				+ $"housing available: {fac.Population.MaxHousing} ({fac.Population.MaxHousing - fac.Population.HousedCount} spots free)\n"
				+ $"new person born in {timetext} ({fac.Population.GetYearlyBirths():0} births/year)"
			;
		});
		resourceDisplay.Display(c => {
			var sb = new StringBuilder();
			var (food, foodUsage) = GetFoodAndUsage();
			sb.Append($"food: {(int)food}");
			sb.Append($" ({(int)foodUsage} eaten/day)");

			(c as Label).Text = sb.ToString();
			var modulate = c.Modulate;
			if (food <= 0 && modulate != Palette.BrownRust) {
				c.Modulate = Palette.BrownRust;
			} else if (food < foodUsage * 0.5 && modulate != Palette.Chardonnay) {
				c.Modulate = Palette.Chardonnay;
			} else if (modulate != Colors.White) {
				c.Modulate = Colors.White;
			}
		},
		() => {
			var (food, foodUsage) = GetFoodAndUsage();
			var leftForDays = food / foodUsage;
			return $"(enough for {GameTime.GetFancyTimeString((TimeT)(leftForDays * GameTime.HOURS_PER_DAY * GameTime.MINUTES_PER_HOUR))})\n";
		});
		if (!fac.IsWild) {
			var reg = fac.Region;

			resourceDisplay.Display(c => {
				(c as Label).Text = $"    silver: {fac.LiquidSilver}";
			}, () => {
				float liq = fac.LiquidSilver;
				float tot = GameMan.Game.Map.TotalSilver;
				return $"{fac.Silver} free\n{liq - fac.Silver} in trade\n{(int)(liq / tot * 100)}% of world silver owned";
			});
			resourceDisplay.Display(c => {
				string txt = "";
				if (reg.GetGroundTile(inRegionTilepos, out GroundTileType tile)) {
					txt += $" {tile.UIString()}";
					if (reg.HasMapObject(inRegionTilepos, out MapObject mopject)) {
						txt += $" with {(mopject.Type as IAssetType).AssetName}";
					}
					txt += $" {inRegionTilepos}";
				} else if (GameMan.Game.Map.TileOwners.TryGetValue(inRegionTilepos + reg.WorldPosition, out var reg2)) {
					bool knowsreg = reg.Neighbors.Contains(reg2);
					txt = $"over region {(knowsreg ? reg2.LocalFaction.Name : "?")} {inRegionTilepos + reg.WorldPosition - reg2.WorldPosition}";
				}
				(c as Label).Text = txt;
			});
			resourceDisplay.Display(c => (c as Label).Text = "military: " + fac.Military);
		} else {
			resourceDisplay.Display(c => (c as Label).Text = fac.Name);
		}
		resourceDisplay.DisplayFat();
		var timeLabel = new Label {
			HorizontalAlignment = HorizontalAlignment.Right,
			LabelSettings = ResourceDisplay.DefaultLabelSettings,
		};
		resourceDisplay.Display(c => (c as Label).Text = GetTimeString(), timeLabel);
	}

	void UpdateDisplays() {
		resourceDisplay.Display();
		DisplayResources();
		controlButtons.UpdateDisplays();
		bool gamePaused = GameMan.IsPaused || GameMan.GameSpeed == 0f;
		pauseDisplayPanel.Visible = gamePaused;
		if (state == State.PlacingBuild) {
			buildingList.UpdateCursorWhilePlacing(Camera.GetMouseHoveredTilePos());
		}
	}

	void DisplayResources() {
		var sb = new StringBuilder();
		if (GameIsOver) {
			resourceLabel.Text = sb.ToString();
			return;
		}
		var resources = GetResourcesEvent?.Invoke();
		if (resources == null) return;
		foreach (var p in resources) {
			sb.Append($"{p.Key.ToString()} x {p.Value.Amount}\n");
		}
		sb.Append($"\ntotal {resources.ItemAmount}");
		resourceLabel.Text = sb.ToString();
	}

	// utilities

	void Escape() {
		switch (state) {
			case State.PlacingBuild:
			case State.ChoosingBuild:
			case State.MapObjectMenu:
			case State.AgreementsMenu:
			case State.JobsMenu:
			case State.TradeMenu:
			case State.WorldMenu:
			case State.WarMenu:
				state = State.Idle;
				break;
		}
	}

	public void HourlyUpdate(TimeT timeInMinutes) {
		if (menuTabs.CurrentTab == (int)Tab.Documents) {
			documentsDisplay.Display();
		}
	}

	public void OnLeftMouseClick(Vector2 position, Vector2I tilePosition) {
		switch (state) {
			case State.PlacingBuild:
				if (RequestBuild(buildingList.SelectedBuildingType, tilePosition) && !Input.IsKeyPressed(Key.Alt)) {
					state = State.Idle;
				}
				break;
			case State.Idle:
				MapClick(tilePosition);
				break;
			// case State.MapObjectMenu:
			// 	state = State.Idle;
			// 	MapClick(tilePosition);
			// 	break;
			case State.WarMenu:
				warInfoPanel.Click(tilePosition + GetFactionActions().Region.WorldPosition);
				break;
			default:
				state = State.Idle;
				MapClick(tilePosition);
				break;
		}
	}

	public void OnRightMouseClick(Vector2 position, Vector2I tilePosition) { }

	Vector2I inRegionTilepos;
	public void OnTileHighlighted(Vector2I tilePosition, Region region) {
		inRegionTilepos = tilePosition;
		//resourceDisplay.Display(inRegionTilepos: (tilePosition, region));
	}

	public void OnBuildingClicked(Building building) {
		Debug.Assert(state == State.Idle, "Can't click on buildings outside of idle state");
		state = State.MapObjectMenu;
		mopjectMenu.Open(building);
	}

	public void OnResourceSiteClicked(ResourceSite resourceSite) {
		Debug.Assert(resourceSite is not null);
		Debug.Assert(state == State.Idle, "Can't click on resourceSite outside of idle state");
		state = State.MapObjectMenu;
		mopjectMenu.Open(resourceSite);
	}

	public void OnProblemClicked(Problem problem) {
		Debug.Assert(state == State.Idle, "Can't click on problem outside of idle state");
		state = State.MapObjectMenu;
		mopjectMenu.Open(problem);
	}

	public void OnAttackedTileClicked(Vector2I tile) {
		Debug.Assert(state == State.Idle, "Can't click on attacking outside of idle state");
		state = State.WarMenu;
		SelectTab(Tab.War);
		warInfoPanel.Display(GetFactionActions().Faction);
		warInfoPanel.Click(tile + GetFactionActions().Region.WorldPosition);
	}

	bool wasPausedBefore = false;
	void OnStateChanged(State old, State current) {
		if (old != current) {
			if (old == State.ChoosingBuild) {
				buildingList.Reset();
				SelectTab(Tab.None);
				controlButtons.SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
			}
			if (old == State.PlacingBuild) {
				buildingList.Reset();
				buildingList.SetBuildCursor(null);
				controlButtons.SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
			}
			if (old == State.MapObjectMenu) {
				mopjectMenu.Close();
				controlButtons.SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
				TileDeselectedEvent?.Invoke();
			}
			if (old == State.AgreementsMenu || old == State.JobsMenu || old == State.TradeMenu || old == State.WorldMenu) {
				SelectTab(Tab.None);
				controlButtons.SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
			}
			if (old == State.WarMenu) {
				SelectTab(Tab.None);
				controlButtons.SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
				warInfoPanel.Undisplay();
			}
		}
		if (current == State.ChoosingBuild
			|| current == State.MapObjectMenu
			|| current == State.AgreementsMenu
			|| current == State.JobsMenu
			|| current == State.TradeMenu
			|| current == State.WorldMenu
			|| current == State.WarMenu
		) {
			controlButtons.SetTimeSpeedAlteringAllowed(false);
			if (!GameMan.IsPaused) {
				GameMan.TogglePause();
				wasPausedBefore = false;
			} else wasPausedBefore = true;
		}
	}

	void Reset() {
		state = State.Idle;
		menuTabs.CurrentTab = -1;
		buildingList.Reset();
		UpdateDisplays();
		announcementParent.Hide();
	}

	public void GameOver() {
		GameIsOver = true;
	}

	public void MapClick(Vector2I tile) => MapClickEvent?.Invoke(tile);

	public bool RequestBuild(IBuildingType a, Vector2I b) => RequestBuildEvent?.Invoke(a, b) ?? false;
	public bool GetHasBuildingMaterials(IBuildingType btype) => GetHasBuildingMaterialsEvent?.Invoke(btype) ?? false;
	public bool GetCanBuild(IBuildingType btype, Vector2I pos) => GetCanBuildEvent?.Invoke(btype, pos) ?? false;
	public List<BuildingType> GetBuildingTypes() => GetBuildingTypesEvent?.Invoke();
	public ResourceStorage GetResources() => GetResourcesEvent?.Invoke();
	public FactionActions GetFactionActions() => GetFactionActionsEvent?.Invoke();
	public (float Food, float Usage) GetFoodAndUsage() => GetFoodAndUsageEvent?.Invoke() ?? (1337, 1337);
	public void TileSelected(Vector2I place) => TileSelectedEvent?.Invoke(place);

	public Job GetMapObjectJob(MapObject mapObject) => GetMapObjectJobEvent?.Invoke(mapObject);
	public IEnumerable<Job> GetJobs() => GetJobsEvent?.Invoke();
	public void AddJobRequested(MapObject mapObject, MapObjectJob job) => AddJobRequestedEvent?.Invoke(mapObject, job);
	public void AddJobRequested(Problem problem, SolveProblemJob job) => AddProblemJobRequestedEvent?.Invoke(problem, job);
	public void AddJobRequested(Job job) => throw new NotImplementedException("Cant add jobs without building yet");
	public uint GetMaxFreeWorkers() {
		var val = GetMaxFreeWorkersEvent?.Invoke();
		if (val == null) Debug.Assert(false, "ungood Connection");
		return val ?? 0;
	}

	public void ChangeJobWorkerCount(Job job, int amount) => ChangeJobWorkerCountEvent?.Invoke(job, amount);
	public void RemoveJob(Job job) => RemoveJobEvent?.Invoke(job);

	public Briefcase GetBriefcase() => GetBriefcaseEvent?.Invoke();

	public string GetTimeString() => GetTimeStringEvent?.Invoke() ?? "NEVER";


}
