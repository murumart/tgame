using System;
using Godot;
using resources.game;
using resources.game.building_types;
using scenes.region;
using scenes.region.ui;
using static Building;

public partial class BuildingList : Control {

	[Export] UI ui;
	[Export] ItemList itemList;
	[Export] Button buildConfirmation;
	[Export] RichTextLabel resourceListText;

	long selectedBuildThingId = -1;
	MapObjectView selectedBuildingScene = null;
	IBuildingType _selectedBuildingType = null;
	public IBuildingType SelectedBuildingType {
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
		buildConfirmation.Text = "Build " + btype.AssetName;
		resourceListText.Text = btype.GetDescription() + '\n';
		var resources = ui.GetResources();
		foreach (var r in btype.GetResourceRequirements()) {
			var str = $"{r.Type.AssetName} x {r.Amount}";
			if (!resources.HasEnough(r)) {
				str = "[color=red]" + str + "[/color]";
				buildConfirmation.Disabled = true;
			}
			resourceListText.AppendText(str + '\n');
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
		ui.SelectTab(UI.Tab.None);
	}

	public void SetBuildCursor(IBuildingType buildingType) {
		if (buildingType == null) {
			if (selectedBuildingScene != null) {
				selectedBuildingScene.QueueFree();
				SelectedBuildingType = null;
			}
			selectedBuildingScene = null;
			SelectedBuildingType = null;
			return;
		}
		Debug.Assert(buildingType != null, "Buuldint type canät be null here....w aht...");
		Debug.Assert(ui.GetCanBuild(buildingType), "can't build this...");
		var scene = MapObjectView.Make(DataStorage.GetScenePath(buildingType), buildingType);
		Debug.Assert(scene != null, "building display scene canät be null here....w aht...");
		ui.Camera.Cursor.AddChild(scene);
		selectedBuildingScene = scene;
		SelectedBuildingType = buildingType;
		ui.state = UI.State.PlacingBuild;
		selectedBuildingScene.Modulate = new Color(selectedBuildingScene.Modulate, 0.67f);
	}

	public void Update() {
		itemList.Clear();
		foreach (var buildingType in ui.GetBuildingTypes()) {
			int ix = itemList.AddItem(buildingType.AssetName);
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
