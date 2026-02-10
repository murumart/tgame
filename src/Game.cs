using System.Linq;

public class Game {

	public readonly Map Map;
	public readonly GameTime Time;
	public Region PlayRegion;

	readonly LocalAI[] regionAIs;


	public Game(Map map) {
		Map = map;
		Time = new();
		var regions = map.GetRegions();
		regionAIs = new LocalAI[regions.Length];
		for (int i = 0; i < regions.Length; i++) {
			if (regions[i].LocalFaction.GetPopulationCount() > 0) regionAIs[i] = new GamerAI(new(regions[i], regions[i].LocalFaction));
			else regionAIs[i] = new NatureAI(new(regions[i], regions[i].LocalFaction));
		}
	}

	TimeT _lastAIUpdate;
	public void PassTime(TimeT minutes) {
		Map.PassTime(minutes);
		Time.PassTime(minutes);
		if (Time.Minutes > GameTime.Hours(8)) {
			if (Time.Minutes - _lastAIUpdate >= 30) {
				var regs = Map.GetRegions();
				for (int i = 0; i < regionAIs.Length; i++) {
					if (regs[i] == PlayRegion) continue;
					regionAIs[i].PreUpdate(Time.Minutes);
					regionAIs[i].Update(Time.Minutes);
				}
				_lastAIUpdate = Time.Minutes;
			}
		}
	}

}