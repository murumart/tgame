using System;
using System.Collections.Generic;
using Godot;
using scenes.autoload;

public class Population {

	public event Func<uint, uint> FoodRequested;
	public event Func<int> SilverRequested;
	public event Func<float> FurnitureRateRequested;
	public event Action<Job, int> JobEmploymentChanged;
	public event Action ApprovalDroppedToZero;

	public uint Count { get; private set; }

	public uint HousedCount { get; private set; }
	public uint MaxHousing { get; private set; }

	public uint EmployedCount { get; private set; }
	public uint MaxEmployed => Count;

	public float Food { get; private set; }
	public float Hunger { get; private set; }
	public bool ArePeopleStarving => Hunger > 0f;
	const float HUNGER_KILL_LIMIT = 2f;

	readonly HashSet<Job> employedOnJobs = new();

	public float OngrowingPopulation { get; private set; }

	public float Approval { get; private set; }

	TimeT time;


	public Population() {
		Approval = 0.5f;
	}

	public void PassTime(TimeT minutes) {
		EatFood(minutes);

		OngrowingPopulation += (GetYearlyBirths() * minutes) / GameTime.Years(1);
		while (OngrowingPopulation >= 1f) {
			OngrowingPopulation -= 1f;
			Manifest(1);
		}

		Approval = Mathf.Clamp(Approval + (GetApprovalMonthlyChange() * minutes) / GameTime.Months(1), 0f, 1f);
		if (Approval == 0f) ApprovalDroppedToZero?.Invoke();
	}

	public void EatFood(TimeT minutesPassed) {
		float wantfood = Faction.GetFoodUsageS(Count, EmployedCount);
		// one unit of food is how much one person eats per day
		uint ONCE_PER_DAY = (uint)((GameTime.Days(1)) * 1);
		wantfood /= ONCE_PER_DAY;

		for (uint i = 0; i < minutesPassed; i++) {
			if (Food < wantfood) {
				Food += FoodRequested?.Invoke((uint)Mathf.Ceil(wantfood - Food)) ?? 0;
			}

			var oldfood = Food;
			if (Food < wantfood) {
				Food = 0;
				Hunger += wantfood;
				while (Hunger > HUNGER_KILL_LIMIT) {
					Hunger -= HUNGER_KILL_LIMIT;
					uint reduction = 1;
					Reduce(reduction);
				}
			} else {
				Food -= wantfood;
				if (Hunger > 0) {
					if (Food > Hunger) {
						Food -= Hunger;
						Hunger = 0;
					} else {
						Hunger -= Food;
						Food = 0;
					}
				}
			}
			Debug.Assert(Food <= oldfood, $"Somehow, food increased while eating ({Food} was {oldfood} food)?");
		}
	}

	public void Manifest(uint count) {
		Count += count;
		UpdateHousing();
	}

	public void Reduce(uint count) {
		Count -= count;
		UpdateHousing();
		// after people are removed, it may happen that EmployedCount is more than Count
		// so this is where that's fixed
		if (EmployedCount > Count) {
			uint overlimit = EmployedCount - Count; // how many people are "employed" despite not existing
			while (overlimit > 0) {
				bool removed = false;
				foreach (Job job in employedOnJobs) {
					if (job.Workers == 0) continue;
					Unemploy(job, 1); // this modifies EmployedCount
					removed = true;
				}
				// sanity check with ints to avoid overflow into a bajillion
				Debug.Assert((int)EmployedCount - (int)Count != overlimit, "More people should have become unemployed, but none did?");
				Debug.Assert((int)EmployedCount - (int)Count <= overlimit, $"Somehow as a consequence of removal more people became employed? (now {EmployedCount})");
				if (!removed || EmployedCount <= Count) break; // ran out of people to unjob
				overlimit = EmployedCount - Count;
			}
			Debug.Assert(Count >= EmployedCount, $"Somehow more people are employed than are actually alive here ({EmployedCount} vs {Count})");
		}
	}

	public void ChangeHousingCapacity(int by) {
		Debug.Assert(MaxHousing + by >= 0, $"MaxHousing should be more than 0 (ends up {MaxHousing + by})");
		if (by >= 0) MaxHousing += (uint)by;
		else MaxHousing -= (uint)-by;
		UpdateHousing();
	}

