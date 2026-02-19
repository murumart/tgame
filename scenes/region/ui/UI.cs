using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using resources.game.building_types;
using scenes.autoload;
using static Document;
using IBuildingType = Building.IBuildingType;


namespace scenes.region.ui {

	public partial class UI : Control {

		// one big script to rule all region ui interactions

		public event Action<Vector2I> MapClickEvent;

		public event Action<IBuildingType, Vector2I> RequestBuildEvent;
		public event Func<IBuildingType, bool> GetCanBuildEvent;
		public event Func<ResourceStorage> GetResourcesEvent;
		public event Func<Faction> GetFactionEvent;
		public event Func<(float, float)> GetFoodAndUsageEvent;
		public event Action<Vector2I> TileSelectedEvent;
		public event Action TileDeselectedEvent;

		public event Func<MapObject, Job> GetMapObjectJobEvent;
		public event Func<ICollection<Job>> GetJobsEvent;
		public event Action<MapObject, MapObjectJob> AddJobRequestedEvent;
		public event Func<uint> GetMaxFreeWorkersEvent;
		public event Action<Job, int> ChangeJobWorkerCountEvent;
		public event Action<Job> DeleteJobEvent;

		public event Func<Briefcase> GetBriefcaseEvent;

		public event Func<string> GetTimeStringEvent;
		public event Func<List<BuildingType>> GetBuildingTypesEvent;

		public enum State {
			Idle,
			ChoosingBuild,
			PlacingBuild,
			MapObjectMenu,
			AgreementsMenu,
		}

		public enum Tab : int {
			None = -1, // because TabContainer -1 = none selected
			Build,
			Documents,
		}

		public bool GameIsOver { get; private set; }

		// camera
		[Export] public RegionCamera Camera;

		// bottom bar buttons
		[Export] public Button buildButton;
		[Export] public Button agreementsButton;
		[Export] public Button jobsButton;

		// bottom bar menus menus
		[Export] public TabContainer menuTabs;
		[Export] public BuildingList buildingList;
		[Export] public DocumentsDisplay documentsDisplay;

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

			pauseButton.Pressed += OnPauseButtonPressed;
			normalSpeedButton.Pressed += OnNormalSpeedButtonPressed;
			fastSpeedButton.Pressed += OnFastSpeedButtonPressed;
			fasterSpeedButton.Pressed += OnFasterSpeedButtonPressed;

			zoomInButton.Pressed += () => Camera.ZoomIn();
			zoomOutButton.Pressed += () => Camera.ZoomOut();
			zoomResetButton.Pressed += () => Camera.ZoomReset();

			panButton.ButtonDown += () => {
				Camera.StartDragging();
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

		void OnPauseButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			GameMan.Singleton.TogglePause();
			SetGameSpeedLabelText();
		}

		const int NORMAL_SPEED = 1;
		const int FAST_SPEED = 10;
		const int FASTER_SPEED = 30;

