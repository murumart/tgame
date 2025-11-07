using System;
using Godot;

namespace scenes.region.ui {

	public partial class JobInfoPanel : Control {

		public static readonly PackedScene Packed = GD.Load<PackedScene>("res://scenes/region/ui/job_info_panel.tscn");

		[ExportGroup("Nodes")]
		[Export] RichTextLabel infoLabel;
		[Export] Label titleLabel;
		[Export] Control sidebar;
		[Export] Control topbar;
		[Export] Container informationList;


		public void AddToTree(Node parent, bool showSide = false, bool showTop = false) {
			parent.AddChild(this);
			sidebar.Visible = showSide;
			topbar.Visible = showTop;
		}

		public void Display(Job job, int jobIndex, int sliderMax, Action<int, int> workersSelected) {
			if (job.NeedsWorkers) {
				var slider = JobSlider.Instantiate();
				informationList.AddChild(slider);
				slider.Setup(workersSelected, jobIndex, job.Workers.Amount, "workers", sliderMax, "");
			}

			titleLabel.Text = job.Title;

			infoLabel.Text = job.GetResourceRequirementDescription();
		}

	}

}
