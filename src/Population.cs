using System;
using System.Collections.Generic;
using Godot;

public class Population {

	public event Func<uint, uint> FoodRequested;
	// this feels kind of ugly but something event bussy is useful
	public event Action<Job, int> JobEmploymentChanged;

	public uint Count { get; private set; }

	public uint HousedCount { get; private set; }
	public uint MaxHousing { get; private set; }

	public uint EmployedCount { get; private set; }
	public uint MaxEmployed => Count;

	public float Food { get; private set; }
	public float Hunger { get; private set; }
	public bool ArePeopleStarving => Hunger > 0f;
	const float HUNGER_KILL_LIMIT = 4f;

	readonly HashSet<Job> employedOnJobs = new();

	TimeT time;


	public Population() {
	}

	public void PassTime(TimeT minutes) {
		for (TimeT i = 0; i < minutes; i++) {
			EatFood(1);
		}
	}

	public void EatFood(TimeT minutesPassed) {
		float wantfood = FactionActions.GetFoodUsageS(Count, EmployedCount);
		uint twiceperday =  (uint)((GameTime.HOURS_PER_DAY * GameTime.MINUTES_PER_HOUR / 2) * minutesPassed);
		wantfood /= twiceperday; // "two meals" per day

		if (Food < wantfood) {
			Food += FoodRequested?.Invoke((uint)Mathf.Ceil(wantfood - Food)) ?? 0;
		}

		var oldfood = Food;
		if (Food < wantfood) {
			Food = 0;
			Hunger += wantfood - Food;
			while (Hunger > HUNGER_KILL_LIMIT) {
				Hunger -= HUNGER_KILL_LIMIT;
				uint reduction = 1;
				Reduce(reduction);
				GD.Print($"Population::EatFood : {reduction} people starved no food");
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
				GD.Print($"Population::EatFood : leftover hunger sated ({Food} food)");
			}
		}
		Debug.Assert(oldfood >= Food, $"Somehow, food increased while eating ({Food} was {oldfood} food)?");
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

}