		void OnNormalSpeedButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			GameMan.Singleton.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, NORMAL_SPEED);
			SetGameSpeedLabelText();
		}

		void OnFastSpeedButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			GameMan.Singleton.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, FAST_SPEED);
			SetGameSpeedLabelText();
		}

		void OnFasterSpeedButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			GameMan.Singleton.MultiplyGameSpeed(GameMan.GameSpeedChanger.UI, FASTER_SPEED);
			SetGameSpeedLabelText();
		}

		void SetGameSpeedLabelText() => gameSpeedLabel.Text = GameMan.Singleton.IsPaused ? "paused" : $"{GameMan.Singleton.GameSpeed}x game speed";

		// menu activites

		public void SelectTab(Tab which) {
			if (which == Tab.None) {
				// reset some things
				buildingList.Reset();
			} else if (which == Tab.Build) {
				buildingList.Update();
				buildingList.Show();
			} else if (which == Tab.Documents) {
				documentsDisplay.Display(GetBriefcase());
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
			if (!GameMan.Singleton.IsPaused) {
				GameMan.Singleton.TogglePause();
				_announcePaused = true;
			}
		}

		void HideAnnounce() {
			announcementParent.Hide();
			if (_announcePaused) {
				GameMan.Singleton.TogglePause();
				_announcePaused = false;
			}
		}

		void AnnouncementOkayPressed() {
			HideAnnounce();
			if (queuedAnnouncements.Count != 0) Announce(queuedAnnouncements.Dequeue());
		}

		// display

		public void SetupResourceDisplay() {
			resourceDisplay.Display(() => $"fps: {Engine.GetFramesPerSecond()}");
			var fac = GetFaction();
			resourceDisplay.Display(() => {
				if (fac.GetPopulationCount() == 0) return "no one lives here.";
				return $"population: {fac.GetPopulationCount()} "
					+ $"({fac.HomelessPopulation / (float)fac.Population.Count * 100:0}% homeless, "
					+ $"{fac.Population.EmployedCount / (float)fac.Population.Count * 100:0}% employed, "
					+ $"{fac.Population.GetYearlyBirths():0} births/year)";
			}, () => {
				return "house and feed your people to make sure a new generation will be born.";
			});
			if (!fac.IsWild) {
				var reg = fac.Region;
				resourceDisplay.Display(() => {
					float monthlyChange = fac.Population.GetApprovalMonthlyChange();
					return $"approval: {fac.Population.Approval * 100:0}% ({(monthlyChange >= 0f ? "+" : "")}{monthlyChange * 100:0}%/month)";
				}, () => {
					var sb = new StringBuilder();
					sb.Append("your approval rate depends on the state of your ruling. when it drops to zero, you lose. ensure prosperity for your people.\n");
					var reasons = fac.Population.GetApprovalMonthlyChangeReasons();
					foreach (var r in reasons) {
						if (r.Item1 == 0f) continue;
						sb.Append(r.Item2).Append('\t').Append($"{(r.Item1 >= 0f ? "+" : "")}{r.Item1 * 100:0}%\n");
					}
					return sb.ToString();
				});
				resourceDisplay.Display(() => {
					return $"silver: {fac.Silver}";
				});
				resourceDisplay.Display(() => {
					string txt = $"{inRegionTilepos}";
					if (reg.GroundTiles.TryGetValue(inRegionTilepos, out GroundTileType tile)) {
						txt += $" {tile.UIString()}";
						if (reg.HasMapObject(inRegionTilepos, out MapObject mopject)) {
							txt += $" with {(mopject.Type as IAssetType).AssetName}";
						}
					}
					return txt;
				});
				resourceDisplay.Display(() => $"faction: {fac.Name}");
			} else {
				resourceDisplay.Display(() => fac.Name);
			}
			resourceDisplay.DisplayFat();
			var timeLabel = new Label {HorizontalAlignment = HorizontalAlignment.Right};
			resourceDisplay.Display(() => GetTimeString(), timeLabel);
		}

		void UpdateDisplays() {
			resourceDisplay.Display();
			DisplayResources();
			SetGameSpeedLabelText();
			bool gamePaused = GameMan.Singleton.IsPaused || GameMan.Singleton.GameSpeed == 0f;
			pauseDisplayPanel.Visible = gamePaused;
			zoomLabel.Text = $"zoom: {(Camera.Zoom.X >= 1 ? Camera.Zoom.X : Mathf.Remap(Camera.Zoom.X, 1f, 0.1f, 1f, -8f)):0}";
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
			if (state == State.PlacingBuild || state == State.ChoosingBuild) {
				state = State.Idle;
			}
			if (state == State.MapObjectMenu) {
				state = State.Idle;
			}
			if (state == State.AgreementsMenu) {
				state = State.Idle;
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
					RequestBuild(buildingList.SelectedBuildingType, tilePosition);
					break;
				case State.Idle:
					MapClick(tilePosition);
					break;
				default:
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
			Debug.Assert(state == State.Idle, "Can't click on resourceSite outside of idle state");
			state = State.MapObjectMenu;
			mopjectMenu.Open(resourceSite);
		}

		bool wasPausedBefore = false;
		void OnStateChanged(State old, State current) {
			if (old != current) {
				if (old == State.ChoosingBuild) {
					buildingList.Reset();
					SelectTab(Tab.None);
					SetTimeSpeedAlteringAllowed(true);
					if (GameMan.Singleton.IsPaused && !wasPausedBefore) GameMan.Singleton.TogglePause();
				}
				if (old == State.PlacingBuild) {
					buildingList.Reset();
					buildingList.SetBuildCursor(null);
					SetTimeSpeedAlteringAllowed(true);
					if (GameMan.Singleton.IsPaused && !wasPausedBefore) GameMan.Singleton.TogglePause();
				}
				if (old == State.MapObjectMenu) {
					mopjectMenu.Close();
					SetTimeSpeedAlteringAllowed(true);
					if (GameMan.Singleton.IsPaused && !wasPausedBefore) GameMan.Singleton.TogglePause();
					TileDeselectedEvent?.Invoke();
				}
				if (old == State.AgreementsMenu) {
					SelectTab(Tab.None);
					SetTimeSpeedAlteringAllowed(true);
					if (GameMan.Singleton.IsPaused && !wasPausedBefore) GameMan.Singleton.TogglePause();
				}
			}
			if (current == State.ChoosingBuild) {
				SetTimeSpeedAlteringAllowed(false);
				if (!GameMan.Singleton.IsPaused) {
					GameMan.Singleton.TogglePause();
					wasPausedBefore = false;
				} else wasPausedBefore = true;
			}
			if (current == State.MapObjectMenu) {
				SetTimeSpeedAlteringAllowed(false);
				if (!GameMan.Singleton.IsPaused) {
					GameMan.Singleton.TogglePause();
					wasPausedBefore = false;
				} else wasPausedBefore = true;
			}
			if (current == State.AgreementsMenu) {
				SetTimeSpeedAlteringAllowed(false);
				if (!GameMan.Singleton.IsPaused) {
					GameMan.Singleton.TogglePause();
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

		public void RequestBuild(IBuildingType a, Vector2I b) => RequestBuildEvent?.Invoke(a, b);
		public bool GetCanBuild(IBuildingType btype) => GetCanBuildEvent?.Invoke(btype) ?? false;
		public List<BuildingType> GetBuildingTypes() => GetBuildingTypesEvent?.Invoke();
		public ResourceStorage GetResources() => GetResourcesEvent?.Invoke();
		public Faction GetFaction() => GetFactionEvent?.Invoke();
		public (float, float) GetFoodAndUsage() => GetFoodAndUsageEvent?.Invoke() ?? (1337, 1337);
		public void TileSelected(Vector2I place) => TileSelectedEvent?.Invoke(place);

		public Job GetMapObjectJob(MapObject mapObject) => GetMapObjectJobEvent?.Invoke(mapObject);
		public ICollection<Job> GetJobs() => GetJobsEvent?.Invoke();
		public void AddJobRequested(MapObject mapObject, MapObjectJob job) => AddJobRequestedEvent?.Invoke(mapObject, job);
		public void AddJobRequested(Job job) => throw new NotImplementedException("Cant add jobs without building yet");
		public uint GetMaxFreeWorkers() {
			var val = GetMaxFreeWorkersEvent?.Invoke();
			if (val == null) Debug.Assert(false, "ungood Connection");
			return val ?? 0;
		}

		public void ChangeJobWorkerCount(Job job, int amount) => ChangeJobWorkerCountEvent?.Invoke(job, amount);
		public void DeleteJob(Job job) => DeleteJobEvent?.Invoke(job);

		public Briefcase GetBriefcase() => GetBriefcaseEvent?.Invoke();

		public string GetTimeString() => GetTimeStringEvent?.Invoke() ?? "NEVER";

	}

}
