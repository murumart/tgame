using Godot;
using Jobs;
using System;

namespace scenes.region.ui {

	public partial class JobInfoPanel : Control {

		public static readonly PackedScene Packed = GD.Load<PackedScene>("res://scenes/region/view/job_infopanel.tscn");

		[ExportGroup("Nodes")]
		[Export] RichTextLabel infoLabel;
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
			
		}

	}

}
