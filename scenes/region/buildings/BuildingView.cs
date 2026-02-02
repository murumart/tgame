using Godot;
using scenes.autoload;

namespace scenes.region.buildings {

	[GlobalClass]
	public partial class BuildingView : MapObjectView {

		[Export] ProgressBar buildingProgressBar;
		[Export] Node2D inProgressDisplay;


		public void DisplayBuildingProgress(float progress, bool show = true) {
			Debug.Assert(progress >= 0f && progress <= 1f, "Progress bar value aout of range");
			buildingProgressBar.Visible = show;
			buildingProgressBar.Value = progress;
			inProgressDisplay.Visible = show;
		}

	}

}
