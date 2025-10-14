using Godot;
using scenes.region.view.buildings;
using System;
using System.Collections.Generic;


namespace scenes.region.view {
	public partial class UI : Control {

		// one big script to rule all region ui interactions


		[Signal] public delegate void BuildRequestedEventHandler(BuildingView building, Vector2I tilePosition);

		private enum State {
			IDLE,
			CHOOSING_BUILD,
			PLACING_BUILD,
		}

		private enum Tab : int {
			NONE = -1,
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
		public List<BuildingType> BuildingTypes;


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
				SelectTab(Tab.BUILD);
			} else {
				state = State.IDLE;
				SelectTab(Tab.NONE);
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
			SetBuildCursor((BuildingType)buildMenuList.GetItemMetadata((int)which).AsGodotObject());
			selectedBuildThingId = -1;
			SelectTab(Tab.NONE);
		}

		// menu activites

		private void SelectTab(Tab which) {
			if (which == Tab.NONE) {
				// reset some things
				buildMenuConfirmation.Disabled = true;
				buildMenuConfirmation.Text = "select";
				selectedBuildThingId = -1;
				buildMenuList.Clear();
			} else if (which == Tab.BUILD) {
				UpdateBuildMenuList();
			}
			menuTabs.CurrentTab = (int)which;
		}

		private void UpdateBuildMenuList() {
			buildMenuList.Clear();
			foreach (var buildingType in BuildingTypes) {
				int ix = buildMenuList.AddItem(buildingType.Name);
				buildMenuList.SetItemMetadata(ix, Variant.CreateFrom(buildingType));
			}
		}

		public void SetBuildCursor(BuildingType buildingType) {
			if (buildingType == null && buildingScene != null) {
				buildingScene.QueueFree();
				buildingScene = null;
				return;
			}
			var packedScene = GD.Load<PackedScene>(buildingType.ScenePath);
			Node scene = packedScene.Instantiate();
			Debug.Assert(scene is BuildingView, "trying to build something that doesn't extend BuildingView");
			cameraCursor.AddChild(scene);
			buildingScene = scene as BuildingView;
			buildingScene.BuildingType = buildingType;
			state = State.PLACING_BUILD;
			buildingScene.Modulate = new Color(buildingScene.Modulate, 0.67f);
		}

		private void PlacingBuild(Vector2I tpos) {
			EmitSignal(SignalName.BuildRequested, buildingScene, tpos);
		}

		// utilities

		public void OnLeftMouseClick(Vector2 position, Vector2I tilePosition) {
			switch (state) {
				case State.PLACING_BUILD:
					PlacingBuild(tilePosition);
					break;
				default:
					break;
			}
		}

		public void OnTileHighlighted(Vector2I tilePosition, Region region) {

		}

		private void Reset() {
			state = State.IDLE;
			menuTabs.CurrentTab = -1;
			buildMenuConfirmation.Disabled = true;
		}

		private void OnStateChanged(State old, State current) {
			if (old == State.PLACING_BUILD && old != current) {
				buildMenuConfirmation.Disabled = true;
				SetBuildCursor(null);
			}
		}
	}
}
