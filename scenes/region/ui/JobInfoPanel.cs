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
		JobBox jobBox;


		public void AddToTree(Node parent, bool showSide = false, bool showTop = false) {
			parent.AddChild(this);

			DeleteJobButton.Pressed += DeleteJob;
			sidebar.Visible = showSide;
			topbar.Visible = showTop;
		}

		public void Display(UI ui, JobBox jbox, int jobIndex, int sliderMax, Action<int, int> workersSelected) {
			this.ui = ui;
			jobBox = jbox;
			if (jbox.NeedsWorkers) {
				var slider = JobSlider.Instantiate();
				informationList.AddChild(slider);
				slider.Setup(workersSelected, jobIndex, jbox.Workers.Count, "workers", sliderMax, "");
			}

			titleLabel.Text = jbox.Title;

			infoLabel.Text =
				jbox.GetResourceRequirementDescription()
				+ "\n" + jbox.GetStatusDescription()
				+ "\n" + jbox.GetProductionDescription();
		}

		void DeleteJob() {
			ui.DeleteJob(jobBox);
			UIRebuildRequested?.Invoke();
		}

	}

}
