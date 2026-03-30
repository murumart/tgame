using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using resources.game.building_types;
using scenes.autoload;
using scenes.map.ui;
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
	[Export] Button panButton;

	[Export] Label zoomLabel;
	[Export] Button zoomInButton;
	[Export] Button zoomResetButton;
	[Export] Button zoomOutButton;

	[Export] public Label gameSpeedLabel;
	[Export] public Button pauseButton;
	[Export] public Button normalSpeedButton;
	[Export] public Button fastSpeedButton;
	[Export] public Button fasterSpeedButton;

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

	bool timeSpeedAlteringDisabled = false;


	// overrides and connections

	bool cursedPanning = true;
	public override void _Ready() {
		buildButton.Pressed += () => OnTabButtonPressed(Tab.Build, State.ChoosingBuild);
		agreementsButton.Pressed += () => OnTabButtonPressed(Tab.Documents, State.AgreementsMenu);
		jobsButton.Pressed += () => OnTabButtonPressed(Tab.Jobs, State.JobsMenu);
		tradeButton.Pressed += () => OnTabButtonPressed(Tab.Trade, State.TradeMenu);
		worldButton.Pressed += () => OnTabButtonPressed(Tab.World, State.WorldMenu);
		warButton.Pressed += () => OnTabButtonPressed(Tab.War, State.WarMenu);

		pauseButton.Pressed += OnPauseButtonPressed;
		normalSpeedButton.Pressed += OnNormalSpeedButtonPressed;
		fastSpeedButton.Pressed += OnFastSpeedButtonPressed;
		fasterSpeedButton.Pressed += OnFasterSpeedButtonPressed;

		zoomInButton.Pressed += () => Camera.ZoomIn();
		zoomOutButton.Pressed += () => Camera.ZoomOut();
		zoomResetButton.Pressed += () => Camera.ZoomReset();

		panButton.ButtonDown += () => {
			Camera.StartDragging(true);
			cursedPanning = true;
			panButton.MouseFilter = MouseFilterEnum.Ignore;
			panButton.Disabled = true;
		};
		//panButton.ButtonUp += () => Camera.StopDragging();

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
		if (@event is InputEventMouseButton iemb && iemb.ButtonIndex == MouseButton.Left && iemb.IsReleased() && cursedPanning) {
			cursedPanning = false;
			Camera.StopDragging();
			panButton.Disabled = false;
			panButton.MouseFilter = MouseFilterEnum.Stop;
		}
		if (@event is InputEventKey k && k.Pressed) {
			if (k.Keycode == Key.Key6) {
				if (timeSpeedAlteringDisabled) return;
				GameMan.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, 1200);
				SetGameSpeedLabelText();
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

	void TogglePause() {
		if (timeSpeedAlteringDisabled) return;
		GameMan.TogglePause();
		SetGameSpeedLabelText();
	}

	void OnPauseButtonPressed() {
		TogglePause();
	}

	const int NORMAL_SPEED = 1;
	const int FAST_SPEED = 10;
	const int FASTER_SPEED = 30;

	void OnNormalSpeedButtonPressed() {
		if (timeSpeedAlteringDisabled) return;
		if (GameMan.IsPaused) TogglePause();
		GameMan.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, NORMAL_SPEED);
		SetGameSpeedLabelText();
	}

	void OnFastSpeedButtonPressed() {
		if (timeSpeedAlteringDisabled) return;
		if (GameMan.IsPaused) TogglePause();
		GameMan.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, FAST_SPEED);
		SetGameSpeedLabelText();
	}

	void OnFasterSpeedButtonPressed() {
		if (timeSpeedAlteringDisabled) return;
		if (GameMan.IsPaused) TogglePause();
		GameMan.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, FASTER_SPEED);
		SetGameSpeedLabelText();
	}

	void SetGameSpeedLabelText() => gameSpeedLabel.Text = GameMan.IsPaused ? "paused" : $"{GameMan.GameSpeed}x game speed";

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
		if (!fac.IsWild) {
			var reg = fac.Region;
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
				(c as Label).Text = $"    silver: {fac.Silver}    ";
			}, () => {
				float liq = fac.LiquidSilver;
				float tot = GameMan.Game.Map.TotalSilver;
				return $"{liq} total\n{(int)(liq / tot * 100)}% of world silver owned";
			});
			resourceDisplay.Display(c => {
				string txt = $"{inRegionTilepos}";
				if (reg.GetGroundTile(inRegionTilepos, out GroundTileType tile)) {
					txt += $" {tile.UIString()}";
					if (reg.HasMapObject(inRegionTilepos, out MapObject mopject)) {
						txt += $" with {(mopject.Type as IAssetType).AssetName}";
					}
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
		SetGameSpeedLabelText();
		bool gamePaused = GameMan.IsPaused || GameMan.GameSpeed == 0f;
		pauseDisplayPanel.Visible = gamePaused;
		zoomLabel.Text = $"zoom: {(Camera.Zoom.X):F1}";
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
			sb.Append($"{p.Key.AssetName} x {p.Value.Amount}\n");
		}
		var (food, foodUsage) = GetFoodAndUsage();
		sb.Append($"\nfood: {(int)food}\n");
		sb.Append($"({(int)foodUsage} eaten/day)\n");
		var leftForDays = food / foodUsage;
		sb.Append($"(enough for {GameTime.GetFancyTimeString((TimeT)(leftForDays * GameTime.HOURS_PER_DAY * GameTime.MINUTES_PER_HOUR))})\n");
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
				SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
			}
			if (old == State.PlacingBuild) {
				buildingList.Reset();
				buildingList.SetBuildCursor(null);
				SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
			}
			if (old == State.MapObjectMenu) {
				mopjectMenu.Close();
				SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
				TileDeselectedEvent?.Invoke();
			}
			if (old == State.AgreementsMenu || old == State.JobsMenu || old == State.TradeMenu || old == State.WorldMenu) {
				SelectTab(Tab.None);
				SetTimeSpeedAlteringAllowed(true);
				if (GameMan.IsPaused && !wasPausedBefore) GameMan.TogglePause();
			}
			if (old == State.WarMenu) {
				SelectTab(Tab.None);
				SetTimeSpeedAlteringAllowed(true);
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
			SetTimeSpeedAlteringAllowed(false);
			if (!GameMan.IsPaused) {
				GameMan.TogglePause();
				wasPausedBefore = false;
			} else wasPausedBefore = true;
		}
	}

	void SetTimeSpeedAlteringAllowed(bool to) {
		timeSpeedAlteringDisabled = !to;
		pauseButton.Disabled = !to;
		fastSpeedButton.Disabled = !to;
		fasterSpeedButton.Disabled = !to;
		normalSpeedButton.Disabled = !to;
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
