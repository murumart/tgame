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
			MapObjectMenu,
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

		public void SetupResourceDisplay() {
			var fac = GetFaction();
			resourceDisplay.Display(() => $"pop: {fac.GetPopulationCount()} ({fac.HomelessPopulation} homeless, {fac.UnemployedPopulation} unemployed)");
			resourceDisplay.Display(() => {
				var foodAndUsage = GetFoodAndUsage();
				return $"food: {foodAndUsage.Item1} (usage {foodAndUsage.Item2})";
			});
			resourceDisplay.Display(() => $"fps: {Engine.GetFramesPerSecond()}");
			var reg = fac.Region;
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
			resourceDisplay.DisplayFat();
			var timeLabel = new Label {HorizontalAlignment = HorizontalAlignment.Right};
			resourceDisplay.Display(() => GetTimeString(), timeLabel);
		}

		public override void _Process(double delta) {
			UpdateDisplays(); // todo move this to something that doesn't happen every frame... if it becomes a bottleneck
		}

		public override void _UnhandledInput(InputEvent @event) {
			base._UnhandledInput(@event);
			if (@event.IsActionPressed("escape")) {
				Escape();
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
			gamePaused = PauseRequested();
			SetGameSpeedLabelText();
		}

		const int NORMAL_SPEED = 1;
		const int FAST_SPEED = 30;

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
			resourceDisplay.Display();
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

		public void OnRightMouseClick(Vector2 position, Vector2I tilePosition) {

		}

		Vector2I inRegionTilepos;
		public void OnTileHighlighted(Vector2I tilePosition, Region region) {
			inRegionTilepos = tilePosition;
			//resourceDisplay.Display(inRegionTilepos: (tilePosition, region));
		}

		public void OnBuildingClicked(Building building) {
			Debug.Assert(state == State.Idle, "Can't click on buildings outside of idle state");
			state = State.MapObjectMenu;
			jobsList.Open(building);
		}

		public void OnResourceSiteClicked(ResourceSite resourceSite) {
			Debug.Assert(state == State.Idle, "Can't click on resourceSite outside of idle state");
			state = State.MapObjectMenu;
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
				if (old == State.MapObjectMenu) {
					jobsList.Close();
					SetTimeSpeedAltering(true);
					if (!gamePaused) internalGamePaused = PauseRequested();
				}
				if (old == State.AgreementsMenu) {
					SelectTab(Tab.None);
				}
			}
			if (current == State.MapObjectMenu) {
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
