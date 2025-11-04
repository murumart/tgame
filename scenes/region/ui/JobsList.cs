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

		State state;
		BuildingView buildingView;

		List<Job> _extantJobs;
		List<Job> ExtantJobs {
			get {
				Debug.Assert(buildingView != null, "Please open the menu first!!! before getting these data");
				_extantJobs ??= ui.GetBuildingJobs(buildingView).ToList(); ;
				return _extantJobs;
			}
		}

		List<Job> _availableJobs;
		List<Job> AvailableJobs {
			get {
				Debug.Assert(buildingView != null, "Please open the menu first!!! before getting these data");
				_availableJobs ??= buildingView.Building.Type.GetAvailableJobs().ToList();
				return _availableJobs;
			}
		}
		int selectedAddJob = -1;



		public override void _Ready() {
			addJobTabButton.Pressed += AddJobClicked;
			viewJobTabButton.Pressed += ViewJobsClicked;
			addJobItemList.ItemSelected += AddJobSelected;
			addJobItemList.ItemActivated += AddJobConfirmed;
			addJobConfirmButton.Pressed += AddJobConfirmed;

			addJobDescription.Text = "";
		}

		public void Open(BuildingView buildingView) {
			this.buildingView = buildingView;

			buildingTitle.Text = "Jobs of " + buildingView.Building.Type.GetName();

			if (ExtantJobs.Count == 0 && AvailableJobs.Count != 0 && buildingView.Building.IsConstructed) OpenAddJobScreen();
			else OpenViewJobScreen();

			CallDeferred("show");
		}

		public void Close() {
			Hide();
			_extantJobs = null;
			_availableJobs = null;
			buildingView = null;
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
			GD.Print("opening jobs");
			state = State.VIEW_JOBS;
			tabs.CurrentTab = (int)state;

			foreach (var node in jobsList.GetChildren()) node.QueueFree();

			for (int i = ExtantJobs.Count - 1; i >= 0; i--) {
				var job = ExtantJobs[i];
				int sliderMax = ui.GetMaxFreeWorkers() + job.GetWorkers().Pop;
				var panel = JobInfoPanel.Packed.Instantiate<JobInfoPanel>();
				panel.AddToTree(jobsList, false, true);
				panel.Display(job, i, sliderMax, JobWorkerCountChanged);
			}

			ManageTabButtons();
		}

		void JobWorkerCountChanged(int ix, int by) {
			GD.Print("worker count changed by ", by);
			ui.ChangeJobWorkerCount(ExtantJobs[ix], by);
			OpenViewJobScreen(); // redo all so that we're good....witht the thing
		}

		void AddJobSelected(long ix) {
			selectedAddJob = (int)ix;
			addJobConfirmButton.Disabled = false;
			addJobDescription.Text = AvailableJobs[selectedAddJob].GetResourceRequirementDescription();
		}

		void AddJobConfirmed(long ix) {
			ui.AddJobRequested(buildingView.Building, AvailableJobs[(int)ix].Copy());
			selectedAddJob = -1;
			_extantJobs = null;
			OpenViewJobScreen();
		}

		void AddJobConfirmed() {
			Debug.Assert(selectedAddJob != -1, "Please select a job before confirming!!");
			Debug.Assert(buildingView.Building.IsConstructed, "Building needs to be constructed before adding job!!!");
			AddJobConfirmed(selectedAddJob);
		}

		void ManageTabButtons() {
			viewJobTabButton.Disabled = ExtantJobs.Count == 0;
			addJobTabButton.Disabled = AvailableJobs.Count == 0 || !buildingView.Building.IsConstructed;

			if (state == State.VIEW_JOBS) viewJobTabButton.Disabled = true;
			if (state == State.ADD_JOBS) addJobTabButton.Disabled = true;
		}

	}

}

