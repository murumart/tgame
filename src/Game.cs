using System;

public class Game {

	public readonly Map Map;
	public readonly GameTime Time;
	public Region PlayRegion { get; private set; }

	public bool AIPlaysInPlayerRegion = false;

	readonly (LocalAI, TimeT)[] regionAIs;
	public TroubleMaker TroubleMaker { get; private set; }


	public Game(Map map) {
		Map = map;
		Time = new();
		var regions = map.GetRegions();
		regionAIs = new (LocalAI, TimeT)[regions.Length];
		for (int i = 0; i < regions.Length; i++) {
			if (regions[i].LocalFaction.IsWild) regionAIs[i] = (new NatureAI(new(regions[i], regions[i].LocalFaction)), 0);
			else regionAIs[i] = (new GamerAI(new(regions[i], regions[i].LocalFaction)), Random.Shared.Next(15));
		}
		TroubleMaker = new(map);
	}

	public void SetPlayRegion(Region region) {
		PlayRegion = region;
		TroubleMaker.SetPlayRegion(region);
	}

	public void PassTime(TimeT minutes) {
		Map.PassTime(minutes);
		Time.PassTime(minutes);
		if (Time.Minutes < GameTime.Hours(8)) return;

		TroubleMaker.Update(); // not synced with time, but it's ok because it exists outside of time
		for (int i = 0; i < regionAIs.Length; i++) {
			while (Time.Minutes - regionAIs[i].Item2 >= 15) {
				var regs = Map.GetRegions();
				if (regs[i] == PlayRegion && !AIPlaysInPlayerRegion) break;
				if (regs[i].LocalFaction.GetPopulationCount() == 0) break; // we are dead here
				regionAIs[i].Item1.PreUpdate(Time.Minutes);
				regionAIs[i].Item1.Update(Time.Minutes);
				regionAIs[i].Item2 += 15;
			}
		}
	}

	public LocalAI GetRegionAI(Region region) {
		Debug.Assert(region.WorldIndex >= 0 && region.WorldIndex < regionAIs.Length, $"{region.WorldIndex} vs {regionAIs.Length}");
		return regionAIs[region.WorldIndex].Item1;
	}

	public void Deinit() {
		TroubleMaker.Deinit();
		TroubleMaker = null;
	}
}