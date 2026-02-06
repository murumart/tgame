using System;
using Godot;

namespace scenes.region.ui {

	public partial class JobInfoPanel : Control {

		public event Action UIRebuildRequested;

		[ExportGroup("Nodes")]
		[Export] RichTextLabel infoLabel;
		[Export] Label titleLabel;
		[Export] Container informationList;
		[Export] public Button DeleteJobButton;
		[Export] JobSlider workerCountSlider;

		UI ui;
		Job jobBox;


		public override void _Ready() {
			DeleteJobButton.Pressed += DeleteJob;
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
		}

		void DeleteJob() {
			ui.DeleteJob(jobBox);
			UIRebuildRequested?.Invoke();
		}

	}

}
