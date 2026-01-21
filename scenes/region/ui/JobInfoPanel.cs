using System;
using Godot;

namespace scenes.region.ui {

	public partial class JobInfoPanel : Control {

		public event Action UIRebuildRequested;

		public static readonly PackedScene Packed = GD.Load<PackedScene>("res://scenes/region/ui/job_info_panel.tscn");

		[ExportGroup("Nodes")]
		[Export] RichTextLabel infoLabel;
		[Export] Label titleLabel;
		[Export] Control sidebar;
		[Export] Control topbar;
		[Export] Container informationList;
		[Export] public Button DeleteJobButton;

		UI ui;
		Job jobBox;


		public void AddToTree(Node parent, bool showSide = false, bool showTop = false) {
			parent.AddChild(this);

			DeleteJobButton.Pressed += DeleteJob;
			sidebar.Visible = showSide;
			topbar.Visible = showTop;
		}

		public void Display(UI ui, Job job, int jobIndex, int sliderMax, Action<int, int> workersSelected) {
			this.ui = ui;
			jobBox = job;
			if (job.NeedsWorkers) {
				Debug.Assert(sliderMax >= 0, "Can't have negative slider values when needing workers");
				var slider = JobSlider.Instantiate();
				informationList.AddChild(slider);
				slider.Setup(workersSelected, jobIndex, job.Workers.Count, "workers", sliderMax, "");
			}

			titleLabel.Text = job.Title;

			infoLabel.Text =
				job.GetResourceRequirementDescription()
				+ "\n" + job.GetStatusDescription()
				+ "\n" + job.GetProductionDescription();
		}

		void DeleteJob() {
			ui.DeleteJob(jobBox);
			UIRebuildRequested?.Invoke();
		}

	}

}
