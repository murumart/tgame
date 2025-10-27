using Godot;
using scenes.region.buildings;
using scenes.region.ui;
using System;
using static Building;

public partial class BuildingList : PanelContainer {

	[Export] UI ui; // COUPLING YAAAAAYYYYYYYY
	[Export] ItemList buildMenuList;
	[Export] Button buildMenuConfirmation;

	long selectedBuildThingId = -1;
	BuildingView selectedBuildingScene = null;
	IBuildingType _selectedBuildingType = null;
	IBuildingType selectedBuildingType {
		get {
			//Debug.PrintWithStack("GET sel build type: ", _selectedBuildingType);
			return _selectedBuildingType;
		}
		set {
			_selectedBuildingType = value;
			//Debug.PrintWithStack("SET sel build type: ", _selectedBuildingType);
		}
	}


	public override void _Ready() {
		buildMenuList.ItemActivated += OnBuildThingConfirmed;
		buildMenuList.ItemSelected += OnBuildThingSelected;
		buildMenuConfirmation.Pressed += OnBuildThingConfirmed;
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
		ui.SelectTab(UI.Tab.NONE);
	}

	public void SetBuildCursor(IBuildingType buildingType) {
		if (buildingType == null) {
			if (selectedBuildingScene != null) {
				selectedBuildingScene.QueueFree();
				selectedBuildingType = null;
			}
			selectedBuildingScene = null;
			selectedBuildingType = null;
			return;
		}
		Debug.Assert(buildingType != null, "Buuldint type canät be null here....w aht...");
		var packedScene = GD.Load<PackedScene>(buildingType.GetScenePath());
		Node scene = packedScene.Instantiate();
		Debug.Assert(scene != null, "building display scene canät be null here....w aht...");
		Debug.Assert(scene is BuildingView, "trying to build something that doesn't extend BuildingView");
		ui.CameraCursor.AddChild(scene);
		selectedBuildingScene = scene as BuildingView;
		selectedBuildingType = buildingType;
		ui.state = UI.State.PLACING_BUILD;
		selectedBuildingScene.Modulate = new Color(selectedBuildingScene.Modulate, 0.67f);
	}

	public void PlacingBuild(Vector2I tpos) {
		ui.BuildRequested(selectedBuildingType, tpos);
	}

	public void Update() {
		buildMenuList.Clear();
		foreach (var buildingType in ui.GetBuildingTypes()) {
			int ix = buildMenuList.AddItem(buildingType.GetName());
			// storing buildingtype references locally so if we happen to update the buildingtypes list
			// in between calls here, we should still get the correct buildings that the visual
			// ItemList was set up with
			buildMenuList.SetItemMetadata(ix, Variant.CreateFrom(buildingType));
		}
	}

	public void Reset() {
		selectedBuildThingId = -1;
		buildMenuConfirmation.Disabled = true;
		buildMenuConfirmation.Text = "select";
		buildMenuList.Clear();
	}


}
