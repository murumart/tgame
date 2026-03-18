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

	[Export] Control addJobMenu;
	[Export] ItemList addJobItemList;
	[Export] RichTextLabel addJobDescription;
	[Export] Button addJobConfirmButton;

	[Export] RichTextLabel detailsText;
	[Export] Button closeButton;

	State state;
	MapObject myMapObject;
	Problem myProblem;

	// we're regenerating these lists every time the menu is opened or updated, hopefully not a performance issue!
	// an alternative is to not do that and set the _* lists to null to regenerate them on next menu open.

	Job ExtantJob {
		get {
			if (myProblem is null) {
				Debug.Assert(myMapObject is not null, "Map object menu isn't attached to map object");
				return ui.GetMapObjectJob(myMapObject);
			} else {
				return myProblem.Job;
			}
		}
	}

	List<Job> AvailableJobs {
		get {
			if (myMapObject is not null) return myMapObject.GetAvailableJobs().ToList();
			if (myProblem is not null && ExtantJob is null) return [new SolveProblemJob(myProblem)];
			return new();
		}
	}
	int selectedAddJob = -1;


	public override void _Ready() {
		addJobItemList.ItemSelected += AddJobSelected;
		addJobItemList.ItemActivated += AddJobConfirmed;
		addJobConfirmButton.Pressed += AddJobConfirmed;
		jobInfoPanel.UIRebuildRequested += Open;
		closeButton.Pressed += Close;

		addJobDescription.Text = "";
	}

	public override void _GuiInput(InputEvent evt) {
		if (evt is InputEventMouseButton) {
			GetViewport().SetInputAsHandled();
		}
	}

	public void Open() {
		bool problematic = myProblem is not null;
		detailsText.Text = "";
		addJobDescription.Text = "";
		if (ExtantJob == null && AvailableJobs.Count != 0) {
			if (problematic) {
				OpenAddJobScreen();
			} else if (myMapObject is Building building && building.IsConstructed) {
				OpenAddJobScreen();
			} else if (myMapObject.Removed) {
				Close();
				return;
			} else {
				OpenAddJobScreen();
			}
		} else {
			OpenViewJobScreen();
		}

		if (!problematic) DisplayMapObjectInfo();
		else DisplayProblemInfo();
		Callable.From(Show).CallDeferred();
	}

	public void Open(MapObject mapObject) {
		this.myMapObject = mapObject;
		this.myProblem = null;

		Open();
	}

	public void Open(Problem problem) {
		this.myProblem = problem;
		this.myMapObject = null;

		Open();
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

		if (ExtantJob == null) {
			jobInfoPanel.Hide();
			return;
		}
		jobInfoPanel.Show();
		uint sliderMax = JobInfoPanel.GetSliderMax(ExtantJob, ui.GetMaxFreeWorkers());
		jobInfoPanel.Display(ui, ExtantJob, 0, sliderMax, JobWorkerCountChanged);
	}

	void DisplayMapObjectInfo() {
		Debug.Assert(myMapObject != null, "Needs a map object to display its info");

		System.Text.StringBuilder sb = new();
		var reg = ui.GetFactionActions().Region;
		if (myMapObject is Building b) {
			titleLabel.Text = $"{b.Type.AssetName.Capitalize()} {(b.GlobalPosition - reg.WorldPosition)}";
			if (!b.IsConstructed) {
				sb.Append($"Construction in progress... ({(int)(b.GetBuildProgress() * 100)}%)\n");
			}
			sb.Append(b.Type.GetDescription()).Append('\n');
			// this looks kind of ugly and duplicates the job item list selection menu where yu can click on the jovs and see what they do.
			//var crafting = b.Type.GetCraftJobs();
			//if (crafting.Length > 0) {
			//	sb.Append("Production of the following occurs here...\n");
			//	foreach (var craftjob in crafting) {
			//		Debug.Assert(craftjob.Process != null, "I wanna verb");
			//		sb.Append(craftjob.Process.Progressive.Capitalize()).Append(' ').Append(craftjob.Product.Plural).Append(" gives...\n");
			//		craftjob.GetProductionBulletList(sb);
			//	}
			//}
			if (b.GetHousingCapacity() > 0 && b.IsConstructed) {
				sb.Append($"Housing room for {b.GetHousingCapacity()} people.\n");
				sb.Append(b.HasFurniture ? "Is furnished.\n" : "Is without furniture.\n");
			}
		} else if (myMapObject is ResourceSite r) {
			titleLabel.Text = $"{r.Type.AssetName.Capitalize()} {(r.GlobalPosition - reg.WorldPosition)}";
			sb.Append($"The {r.Type.AssetName} contains exploitable resources...\n");
			bool reproduce = false;
			foreach (var well in r.Wells) {
				sb.Append($" * {well.ResourceType.AssetName} x {well.Bunches}\n");
				sb.Append($"   This is {100 - ((float)well.Bunches / well.InitialBunches) * 100:0}% depleted.\n");
				reproduce = reproduce || well.MinutesPerBunchRegen > 0;
			}
			//if (reproduce) {
			//	sb.Append($"\nSome {("resources")} can regrow...\n");
			//	foreach (var well in r.Wells) {
			//		if (well.MinutesPerBunchRegen > 0) {
			//			sb.Append($" * {well.ResourceType.AssetName} grows every {GameTime.GetFancyTimeString(well.MinutesPerBunchRegen)}.\n");
			//		}
			//	}
			//}
		} else Debug.Assert(false, "Unimplemented map object name display");

		detailsText.Text = sb.ToString();
	}

	void DisplayProblemInfo() {
		Debug.Assert(myProblem != null, "Need a problem to display it BTW!!!!!!");
		titleLabel.Text = $"{myProblem.Title.Capitalize()} {(myProblem.LocalPosition)}";
		System.Text.StringBuilder sb = new();
		sb.Append("You're in big trouble now").Append('\n');
		detailsText.Text = sb.ToString();
	}

	void JobWorkerCountChanged(int ix, int by) {
		GD.Print("MapObjectMenu::JobWorkerCountChanged : worker count changed by ", by);
		ui.ChangeJobWorkerCount(ExtantJob, by);
		OpenViewJobScreen(); // rebuild ui entirely in a lazy unoptimised manner
	}

	void AddJobSelected(long ix) {
		selectedAddJob = (int)ix;
		addJobConfirmButton.Disabled = false;
		addJobDescription.Text = AvailableJobs[selectedAddJob].GetProductionDescription();
	}

	void AddJobConfirmed(long ix) {
		if (myMapObject is not null) {
			ui.AddJobRequested(myMapObject, (MapObjectJob)AvailableJobs[(int)ix]);
		} else if (myProblem is not null) {
			ui.AddJobRequested(myProblem, (SolveProblemJob)AvailableJobs[(int)ix]);
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
