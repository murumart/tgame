using System;
using System.Collections.Generic;
using Godot;
using resources.game.building_types;
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
		public event Func<(uint, uint)> GetFoodAndUsageEvent;

		public event Func<MapObject, Job> GetMapObjectJobEvent;
		public event Func<ICollection<Job>> GetJobsEvent;
		public event Action<MapObject, MapObjectJob> AddJobRequestedEvent;
		public event Func<uint> GetMaxFreeWorkersEvent;
		public event Action<Job, int> ChangeJobWorkerCountEvent;
		public event Action<Job> DeleteJobEvent;

		public event Func<Briefcase> GetBriefcaseEvent;

		public event Func<string> GetTimeStringEvent;
		public event Func<List<BuildingType>> GetBuildingTypesEvent;

		public event Func<bool> PauseRequestedEvent;
		public event Action<float> GameSpeedChangeRequestedEvent;

		public enum State {
			Idle,
			ChoosingBuild,
			PlacingBuild,
			JobsMenu,
			AgreementsMenu,
		}

		public enum Tab : int {
			None = -1, // because TabContainer -1 = none selected
			Build,
			Documents,
		}

		// camera
		[Export] public Node2D CameraCursor;

		// bottom bar buttons
		[Export] public Button buildButton;
		[Export] public Button agreementsButton;
		[Export] public Button jobsButton;

		// bottom bar menus menus
		[Export] public TabContainer menuTabs;
		[Export] public BuildingList buildingList;
		[Export] public DocumentsDisplay documentsDisplay;

		// right
		[Export] public MapObjectMenu jobsList;

		// top bar
		[Export] ResourceDisplay resourceDisplay;
		[Export] public Panel pauseDisplayPanel;

		// top bar bottom
		[Export] public Label gameSpeedLabel;
		[Export] public Button pauseButton;
		[Export] public Button normalSpeedButton;
		[Export] public Button fastSpeedButton;

		[Export] public RichTextLabel resourceLabel;

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

		float gameSpeed = 1f;
		bool gamePaused = false;
		bool internalGamePaused = false;
		bool timeSpeedAlteringDisabled = false;


		// overrides and connections

		public override void _Ready() {
			buildButton.Pressed += () => OnTabButtonPressed(Tab.Build, State.ChoosingBuild);
			agreementsButton.Pressed += () => OnTabButtonPressed(Tab.Documents, State.AgreementsMenu);

			pauseButton.Pressed += OnPauseButtonPressed;
			normalSpeedButton.Pressed += OnNormalSpeedButtonPressed;
			fastSpeedButton.Pressed += OnFastSpeedButtonPressed;

			Reset();
		}

		public override void _Process(double delta) {
			UpdateDisplays(); // todo move this to something that doesn't happen every frame... if it becomes a bottleneck
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
			gamePaused = PauseRequested();
			SetGameSpeedLabelText();
		}

		const int NORMAL_SPEED = 1;
		const int FAST_SPEED = 15;

		void OnNormalSpeedButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			GameSpeedChangeRequested(NORMAL_SPEED);
			gameSpeed = NORMAL_SPEED;
			SetGameSpeedLabelText();
		}

		void OnFastSpeedButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			GameSpeedChangeRequested(FAST_SPEED);
			gameSpeed = FAST_SPEED;
			SetGameSpeedLabelText();
		}

		void SetGameSpeedLabelText() => gameSpeedLabel.Text = gamePaused || internalGamePaused ? "paused" : $"{gameSpeed}x game speed";

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

		// display

		void UpdateDisplays() {
			var faction = GetFaction();
			resourceDisplay.Display(population: faction.GetPopulationCount(), homelessPopulation: faction.HomelessPopulation, unemployedPopulation: faction.UnemployedPopulation);
			resourceDisplay.Display(timeString: GetTimeString(), faction: faction, foodAndUsage: GetFoodAndUsage());
			DisplayResources();
			SetGameSpeedLabelText();
			pauseDisplayPanel.Visible = gamePaused || gameSpeed == 0f || internalGamePaused;
		}

		void DisplayResources() {
			resourceLabel.Text = "";
			var resources = GetResourcesEvent?.Invoke();
			foreach (var p in resources) {
				resourceLabel.AppendText($"{p.Key.AssetName} x {p.Value.Amount}\n");
			}
			resourceLabel.AppendText($"\ntotal {resources.ItemAmount}");
		}

		// utilities

		public void HourlyUpdate(TimeT timeInMinutes) {
			if (menuTabs.CurrentTab == (int)Tab.Documents) {
				documentsDisplay.Display();
			}
		}

		public void OnLeftMouseClick(Vector2 position, Vector2I tilePosition) {
			switch (state) {
				case State.PlacingBuild:
					buildingList.RequestBuild(tilePosition);
					break;
				case State.Idle:
					MapClick(tilePosition);
					break;
				default:
					break;
			}
		}

		public void OnRightMouseClick(Vector2 position, Vector2I tilePosition) {
			if (state == State.PlacingBuild || state == State.ChoosingBuild) {
				state = State.Idle;
			}
			if (state == State.JobsMenu) {
				state = State.Idle;
			}
		}

		public void OnTileHighlighted(Vector2I tilePosition, Region region) {
			resourceDisplay.Display(inRegionTilepos: (tilePosition, region));
		}

		public void OnBuildingClicked(Building building) {
			Debug.Assert(state == State.Idle, "Can't click on buildings outside of idle state");
			state = State.JobsMenu;
			jobsList.Open(building);
		}

		public void OnResourceSiteClicked(ResourceSite resourceSite) {
			Debug.Assert(state == State.Idle, "Can't click on resourceSite outside of idle state");
			state = State.JobsMenu;
			jobsList.Open(resourceSite);
		}

		void OnStateChanged(State old, State current) {
			if (old != current) {
				if (old == State.ChoosingBuild) {
					buildingList.Reset();
					SelectTab(Tab.None);
				}
				if (old == State.PlacingBuild) {
					buildingList.Reset();
					buildingList.SetBuildCursor(null);
				}
				if (old == State.JobsMenu) {
					jobsList.Close();
					SetTimeSpeedAltering(true);
					if (!gamePaused) internalGamePaused = PauseRequested();
				}
			}
			if (current == State.JobsMenu) {
				SetTimeSpeedAltering(false);
				if (!gamePaused) internalGamePaused = PauseRequested();
			}
		}

		void SetTimeSpeedAltering(bool to) {
			timeSpeedAlteringDisabled = !to;
			pauseButton.Disabled = !to;
			fastSpeedButton.Disabled = !to;
			normalSpeedButton.Disabled = !to;
		}

		void Reset() {
			state = State.Idle;
			menuTabs.CurrentTab = -1;
			buildingList.Reset();
		}

		public void MapClick(Vector2I tile) => MapClickEvent?.Invoke(tile);

		public void RequestBuild(IBuildingType a, Vector2I b) => RequestBuildEvent?.Invoke(a, b);
		public bool GetCanBuild(IBuildingType btype) => GetCanBuildEvent?.Invoke(btype) ?? false;
		public List<BuildingType> GetBuildingTypes() => GetBuildingTypesEvent?.Invoke();
		public ResourceStorage GetResources() => GetResourcesEvent?.Invoke();
		public Faction GetFaction() => GetFactionEvent?.Invoke();
		public (uint, uint) GetFoodAndUsage() => GetFoodAndUsageEvent?.Invoke() ?? (1337, 1337);

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

		public bool PauseRequested() => PauseRequestedEvent?.Invoke() ?? false;
		public void GameSpeedChangeRequested(float spd) => GameSpeedChangeRequestedEvent?.Invoke(spd);


	}

}
