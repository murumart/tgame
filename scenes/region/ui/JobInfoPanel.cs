using System;
using Godot;

namespace scenes.region.ui;

public partial class JobInfoPanel : Control {

	public event Action UIRebuildRequested;
	public event Action JobFocusRequested;

	[ExportGroup("Nodes")]
	[Export] RichTextLabel infoLabel;
	[Export] Label titleLabel;
	[Export] Container informationList;
	[Export] Button DeleteJobButton;
	[Export] Button FocusOnJobButton;
	[Export] JobSlider workerCountSlider;

	UI ui;
	Job jobBox;


	public override void _Ready() {
		DeleteJobButton.Pressed += RemoveJob;
		FocusOnJobButton.Pressed += JumpToJob;
	}

	public void Display(UI ui, Job job, int jobIndex, uint sliderMax, Action<int, int> workersSelected) {
		this.ui = ui;
		jobBox = job;
		if (job.NeedsWorkers) {
			Debug.Assert(sliderMax < 1000, $"Slider max value ({sliderMax}) overflowed probably");
			workerCountSlider.Setup(workersSelected, jobIndex, job.Workers, "workers", sliderMax, "");
			workerCountSlider.Show();
		} else {
			workerCountSlider.Hide();
		}

		titleLabel.Text = job.Title;

		var desc = job.GetStatusDescription();
		if (desc.Length != 0) desc += '\n';
		desc += job.GetProductionDescription();
		infoLabel.Text = desc;

		if (job.Locked) {
			DeleteJobButton.Disabled = true;
			workerCountSlider.Disable();
			titleLabel.Text += " (INACCESSIBLE)";
		}
	}

	void RemoveJob() {
		ui.RemoveJob(jobBox);
		UIRebuildRequested?.Invoke();
	}

	void JumpToJob() {
		JobFocusRequested?.Invoke();
	}

	public static uint GetSliderMax(Job job, uint maxFreeWorkers) {
		if (!job.NeedsWorkers) return 0;
		return Math.Min(maxFreeWorkers + (uint)job.Workers, job.MaxWorkers);
	}

}
