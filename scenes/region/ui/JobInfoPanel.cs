using Godot;
using System;
using System.Text;

namespace scenes.region.ui {

	public partial class JobInfoPanel : Control {

		public static readonly PackedScene Packed = GD.Load<PackedScene>("res://scenes/region/ui/job_info_panel.tscn");

		[ExportGroup("Nodes")]
		[Export] RichTextLabel infoLabel;
		[Export] Label titleLabel;
		[Export] Control sidebar;
		[Export] Control topbar;
		[Export] RichTextLabel informationText;
		[Export] Container informationList;


		public void AddToTree(Node parent, bool showSide = false, bool showTop = false) {
			parent.AddChild(this);
			sidebar.Visible = showSide;
			topbar.Visible = showTop;
		}

		public void Display(Job job) {
			titleLabel.Text = job.Title;

			infoLabel.Text = job.GetResourceRequirementDescription();

		}

	}

}
