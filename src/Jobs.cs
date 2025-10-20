using Godot;

namespace Jobs {

	// abstract jobs

	public abstract class Job : ITimePassing {
		public abstract void PassTime(float hours); // if repeating: add whatever overflows in completoin
		public abstract void Finish();
		public virtual void Start() { }
	}

	public interface ICompletableJob {
		public float GetCompletion();
		public void AddCompletion(float amount);
	}

	public interface IPopulatableJob {
		public ref Population GetPopulation();
	}

	public abstract class CompletableJob : Job, ICompletableJob, IPopulatableJob {
		protected float completion;
		protected Population population;

		public void AddCompletion(float amount) {
			completion += amount;
		}

		public float GetCompletion() {
			return completion;
		}

		public ref Population GetPopulation() {
			return ref population;
		}
	}

	// concrete jobs

	public class AbsorbFromHomelessPopulationJob : Job {
		Building building;
		Region region;

		public AbsorbFromHomelessPopulationJob(Building building, Region region) {
			this.building = building;
			this.region = region;
		}

		public override void Finish() { }

		float remainderPeopleTransferTime;
		public override void PassTime(float hours) {
			if (building.IsConstructed && region.HomelessPopulation.Pop > 0) {
				/* if (homelessPopulation.CanTransfer(ref building.Population, 1)) {
					homelessPopulation.Transfer(ref building.Population, 1);
				} */
				float peopleTransferTime = hours + remainderPeopleTransferTime;
				while (peopleTransferTime > 0.1 && region.HomelessPopulation.CanTransfer(ref building.Population, 1)) {
					region.HomelessPopulation.Transfer(ref building.Population, 1);
					peopleTransferTime -= 0.1f;
				}
				remainderPeopleTransferTime = peopleTransferTime;
			} else {
				remainderPeopleTransferTime = 0.0f;
			}
		}
	}

	public class BuildingJob : CompletableJob {
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
}