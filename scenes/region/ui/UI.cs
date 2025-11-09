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

		public event Func<MapObject, ICollection<JobBox>> GetMapObjectJobsEvent;
		public event Func<ICollection<JobBox>> GetJobsEvent;
		public event Action<MapObject, MapObjectJob> AddJobRequestedEvent;
		public event Func<int> GetMaxFreeWorkersEvent;
		public event Action<JobBox, int> ChangeJobWorkerCountEvent;
		public event Action<JobBox> DeleteJobEvent;

		public event Func<Briefcase> GetBriefcaseEvent;

		public event Func<int> GetPopulationCountEvent;
		public event Func<int> GetHomelessPopulationCountEvent;
		public event Func<int> GetUnemployedPopulationCountEvent;
		public event Func<string> GetTimeStringEvent;
		public event Func<List<BuildingType>> GetBuildingTypesEvent;

		public event Func<bool> PauseRequestedEvent;
		public event Action<float> GameSpeedChangeRequestedEvent;

		public enum State {
			IDLE,
			CHOOSING_BUILD,
			PLACING_BUILD,
			JOBS_MENU,
			AGREEMENTS_MENU,
		}

		public enum Tab : int {
			NONE = -1, // because TabContainer -1 = none selected
			BUILD,
			DOCUMENTS,
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
		[Export] public JobsList jobsList;

		// top bar
		[Export] public Panel pauseDisplayPanel;
		[Export] public Label populationLabel;
		[Export] public Label timeLabel;
		[Export] public Label fpsLabel; // debug
		[Export] public Label tilePosLabel; // debug

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
			buildButton.Pressed += () => OnTabButtonPressed(Tab.BUILD, State.CHOOSING_BUILD);
			agreementsButton.Pressed += () => OnTabButtonPressed(Tab.DOCUMENTS, State.AGREEMENTS_MENU);

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
				state = State.IDLE;
				SelectTab(Tab.NONE);
			}
		}

		void OnPauseButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			gamePaused = PauseRequested();
			SetGameSpeedLabelText();
		}

		void OnNormalSpeedButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			GameSpeedChangeRequested(1);
			gameSpeed = 1;
			SetGameSpeedLabelText();
		}

		void OnFastSpeedButtonPressed() {
			if (timeSpeedAlteringDisabled) return;
			GameSpeedChangeRequested(3);
			gameSpeed = 3;
			SetGameSpeedLabelText();
		}

		void SetGameSpeedLabelText() => gameSpeedLabel.Text = gamePaused || internalGamePaused ? "paused" : $"{gameSpeed}x game speed";

		// menu activites

		public void SelectTab(Tab which) {
			if (which == Tab.NONE) {
				// reset some things
				buildingList.Reset();
			} else if (which == Tab.BUILD) {
				buildingList.Update();
				buildingList.Show();
			} else if (which == Tab.DOCUMENTS) {
				documentsDisplay.Display(GetBriefcase());
			}
			menuTabs.CurrentTab = (int)which;
		}

		// display

		void UpdateDisplays() {
			fpsLabel.Text = "fps: " + Engine.GetFramesPerSecond().ToString();
			populationLabel.Text = $"pop: {GetPopulationCount()} ({GetHomelessPopulationCount()} homeless, {GetUnemployedPopulationCount()} unemployed)";
			DisplayResources();
			SetGameSpeedLabelText();
			timeLabel.Text = GetTimeString();
			pauseDisplayPanel.Visible = gamePaused || gameSpeed == 0f || internalGamePaused;
		}

		void DisplayResources() {
			resourceLabel.Text = "";
			var resources = GetResourcesEvent?.Invoke();
			foreach (var p in resources) {
				resourceLabel.AppendText($"{p.Key.Name} x {p.Value.Amount}\n");
			}
			resourceLabel.AppendText($"\ntotal {resources.ItemAmount}/{resources.ItemCapacity}");
		}

		// utilities

		public void HourlyUpdate(TimeT timeInMinutes) {
			if (menuTabs.CurrentTab == (int)Tab.DOCUMENTS) {
				documentsDisplay.Display();
			}
		}

		public void OnLeftMouseClick(Vector2 position, Vector2I tilePosition) {
			switch (state) {
				case State.PLACING_BUILD:
					buildingList.RequestBuild(tilePosition);
					break;
				case State.IDLE:
					MapClick(tilePosition);
					break;
				default:
					break;
			}
		}

		public void OnRightMouseClick(Vector2 position, Vector2I tilePosition) {
			if (state == State.PLACING_BUILD || state == State.CHOOSING_BUILD) {
				state = State.IDLE;
			}
			if (state == State.JOBS_MENU) {
				state = State.IDLE;
			}
		}

		public void OnTileHighlighted(Vector2I tilePosition, Region region) {
			tilePosLabel.Text = tilePosition.ToString();
		}

		public void OnBuildingClicked(Building building) {
			Debug.Assert(state == State.IDLE, "Can't click on buildings outside of idle state");
			state = State.JOBS_MENU;
			jobsList.Open(building);
		}

		public void OnResourceSiteClicked(ResourceSite resourceSite) {
			Debug.Assert(state == State.IDLE, "Can't click on resourceSite outside of idle state");
			state = State.JOBS_MENU;
			jobsList.Open(resourceSite);
		}

		void OnStateChanged(State old, State current) {
			if (old != current) {
				if (old == State.CHOOSING_BUILD) {
					buildingList.Reset();
					SelectTab(Tab.NONE);
				}
				if (old == State.PLACING_BUILD) {
					buildingList.Reset();
					buildingList.SetBuildCursor(null);
				}
				if (old == State.JOBS_MENU) {
					jobsList.Close();
					SetTimeSpeedAltering(true);
					if (!gamePaused) internalGamePaused = PauseRequested();
				}
			}
			if (current == State.JOBS_MENU) {
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
			state = State.IDLE;
			menuTabs.CurrentTab = -1;
			buildingList.Reset();
		}

		public void MapClick(Vector2I tile) => MapClickEvent?.Invoke(tile);

		public void RequestBuild(IBuildingType a, Vector2I b) => RequestBuildEvent?.Invoke(a, b);
		public bool GetCanBuild(IBuildingType btype) => GetCanBuildEvent?.Invoke(btype) ?? false;
		public List<BuildingType> GetBuildingTypes() => GetBuildingTypesEvent?.Invoke();
		public ResourceStorage GetResources() => GetResourcesEvent?.Invoke();

		public ICollection<JobBox> GetMapObjectJobs(MapObject mapObject) => GetMapObjectJobsEvent?.Invoke(mapObject);
		public ICollection<JobBox> GetJobs() => GetJobsEvent?.Invoke();
		public void AddJobRequested(MapObject mapObject, MapObjectJob job) => AddJobRequestedEvent?.Invoke(mapObject, job);
		public void AddJobRequested(Job job) => throw new NotImplementedException("Cant add jobs without building yet");
		public int GetMaxFreeWorkers() => GetMaxFreeWorkersEvent?.Invoke() ?? -1;
		public void ChangeJobWorkerCount(JobBox job, int amount) => ChangeJobWorkerCountEvent?.Invoke(job, amount);
		public void DeleteJob(JobBox jobBox) => DeleteJobEvent?.Invoke(jobBox);

		public Briefcase GetBriefcase() => GetBriefcaseEvent?.Invoke();

		public int GetPopulationCount() => GetPopulationCountEvent?.Invoke() ?? -1;
		public int GetHomelessPopulationCount() => GetHomelessPopulationCountEvent?.Invoke() ?? -1;
		public int GetUnemployedPopulationCount() => GetUnemployedPopulationCountEvent?.Invoke() ?? -1;
		public string GetTimeString() => GetTimeStringEvent?.Invoke() ?? "NEVER";

		public bool PauseRequested() => PauseRequestedEvent?.Invoke() ?? false;
		public void GameSpeedChangeRequested(float spd) => GameSpeedChangeRequestedEvent?.Invoke(spd);


	}

}
