using Godot;

// abstract jobs

public abstract class Job : ITimePassing {
	protected float completion; public float Completion { get => completion; }
	protected Population population; public Population Population { get => population; }

	public abstract void PassTime(float hours); // if repeating: add whatever overflows in completoin
	public abstract void Finish();
	public virtual void Start() { }
}

public abstract class TileJob : Job {
	Vector2I tilePosition; public Vector2I TilePosition { get => tilePosition; }
}

// concrete jobs

public class BuildingJob : TileJob {
	Building building;

	public override void Finish() {

	}

	public override void PassTime(float hours) {
		completion += hours;
		if (completion >= 1.0) {
			Finish();
		}
	}
}