	public void UpdateHousing() {
		HousedCount = Math.Min(Count, MaxHousing);
		Debug.Assert(HousedCount <= Count, $"More housed people than people extant?? ({HousedCount} vs {Count})");
	}

	public void Employ(Job job, uint amount) {
		Debug.Assert(job.Workers + amount <= job.MaxWorkers, $"{job} out of capacity ({job.Workers}, {job.MaxWorkers}) to fit {amount} extra workers");
		Debug.Assert(amount + EmployedCount <= MaxEmployed, $"Population out of capacity ({EmployedCount}, {MaxEmployed}) to fit {amount} extra workers");
		EmployedCount += amount;
		job.SetWorkers(job.Workers + (int)amount);
		if (job.Workers > 0) employedOnJobs.Add(job);
		Debug.Assert(job.Workers >= 0, $"Job can't have negative workers ({job.Workers})");
		JobEmploymentChanged?.Invoke(job, (int)amount);
	}

	public void Unemploy(Job job, uint amount) {
		Debug.Assert(job.Workers - amount >= 0, $"Can't unemploy more workers ({-amount}) than exist in job");
		var oldemployed = EmployedCount;
		Debug.Assert(EmployedCount >= amount, $"Can't unemploy more workers ({-amount}) than exist in population");
		EmployedCount -= amount;
		Debug.Assert(EmployedCount <= oldemployed, $"Somehow, more people became employed as a result of unemploying them (old {oldemployed} new {EmployedCount})");
		job.SetWorkers(job.Workers - (int)amount);
		if (job.Workers == 0) employedOnJobs.Remove(job);
		Debug.Assert(job.Workers >= 0, $"Job can't have negative workers ({job.Workers})");
		JobEmploymentChanged?.Invoke(job, -(int)amount);
	}

	public float GetYearlyBirths() {
		if (ArePeopleStarving) return 0f;
		float lessthanpop = Mathf.Max(0, (float)Count - 1f);
		float lessthanhoused = Mathf.Max(0, (float)HousedCount - 1f);
		return (lessthanpop - lessthanhoused) * 0.05f + (lessthanhoused) * 0.5f;
	}

	(float, string)[] reasons = null;
	void UpdateApprovalMonthlyChangeReasons() {
		if (reasons == null) {
			reasons = new (float, string)[5];
			reasons[0].Item2 = "no people";
			reasons[1].Item2 = "people are starving";
			reasons[2].Item2 = "not enough housing for people";
			reasons[3].Item2 = "your coffers are";
			reasons[4].Item2 = "furnidur";
		}
		if (Count == 0) {
			return;
		}
		reasons[1].Item1 = ArePeopleStarving ? -15f : 0f;
		reasons[2].Item1 = -((float)Count - HousedCount) / Count * 0.5f;
		int silver = SilverRequested?.Invoke() ?? 0;
		float silverapprovalchange = 0.05f * Mathf.Ease(Mathf.Clamp(silver - 25, -25, 50) / 50f, -2f);
		reasons[3].Item1 = silverapprovalchange;
		reasons[3].Item2 = silverapprovalchange > 0.005f ? "your coffers are full" : silverapprovalchange < -0.005f ? "your faction is poor" : "your budget is just okay";
		float furnitureapprovalchange = 0.4f * Mathf.Clamp(Mathf.Pow((FurnitureRateRequested?.Invoke() ?? 0f - 0.1f), 3) / 0.8f, -0.16f, 0.16f);
		reasons[4].Item1 = furnitureapprovalchange;
		reasons[4].Item2 = furnitureapprovalchange > 0f ? "people have furniture in their homes" : "people are missing furniture in their homes";
	}

	public (float, string)[] GetApprovalMonthlyChangeReasons() => reasons;

	public float GetApprovalMonthlyChange() {
		UpdateApprovalMonthlyChangeReasons();
		if (Count == 0) return 0f;
		float a = 0f;
		foreach (var f in reasons) {
			a += f.Item1;
		}
		return a;
	}

}
