using Godot;

public partial class ResourceDisplay : PanelContainer {

	[Export] Label populationLabel;
	[Export] Label silverLabel;
	[Export] Label tileposLabel;
	[Export] Label timeLabel;
	[Export] Label regionLabel;

	[Export] Label fpsLabel;


	public void Display(
		int? population = null,
		int? homelessPopulation = null,
		int? unemployedPopulation = null,
		int? silver = null,
		Region region = null,
		Vector2I? tilepos = null,
		string timeString = null
	) {
		fpsLabel.Text = "fps: " + Engine.GetFramesPerSecond().ToString();
		if (population != null && unemployedPopulation != null && homelessPopulation != null) {
			populationLabel.Text = $"pop: {population} ({homelessPopulation} homeless, {unemployedPopulation} unemployed)";
		}
		if (timeString != null) timeLabel.Text = timeString;
		if (silver != null) silverLabel.Text = $"silver: {silver}";
		if (tilepos != null) tileposLabel.Text = $"{tilepos}";
		if (region != null) regionLabel.Text = $"region: {region}";
	}

}
