using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace scenes.region.ui;

public partial class MapObjectMenu : Control {

	public enum State : int {
		ViewJob,
		AddJob,
	}

	[Export] UI ui;

	[Export] Label titleLabel;
	[Export] JobInfoPanel jobInfoPanel;
	[Export] TradeInfoPanel tradeInfoPanel;

	[Export] Control addJobMenu;
	[Export] ItemList addJobItemList;
	[Export] RichTextLabel addJobDescription;
	[Export] Button addJobConfirmButton;

	[Export] RichTextLabel detailsText;

	bool attachedToMapObject = false;
	State state;
	MapObject myMapObject;
	bool IsMarketplaceBuilding => (myMapObject is Building b && b.IsConstructed && b.Type.GetSpecial() == Building.IBuildingType.Special.Marketplace);
	bool IsMarketplaceActive => IsMarketplaceBuilding && ExtantJob != null && ExtantJob is ProcessMarketJob;

	// we're regenerating these lists every time the menu is opened or updated, hopefully not a performance issue!
	// an alternative is to not do that and set the _* lists to null to regenerate them on next menu open.

	Job ExtantJob {
		get {
			Debug.Assert(attachedToMapObject, "Map object menu isn't attached to map object");
			return ui.GetMapObjectJob(myMapObject);
		}
	}

	List<Job> AvailableJobs {
		get {
			if (attachedToMapObject) return myMapObject.GetAvailableJobs().ToList();
			return new();
		}
	}
	int selectedAddJob = -1;


	public override void _Ready() {
		addJobItemList.ItemSelected += AddJobSelected;
		addJobItemList.ItemActivated += AddJobConfirmed;
		addJobConfirmButton.Pressed += AddJobConfirmed;
		jobInfoPanel.UIRebuildRequested += Open;

		addJobDescription.Text = "";
	}

	public override void _GuiInput(InputEvent evt) {
		if (evt is InputEventMouseButton) {
			GetViewport().SetInputAsHandled();
		}
	}

	public void Open() {
		if (ExtantJob == null && AvailableJobs.Count != 0) {
			if (myMapObject is Building building && building.IsConstructed) {
				OpenAddJobScreen();
			} else {
				OpenAddJobScreen();
			}
		} else {
			OpenViewJobScreen();
		}
		detailsText.Text = "";
		addJobDescription.Text = "";
		SizeFlagsStretchRatio = IsMarketplaceActive ? 4.0f : 1.0f;
		tradeInfoPanel.Visible = IsMarketplaceActive;
		if (IsMarketplaceActive) {
			var job = ExtantJob as ProcessMarketJob;
			tradeInfoPanel.Display(job.Faction, job.TradeOffers);
		}

		Callable.From(Show).CallDeferred();
	}

	public void Open(MapObject mapObject) {
		attachedToMapObject = true;
		this.myMapObject = mapObject;

		Open();
		OpenMapObjectInfo();
	}

	public void Close() {
		Hide();
		//_extantJobs = null;
		//_availableJobs = null;
		myMapObject = null;
		selectedAddJob = -1;
		addJobConfirmButton.Disabled = true;
	}

	void OpenAddJobScreen() {
		state = State.AddJob;
		addJobMenu.Show();
		jobInfoPanel.Hide();

		addJobItemList.Clear();

		foreach (var job in AvailableJobs) {
			addJobItemList.AddItem(job.Title);
		}
		addJobConfirmButton.Disabled = selectedAddJob == -1;
	}

	void OpenViewJobScreen() {
		GD.Print("JobsList::OpenViewJobScreen : opening job view screen");
		state = State.ViewJob;
		jobInfoPanel.Show();
		addJobMenu.Hide();

		uint sliderMax = 0;
		if (ExtantJob == null) {
			jobInfoPanel.Hide();
			return;
		}
		jobInfoPanel.Show();
		if (ExtantJob.NeedsWorkers) sliderMax = Math.Min(ui.GetMaxFreeWorkers() + (uint)ExtantJob.Workers, ExtantJob.MaxWorkers);
		jobInfoPanel.Display(ui, ExtantJob, 0, sliderMax, JobWorkerCountChanged);
	}

