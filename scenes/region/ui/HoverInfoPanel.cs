using Godot;
using scenes.region.buildings;
using System.Text;

namespace scenes.region.ui {

	public partial class HoverInfoPanel : PanelContainer {
		[Export] Label nameLabel;
		[Export] Label infoLabel;
		BuildingView display;
		StringBuilder infoStr = new();

		public override void _Ready() {
			VisibilityChanged += () => SetProcess(Visible);
		}

		public override void _Process(double delta) {
			Display();
		}

		public void Display(BuildingView buildingView) {
			display = buildingView;
			Display();
		}

		public void Display() {
			if (display == null) return;
			Position = GetGlobalMousePosition(); // hacky
			nameLabel.Text = display.Building.Type.Name;
			infoStr.Clear();
			var bld = display.Building;
			if (!display.Building.IsConstructed) {
				infoStr.Append("Construction progress: ").Append((int)(display.Building.GetBuildProgress() * 100)).Append('%');
			} else {
				if (bld.Type.GetPopulationCapacity() > 0) {
					infoStr.Append("Residents: ").Append(bld.Population.Amount).Append('/').Append(bld.Population.MaxPop);
				}
			}
			infoLabel.Text = infoStr.ToString();
		}

	}
}
