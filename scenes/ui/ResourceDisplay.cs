using Godot;

public partial class ResourceDisplay : PanelContainer {

	[Export] Label populationLabel;
	[Export] Label foodLabel;
	[Export] Label silverLabel;
	[Export] Label tileposLabel;
	[Export] Label timeLabel;
	[Export] Label factionLabel;
	[Export] Label worldTileInfoLabel;

	[Export] Label fpsLabel;


	public void Display(
		uint? population = null,
		uint? homelessPopulation = null,
		uint? unemployedPopulation = null,
		uint? silver = null,
		Faction faction = null,
		Vector2I? tilepos = null,
		(Vector2I, Region)? inRegionTilepos = null,
		string timeString = null,
		(uint, uint)? foodAndUsage = null,
		(float, float, float)? worldTileInfo = null
	) {
		fpsLabel.Text = "fps: " + Engine.GetFramesPerSecond().ToString();
		if (population != null && unemployedPopulation != null && homelessPopulation != null) {
			populationLabel.Text = $"pop: {population} ({homelessPopulation} homeless, {unemployedPopulation} unemployed)";
		}
		if (foodAndUsage != null) {
			foodLabel.Text = $"food: {foodAndUsage?.Item1} (usage {foodAndUsage?.Item2})";
		}
		if (timeString != null) timeLabel.Text = timeString;
		if (silver != null) silverLabel.Text = $"silver: {silver}";
		if (tilepos != null) tileposLabel.Text = $"{tilepos}";
		if (inRegionTilepos != null) {
			Vector2I pos = (Vector2I)(inRegionTilepos?.Item1);
			var reg = inRegionTilepos?.Item2;
			string txt = $"{pos}";
			if (reg.GroundTiles.TryGetValue(pos, out GroundTileType tile)) {
				txt += $" {tile.UIString()}";
				if (reg.HasMapObject(pos, out MapObject mopject)) {
					txt += $" with {(mopject.Type as IAssetType).AssetName}";
				}
			}
			tileposLabel.Text = txt;
		}
		if (faction != null) factionLabel.Text = $"region: {faction.Name}";
		if (worldTileInfo != null) {
			worldTileInfoLabel.Text = $"ele: {worldTileInfo?.Item1} temp: {worldTileInfo?.Item2} humi: {worldTileInfo?.Item3}";
		}
	}

}
