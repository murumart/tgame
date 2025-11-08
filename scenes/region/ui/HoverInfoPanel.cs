using Godot;
using scenes.region.buildings;
using System.Text;

namespace scenes.region.ui {

	public partial class HoverInfoPanel : PanelContainer {
		[Export] Label nameLabel;
		[Export] Label infoLabel;

		StringBuilder infoStr = new();

		public override void _Ready() {
			VisibilityChanged += () => SetProcess(Visible);
		}

		public override void _Process(double delta) {
			Display();
		}

		public void Display() {

		}

	}
}
