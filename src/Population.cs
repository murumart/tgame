
using System;

public class Population {

	public int Count { get; private set; }

	public int HousedCount { get; private set; }
	public int MaxHousing { get; private set; }

	public int EmployedCount { get; private set; }
	public int MaxEmployed => Count;


	public Population() {
	}

	public void Manifest(int count) {
		Count += count;
		UpdateHousing();
	}

	public void Reduce(int count) {
		Debug.Assert(false, "Reducing population not implemented");
		Count -= count;
		UpdateHousing();
	}

	public void ChangeHousingCapacity(int by) {
		Debug.Assert(MaxHousing + by >= 0, $"MaxHousing should be more than 0 (ends up {MaxHousing + by})");
		MaxHousing += by;
		UpdateHousing();
	}

	public void UpdateHousing() {
		HousedCount = Math.Min(Count, MaxHousing);
		Debug.Assert(HousedCount <= Count, $"More housed people than people extant?? ({HousedCount} vs {Count})");
	}

	public void Employ(Job job, int amount) {
		Debug.Assert(job.Workers + amount <= job.MaxWorkers, $"{job} out of capacity ({job.Workers}, {job.MaxWorkers}) to fit {amount} extra workers");
		Debug.Assert(amount + EmployedCount <= MaxEmployed, $"Population out of capacity ({EmployedCount}, {MaxEmployed}) to fit {amount} extra workers");
		EmployedCount += amount;
		job.SetWorkers(job.Workers + amount);
	}

	public void Unemploy(Job job, int amount) {
		Debug.Assert(job.Workers - amount >= 0, $"Can't unemploy more workers ({-amount}) than exist in job");
		Debug.Assert(EmployedCount - amount >= 0, $"Can't unemploy more workers ({-amount}) than exist in population");
		EmployedCount -= amount;
		job.SetWorkers(job.Workers - amount);
	}

}
