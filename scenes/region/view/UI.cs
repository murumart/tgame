using Godot;
using scenes.region.view.buildings;
using System;


namespace scenes.region.view {
	public partial class UI : Control {

		// one big script to rule all region ui interactions


		[Signal] public delegate void BuildTargetSetEventHandler(int target);
		[Signal] public delegate void BuildRequestedEventHandler(BuildingView building, Vector2I tilePosition);

		private enum State {
			IDLE,
			CHOOSING_BUILD,
			PLACING_BUILD,
		}

		private enum Tab {
			BUILD,
		}

		// camera
		[Export] Node2D cameraCursor;

		// bottom bar buttons
		[Export] Button buildButton;
		[Export] Button policyButton;
		[Export] Button worldButton;


		// bottom bar menus menus
		[Export] TabContainer menuTabs;

		[Export] ItemList buildMenuList;
		[Export] Button buildMenuConfirmation;

		// top bar
		[Export] Label fpsLabel; // debug

		private State _state;
		private State state {
			get => _state;
			set {
				var old = _state;
				_state = value;
				OnStateChanged(old, value);
				//Debug.PrintWithStack("UI: state changed to", value);
			}
		}
		private long selectedBuildThingId = -1;
		private BuildingView buildingScene = null;


		// overrides and connections

		public override void _Ready() {

			buildButton.Pressed += OnBuildButtonPressed;
			buildMenuList.ItemActivated += OnBuildThingConfirmed;
			buildMenuList.ItemSelected += OnBuildThingSelected;
			buildMenuConfirmation.Pressed += OnBuildThingConfirmed;

			Reset();
		}

		public override void _Process(double delta) {
			fpsLabel.Text = "fps: " + Engine.GetFramesPerSecond().ToString();
		}

		private void OnBuildButtonPressed() {
			if (menuTabs.CurrentTab != (int)Tab.BUILD) {
				state = State.CHOOSING_BUILD;
				SelectTab(0);
			} else {
				state = State.IDLE;
				SelectTab(-1);
			}
		}

		private void OnBuildThingSelected(long which) {
			buildMenuConfirmation.Disabled = false;
			selectedBuildThingId = which;
			buildMenuConfirmation.Text = "Build " + buildMenuList.GetItemText((int)which);
		}

		private void OnBuildThingConfirmed() {
			// pressed button, didnt doubleclick
			OnBuildThingConfirmed(selectedBuildThingId);
		}

		private void OnBuildThingConfirmed(long which) {
			selectedBuildThingId = which;
			EmitSignal(SignalName.BuildTargetSet, selectedBuildThingId);
			selectedBuildThingId = -1;
			SelectTab(-1);
		}

		// menu activites

		private void SelectTab(long which) {
			if (which == -1) {
				// reset some things
				buildMenuConfirmation.Disabled = true;
				buildMenuConfirmation.Text = "select";
				selectedBuildThingId = -1;
			}
			menuTabs.CurrentTab = (int)which;
		}

		public void SetBuildCursorScene(PackedScene packedScene) {
			if (packedScene == null && buildingScene != null) {
				buildingScene.QueueFree();
				buildingScene = null;
				return;
			}
			Node scene = packedScene.Instantiate();
			Debug.Assert(scene is BuildingView, "trying to build something that doesn't extend BuildingView");
			cameraCursor.AddChild(scene);
			buildingScene = scene as BuildingView;
			state = State.PLACING_BUILD;
			buildingScene.Modulate = new Color(buildingScene.Modulate, 0.67f);
		}

		public void OnLeftMouseClick(Vector2 position, Vector2I tilePosition) {
			switch (state) {
				case State.PLACING_BUILD:
					PlacingBuild(tilePosition);
					break;
				default:
					break;
			}
		}

		private void PlacingBuild(Vector2I tpos) {
			EmitSignal(SignalName.BuildRequested, buildingScene, tpos);
		}

		// utilities

		private void Reset() {
			state = State.IDLE;
			menuTabs.CurrentTab = -1;
			buildMenuConfirmation.Disabled = true;
		}

		private void OnStateChanged(State old, State current) {
			if (old == State.PLACING_BUILD && old != current) {
				buildMenuConfirmation.Disabled = true;
				SetBuildCursorScene(null);
			}
		}


	}
}
