using Godot;
using scenes.region.buildings;
using System;
using System.Collections.Generic;
using IBuildingType = Building.IBuildingType;


namespace scenes.region.ui {

	public partial class UI : Control {

		// one big script to rule all region ui interactions

		public event Action<IBuildingType, Vector2I> BuildRequestedEvent;
		public event Func<int> GetPopulationCountEvent;
		public event Func<int> GetHomelessPopulationCountEvent;
		public event Func<List<BuildingType>> GetBuildingTypesEvent;
		public event Func<ResourceStorage> GetResourcesEvent;

		public enum State {
			IDLE,
			CHOOSING_BUILD,
			PLACING_BUILD,
		}

		public enum Tab : int {
			NONE = -1,
			BUILD,
		}

		// camera
		[Export] public Node2D CameraCursor;

		// bottom bar buttons
		[Export] public Button buildButton;
		[Export] public Button policyButton;
		[Export] public Button worldButton;

		// bottom bar menus menus
		[Export] public TabContainer menuTabs;
		[Export] public BuildingList buildingList;

		// top bar
		[Export] public Label populationLabel;
		[Export] public Label fpsLabel; // debug
		[Export] public Label tilePosLabel; // debug

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

		// overrides and connections

		public override void _Ready() {
			buildButton.Pressed += OnBuildButtonPressed;
			Reset();
		}

		public override void _Process(double delta) {
			fpsLabel.Text = "fps: " + Engine.GetFramesPerSecond().ToString();
			populationLabel.Text = $"pop: {GetPopulationCountEvent?.Invoke() ?? -2} ({GetHomelessPopulationCountEvent?.Invoke() ?? -2} homeless)";
			DisplayResources();
		}

		void OnBuildButtonPressed() {
			if (menuTabs.CurrentTab != (int)Tab.BUILD) {
				state = State.CHOOSING_BUILD;
				SelectTab(Tab.BUILD);
			} else {
				state = State.IDLE;
				SelectTab(Tab.NONE);
			}
		}

		// menu activites

		public void SelectTab(Tab which) {
			if (which == Tab.NONE) {
				// reset some things
				buildingList.Reset();
			} else if (which == Tab.BUILD) {
				buildingList.Update();
			}
			menuTabs.CurrentTab = (int)which;
		}

		// display

		void DisplayResources() {
			resourceLabel.Text = "";
			var resources = GetResourcesEvent?.Invoke();
			foreach (var p in resources) {
				resourceLabel.AppendText($"{p.Key.Name} x {p.Value.Amount}/{p.Value.Capacity}\n");
			}
		}

		// utilities

		public void OnLeftMouseClick(Vector2 position, Vector2I tilePosition) {
			switch (state) {
				case State.PLACING_BUILD:
					buildingList.PlacingBuild(tilePosition);
					break;
				default:
					break;
			}
		}

		public void OnRightMouseClick(Vector2 position, Vector2I tilePosition) {
			if (state == State.PLACING_BUILD) {
				state = State.IDLE;
			}
		}

		public void OnTileHighlighted(Vector2I tilePosition, Region region) {
			tilePosLabel.Text = tilePosition.ToString();
		}

		public void OnBuildingClicked(BuildingView buildingView) {
			if (state != State.IDLE) return;
		}

		void OnStateChanged(State old, State current) {
			if (old != current) {
				if (old == State.PLACING_BUILD) {
					buildingList.Reset();
					buildingList.SetBuildCursor(null);
				}
			}
		}

		void Reset() {
			state = State.IDLE;
			menuTabs.CurrentTab = -1;
			buildingList.Reset();
		}

		public void BuildRequested(IBuildingType a, Vector2I b) => BuildRequestedEvent?.Invoke(a, b);
		public int GetPopulationCount() => GetPopulationCountEvent?.Invoke() ?? -1;
		public int GetHomelessPopulationCount() => GetHomelessPopulationCountEvent?.Invoke() ?? -1;
		public List<BuildingType> GetBuildingTypes() => GetBuildingTypesEvent?.Invoke();
		public ResourceStorage GetResources() => GetResourcesEvent?.Invoke();
	}

}
