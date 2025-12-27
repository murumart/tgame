using Godot;
using scenes.region.buildings;
using System;
using System.Collections.Generic;
using System.Linq;


namespace scenes.region.ui {

	public partial class JobsList : Control {

		public enum State : int {
			VIEW_JOBS,
			ADD_JOBS,
		}

		[Export] UI ui;
		[Export] TabContainer tabs;

		[Export] Button viewJobTabButton;
		[Export] BoxContainer jobsList;
		[Export] Label buildingTitle;

		[Export] Button addJobTabButton;
		[Export] ItemList addJobItemList;
		[Export] RichTextLabel addJobDescription;
		[Export] Button addJobConfirmButton;

		bool attachedToMapObject = false;
		State state;
		MapObject myMapObject;

		// we're regenerating these lists every time the menu is opened or updated, hopefully not a performance issue!
		// an alternative is to not do that and set the _* lists to null to regenerate them on next menu open.

		//List<JobBox> _extantJobs;
		List<JobBox> ExtantJobs {
			get {
				if (attachedToMapObject) return ui.GetMapObjectJobs(myMapObject).ToList();
				return ui.GetJobs().ToList();
			}
		}
		//{
		//	get {
		//		Debug.Assert(myBuilding != null, "Please open the menu first!!! before getting these data");
		//		_extantJobs ??= ui.GetBuildingJobs(myBuilding).ToList(); ;
		//		return _extantJobs;
		//	}
		//}

		//List<JobBox> _availableJobs;
		List<Job> AvailableJobs {
			get {
				if (attachedToMapObject) return myMapObject.Type.GetAvailableJobs().ToList();
				return new();
			}
		}
		//{
		//	get {
		//		Debug.Assert(myBuilding != null, "Please open the menu first!!! before getting these data");
		//		_availableJobs ??= myBuilding.Type.GetAvailableJobs().ToList();
		//		return _availableJobs;
		//	}
		//}
		int selectedAddJob = -1;


		public override void _Ready() {
			addJobTabButton.Pressed += AddJobClicked;
			viewJobTabButton.Pressed += ViewJobsClicked;
			addJobItemList.ItemSelected += AddJobSelected;
			addJobItemList.ItemActivated += AddJobConfirmed;
			addJobConfirmButton.Pressed += AddJobConfirmed;

			addJobDescription.Text = "";
		}

		public override void _GuiInput(InputEvent evt) {
			if (evt is InputEventMouseButton) {
				GetViewport().SetInputAsHandled();
			}
		}

		public void Open() {
			if (ExtantJobs.Count == 0 && AvailableJobs.Count != 0) {
				if (myMapObject is Building building && building.IsConstructed) {
					OpenAddJobScreen();
				} else {
					OpenAddJobScreen();
				}
			} else {
				OpenViewJobScreen();
			}

			CallDeferred("show");
		}

		public void Open(Building myBuilding) {
			attachedToMapObject = true;
			this.myMapObject = myBuilding;

			buildingTitle.Text = "Jobs in " + myBuilding.Type.AssetName;

			Open();
		}

		public void Open(ResourceSite resourceSite) {
			attachedToMapObject = true;
			this.myMapObject = resourceSite;

			buildingTitle.Text = "Jobs at " + resourceSite.Type.AssetName;

			Open();
		}

		public void Close() {
			Hide();
			//_extantJobs = null;
			//_availableJobs = null;
			myMapObject = null;
			selectedAddJob = -1;
			addJobConfirmButton.Disabled = true;

			foreach (var node in jobsList.GetChildren()) node.QueueFree();
		}

		void AddJobClicked() {
			if (state == State.VIEW_JOBS) {
				OpenAddJobScreen();
			}
		}

		void ViewJobsClicked() {
			if (state == State.ADD_JOBS) {
				OpenViewJobScreen();
			}
		}

		void OpenAddJobScreen() {
			state = State.ADD_JOBS;
			tabs.CurrentTab = (int)state;

			addJobItemList.Clear();

			foreach (var job in AvailableJobs) {
				addJobItemList.AddItem(job.Title);
			}
			addJobConfirmButton.Disabled = selectedAddJob == -1;

			ManageTabButtons();
		}

		void OpenViewJobScreen() {
			GD.Print("JobsList::OpenViewJobScreen : opening jobs");
			state = State.VIEW_JOBS;
			tabs.CurrentTab = (int)state;

			foreach (var node in jobsList.GetChildren()) node.QueueFree();

			for (int i = ExtantJobs.Count - 1; i >= 0; i--) {
				var box = ExtantJobs[i];
				int sliderMax = Math.Min(ui.GetMaxFreeWorkers() + box.Workers.Count, box.Workers.Capacity);
				var panel = JobInfoPanel.Packed.Instantiate<JobInfoPanel>();
				panel.AddToTree(jobsList, box.IsDeletable, true);
				panel.Display(ui, box, i, sliderMax, JobWorkerCountChanged);
				panel.UIRebuildRequested += OpenViewJobScreen;
			}

			ManageTabButtons();
		}

		void JobWorkerCountChanged(int ix, int by) {
			GD.Print("JobsList::JobWorkerCountChanged : worker count changed by ", by);
			ui.ChangeJobWorkerCount(ExtantJobs[ix], by);
			OpenViewJobScreen(); // rebuild ui entirely in a lazy unoptimised manner
		}

		void AddJobSelected(long ix) {
			selectedAddJob = (int)ix;
			addJobConfirmButton.Disabled = false;
			addJobDescription.Text =
				AvailableJobs[selectedAddJob].GetResourceRequirementDescription()
				+ "\n" + AvailableJobs[selectedAddJob].GetProductionDescription();
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

		void ManageTabButtons() {
			viewJobTabButton.Disabled = ExtantJobs.Count == 0;
			addJobTabButton.Disabled = AvailableJobs.Count == 0 || (myMapObject is Building building && !building.IsConstructed);

			if (state == State.VIEW_JOBS) viewJobTabButton.Disabled = true;
			if (state == State.ADD_JOBS) addJobTabButton.Disabled = true;
		}

	}

}

