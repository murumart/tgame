public class Game {
	public readonly Map Map;
	public readonly GameTime Time;

	public Game(Map map) {
		Map = map;
		Time = new();
	}

	public void PassTime(TimeT minutes) {
		Map.PassTime(minutes);
		Time.PassTime(minutes);
	}
}