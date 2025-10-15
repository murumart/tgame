public class Game {
	public readonly Map Map;
	float time;

	public Game(Map map) {
		Map = map;
	}

	public void PassTime(float secs) {
		Map.PassTime(secs);
	}
}