using Godot;
using scenes.region.buildings;
using System;
using System.Collections.Generic;
using IBuildingType = Building.IBuildingType;


namespace scenes.region.ui {

	public partial class UI : Control {

		// one big script to rule all region ui interactions


		public event Action<Building.IBuildingType, Vector2I> BuildRequested;
		public event Func<int> GetPopulationCount;
		public event Func<int> GetHomelessPopulationCount;
		public event Func<List<BuildingType>> GetBuildingTypes;
		public event Func<ResourceStorage> GetResources;

		enum State {
			IDLE,
			CHOOSING_BUILD,
			PLACING_BUILD,
		}

		enum Tab : int {
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
		[Export] Label populationLabel;
		[Export] Label fpsLabel; // debug
		[Export] Label tilePosLabel; // debug

		[Export] RichTextLabel resourceLabel;

		// internal

		State _state;
		State state {
			get => _state;
			set {
				var old = _state;
				_state = value;
				OnStateChanged(old, value);
				//Debug.PrintWithStack("UI: state changed to", value);
			}
		}
		long selectedBuildThingId = -1;
		BuildingView selectedBuildingScene = null;
		Building.IBuildingType selectedBuildingType = null;

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
			populationLabel.Text = $"pop: {GetPopulationCount?.Invoke() ?? -2} ({GetHomelessPopulationCount?.Invoke() ?? -2} homeless)";
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

		void OnBuildThingSelected(long which) {
			buildMenuConfirmation.Disabled = false;
			selectedBuildThingId = which;
			buildMenuConfirmation.Text = "Build " + buildMenuList.GetItemText((int)which);
		}

		void OnBuildThingConfirmed() {
			// pressed button, didnt doubleclick
			OnBuildThingConfirmed(selectedBuildThingId);
		}

		void OnBuildThingConfirmed(long which) {
			selectedBuildThingId = which;
			SetBuildCursor((BuildingType)buildMenuList.GetItemMetadata((int)which).AsGodotObject());
			selectedBuildThingId = -1;
			SelectTab(Tab.NONE);
		}

		// menu activites

		void SelectTab(Tab which) {
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

		void UpdateBuildMenuList() {
			buildMenuList.Clear();
			foreach (var buildingType in GetBuildingTypes?.Invoke()) {
				int ix = buildMenuList.AddItem(buildingType.GetName());
				// storing buildingtype references locally so if we happen to update the buildingtypes list
				// in between calls here, we should still get the correct buildings that the visual
				// ItemList was set up with
				buildMenuList.SetItemMetadata(ix, Variant.CreateFrom(buildingType));
			}
		}

		public void SetBuildCursor(IBuildingType buildingType) {
			if (buildingType == null && selectedBuildingScene != null) {
				selectedBuildingScene.QueueFree();
				selectedBuildingScene = null;
				selectedBuildingType = null;
				return;
			}
			var packedScene = GD.Load<PackedScene>(buildingType.GetScenePath());
			Node scene = packedScene.Instantiate();
			Debug.Assert(scene is BuildingView, "trying to build something that doesn't extend BuildingView");
			cameraCursor.AddChild(scene);
			selectedBuildingScene = scene as BuildingView;
			selectedBuildingType = buildingType;
			state = State.PLACING_BUILD;
			selectedBuildingScene.Modulate = new Color(selectedBuildingScene.Modulate, 0.67f);
		}

		void PlacingBuild(Vector2I tpos) {
			BuildRequested.Invoke(selectedBuildingType, tpos);
		}

		// display

		void DisplayResources() {
			resourceLabel.Text = "";
			var resources = GetResources?.Invoke();
			foreach (var p in resources) {
				resourceLabel.AppendText($"{p.Key.Name} x {p.Value.Amount}\n");
			}
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

		void Reset() {
			state = State.IDLE;
			menuTabs.CurrentTab = -1;
			buildMenuConfirmation.Disabled = true;
		}

		void OnStateChanged(State old, State current) {
			if (old != current) {
				if (old == State.PLACING_BUILD) {
					buildMenuConfirmation.Disabled = true;
					SetBuildCursor(null);
				}
			}
		}

	}

}
