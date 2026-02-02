public class Game {

	public readonly Map Map;
	public readonly GameTime Time;
	public readonly Region PlayRegion;


	public Game(Region playRegion, Map map) {
		Map = map;
		Time = new();
		PlayRegion = playRegion;
	}

	public void PassTime(TimeT minutes) {
		Map.PassTime(minutes);
		Time.PassTime(minutes);
	}

}