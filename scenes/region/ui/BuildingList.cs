using Godot;
using resources.game;
using resources.game.building_types;
using scenes.region.buildings;
using scenes.region.ui;
using System;
using static Building;

public partial class BuildingList : PanelContainer {

	[Export] UI ui; // COUPLING YAAAAAYYYYYYYY
	[Export] ItemList itemList;
	[Export] Button buildConfirmation;
	[Export] RichTextLabel resourceListText;

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
		itemList.ItemActivated += OnBuildThingConfirmed;
		itemList.ItemSelected += OnBuildThingSelected;
		buildConfirmation.Pressed += OnBuildThingConfirmed;
	}

	public override void _GuiInput(InputEvent evt) {
			if (evt is InputEventMouseButton) {
				GetViewport().SetInputAsHandled();
			}
		}

	void OnBuildThingSelected(long which) {
		buildConfirmation.Disabled = false;
		selectedBuildThingId = which;
		var btype = (BuildingType)itemList.GetItemMetadata((int)which).Obj;
		buildConfirmation.Text = "Build " + btype.Name;
		resourceListText.Text = "";
		var resources = ui.GetResources();
		foreach (var r in btype.GetResourceRequirements()) {
			var str = $"{r.Type.Name} x {r.Amount}";
			if (!resources.HasEnough(r)) {
				str = "[color=red]" + str + "[/color]";
				buildConfirmation.Disabled = true;
			}
			resourceListText.AppendText(str + "\n");
		}
	}

	void OnBuildThingConfirmed() {
		// pressed button, didnt doubleclick
		OnBuildThingConfirmed(selectedBuildThingId);
	}

	void OnBuildThingConfirmed(long which) {
		var btype = (BuildingType)itemList.GetItemMetadata((int)which).Obj;
		if (!ui.GetCanBuild(btype)) return;
		SetBuildCursor(btype);
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
		Debug.Assert(ui.GetCanBuild(buildingType), "can't build this...");
		var packedScene = GD.Load<PackedScene>(DataStorage.GetScenePath(buildingType));
		Node scene = packedScene.Instantiate();
		Debug.Assert(scene != null, "building display scene canät be null here....w aht...");
		Debug.Assert(scene is BuildingView, "trying to build something that doesn't extend BuildingView");
		ui.CameraCursor.AddChild(scene);
		selectedBuildingScene = scene as BuildingView;
		selectedBuildingType = buildingType;
		ui.state = UI.State.PLACING_BUILD;
		selectedBuildingScene.Modulate = new Color(selectedBuildingScene.Modulate, 0.67f);
	}

	public void RequestBuild(Vector2I tpos) {
		ui.RequestBuild(selectedBuildingType, tpos);
	}

	public void Update() {
		itemList.Clear();
		foreach (var buildingType in ui.GetBuildingTypes()) {
			int ix = itemList.AddItem(buildingType.Name);
			// storing buildingtype references locally so if we happen to update the buildingtypes list
			// in between calls here, we should still get the correct buildings that the visual
			// ItemList was set up with
			itemList.SetItemMetadata(ix, Variant.CreateFrom(buildingType));
		}
	}

	public void Reset() {
		selectedBuildThingId = -1;
		buildConfirmation.Disabled = true;
		buildConfirmation.Text = "select";
		resourceListText.Text = "";
		itemList.Clear();
	}


}
