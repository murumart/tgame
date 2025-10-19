using Godot;
using System;

namespace scenes.region.view {

	public partial class JobInfoPanel : Control {
		public static readonly PackedScene Packed = GD.Load<PackedScene>("res://scenes/region/view/job_infopanel.tscn");

		[Export] RichTextLabel infoLabel;
		[Export] Control sidebar;
		[Export] Control topbar;

		public void AddToTree(Node parent, bool showSide = false, bool showTop = false) {
			parent.AddChild(this);
			sidebar.Visible = showSide;
			topbar.Visible = showTop;
		}
	}
}
