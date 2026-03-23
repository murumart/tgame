using System.Collections.Generic;
using System.Linq;
using Godot;

namespace scenes.region.ui;

public partial class JobsList : VBoxContainer {

	[Export] PackedScene infopanelScene;

	[Export] UI ui;
	[Export] Container jobsContainer;
	[Export] Label infoLabel;

	Job[] displayedJobs;


	public void Display() {
		var jobs = ui.GetJobs();
		Debug.Assert(jobs != null, "ui.GetJobs returned null");
		displayedJobs = jobs.ToArray();

		int ix = 0;
		foreach (var child in jobsContainer.GetChildren()) child.QueueFree();
		foreach (var job in displayedJobs) {
			var panel = infopanelScene.Instantiate<JobInfoPanel>();
			jobsContainer.AddChild(panel);
			panel.UIRebuildRequested += Display;
			uint sliderMax = JobInfoPanel.GetSliderMax(job, ui.GetMaxFreeWorkers());
			panel.Display(ui, job, ix, sliderMax, JobWorkerCountChanged);
			ix++;
		}

        var fac = ui.GetFactionActions().Faction;
        if (fac.GetPopulationCount() == 0) return;
        infoLabel.Text = $"You have {fac.Population.EmployedCount} out of {fac.GetPopulationCount()} workers employed.";
	}

	void JobWorkerCountChanged(int ix, float by) {
		Debug.Assert(Mathf.Ceil(by) == Mathf.Floor(by), $"Invaliud worker count change value {by}");
		GD.Print("JobsList::JobWorkerCountChanged : worker count changed by ", by);
		ui.ChangeJobWorkerCount(displayedJobs[ix], (int)by);
		Display(); // rebuild ui entirely in a lazy unoptimised manner
	}

}
