// game master
using scenes.autoload;

public class TroubleMaker {

	readonly Game game;
	readonly Map map;
	Region PlayerRegion => game.PlayRegion;
	Faction PlayerFaction => game.PlayRegion.LocalFaction;

	float playerTension = 0f;


	public TroubleMaker(Game game, Map map) {
		this.game = game;
		this.map = map;

        UILayer.DebugDisplay(() => "tension: " + playerTension, "tension");
	}

	public void Update() {

	}

	void CreateFishingAccident(Faction faction) {
		foreach (var job in faction.GetJobs()) {
			if (job is GatherResourceJob gjob && gjob.Site.Type == Registry.ResourceSitesS.FishingSpot) {
				var localpos = gjob.GlobalPosition - faction.Region.WorldPosition;
				if (!faction.HasProblem(localpos)) {
					var problem = new FishingBoatProblem(localpos, gjob);
					faction.AddProblem(problem, localpos);
					break;
				}
			}
		}
        if (faction == PlayerFaction) playerTension += 1f;
	}

}
