using Godot;
using scenes.region.view.buildings;
using System;
using System.Text;

public partial class TileInfoPanel : PanelContainer {
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
		nameLabel.Text = display.BuildingType.Name;
		infoStr.Clear();
		var bld = display.Building;
		if (display.Building.ConstructionProgress < 1.0) {
			infoStr.Append("Construction progress: ").Append((int)(bld.ConstructionProgress * 100)).Append('%');
		} else {
			if (bld.Type.PopulationCapacity > 0) {
				infoStr.Append("Residents: ").Append(bld.Population).Append('/').Append(bld.Type.PopulationCapacity);
			}
		}
		infoLabel.Text = infoStr.ToString();
	}

}