	void OpenMapObjectInfo() {
		Debug.Assert(attachedToMapObject && myMapObject != null, "Needs a map object to display its info");

		System.Text.StringBuilder sb = new();
		if (myMapObject is Building b) {
			titleLabel.Text = b.Type.AssetName.Capitalize();
			if (!b.IsConstructed) {
				sb.Append($"Construction in progress... ({(int)(b.GetBuildProgress() * 100)}%)\n");
			}
			sb.Append(b.Type.GetDescription()).Append('\n');
			if (IsMarketplaceBuilding && !IsMarketplaceActive) sb.Append("Add a job to process trade activities.").Append('\n');
			var crafting = b.Type.GetCraftJobs();
			if (crafting.Length > 0) {
				sb.Append("Production of the following occurs here...\n");
				foreach (var craftjob in crafting) {
					Debug.Assert(craftjob.Process != null, "I wanna verb");
					sb.Append(craftjob.Process.Progressive.Capitalize()).Append(' ').Append(craftjob.Product.Plural).Append(" gives...\n");
					craftjob.GetProductionBulletList(sb);
				}
			}
			if (b.GetHousingCapacity() > 0 && b.IsConstructed) {
				sb.Append($"Housing room for {b.GetHousingCapacity()} people.\n");
			}
		} else if (myMapObject is ResourceSite r) {
			titleLabel.Text = r.Type.AssetName.Capitalize();
			sb.Append($"The {r.Type.AssetName} contains exploitable resources...\n");
			bool reproduce = false;
			foreach (var well in r.Wells) {
				sb.Append($" * {well.ResourceType.AssetName} x {well.Bunches * well.BunchSize}\n");
				sb.Append($"   This is {100 - ((float)well.Bunches / well.InitialBunches) * 100:0}% depleted.\n");
				reproduce = reproduce || well.MinutesPerBunchRegen > 0;
			}
			if (reproduce) {
				sb.Append($"\nSome {("resources")} can regrow...\n");
				foreach (var well in r.Wells) {
					if (well.MinutesPerBunchRegen > 0) {
						sb.Append($" * {well.ResourceType.AssetName} grows every {GameTime.GetFancyTimeString(well.MinutesPerBunchRegen)}.\n");
					}
				}
			}
		} else Debug.Assert(false, "Unimplemented map object name display");

		detailsText.Text = sb.ToString();
	}

	void JobWorkerCountChanged(int ix, int by) {
		GD.Print("JobsList::JobWorkerCountChanged : worker count changed by ", by);
		ui.ChangeJobWorkerCount(ExtantJob, by);
		OpenViewJobScreen(); // rebuild ui entirely in a lazy unoptimised manner
	}

	void AddJobSelected(long ix) {
		selectedAddJob = (int)ix;
		addJobConfirmButton.Disabled = false;
		addJobDescription.Text = AvailableJobs[selectedAddJob].GetProductionDescription();
	}

	void AddJobConfirmed(long ix) {
		if (attachedToMapObject) {
			ui.AddJobRequested(myMapObject, (MapObjectJob)AvailableJobs[(int)ix]);
		} else {
			ui.AddJobRequested(AvailableJobs[(int)ix]);
		}
		selectedAddJob = -1;
		//_extantJobs = null;
		OpenViewJobScreen();
	}

	void AddJobConfirmed() {
		Debug.Assert(selectedAddJob != -1, "Please select a job before confirming!!");
		if (myMapObject is Building building) Debug.Assert(building.IsConstructed, "Building needs to be constructed before adding job!!!");
		AddJobConfirmed(selectedAddJob);
	}

}



