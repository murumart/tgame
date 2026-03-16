using System.Linq;

public class Game {

	public readonly Map Map;
	public readonly GameTime Time;
	public Region PlayRegion;

	public bool AIPlaysInPlayerRegion = false;

	readonly LocalAI[] regionAIs;
	readonly TroubleMaker troubleMaker;


	public Game(Map map) {
		Map = map;
		Time = new();
		var regions = map.GetRegions();
		regionAIs = new LocalAI[regions.Length];
		for (int i = 0; i < regions.Length; i++) {
			if (regions[i].LocalFaction.IsWild) regionAIs[i] = new NatureAI(new(regions[i], regions[i].LocalFaction));
			else regionAIs[i] = new GamerAI(new(regions[i], regions[i].LocalFaction));
		}
		troubleMaker = new(this, map);
	}

	TimeT _lastAIUpdate = 0;
	public void PassTime(TimeT minutes) {
		Map.PassTime(minutes);
		Time.PassTime(minutes);
		if (Time.Minutes < GameTime.Hours(8)) return;

		while (Time.Minutes - _lastAIUpdate >= 15) {
			troubleMaker.Update();
			var regs = Map.GetRegions();
			for (int i = 0; i < regionAIs.Length; i++) {
				if (regs[i] == PlayRegion && !AIPlaysInPlayerRegion) continue;
				regionAIs[i].PreUpdate(Time.Minutes);
				regionAIs[i].Update(Time.Minutes);
			}
			_lastAIUpdate += 15;
		}
	}

	public LocalAI GetRegionAI(Region region) {
		Debug.Assert(region.WorldIndex >= 0 && region.WorldIndex < regionAIs.Length, $"{region.WorldIndex} vs {regionAIs.Length}");
		return regionAIs[region.WorldIndex];
	}

}