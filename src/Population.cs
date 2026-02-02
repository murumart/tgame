using System;
using Godot;

public class Population {

	public event Func<uint> FoodRequested;

	public uint Count { get; private set; }

	public uint HousedCount { get; private set; }
	public uint MaxHousing { get; private set; }

	public uint EmployedCount { get; private set; }
	public uint MaxEmployed => Count;

	public uint Food { get; private set; }

	TimeT time;


	public Population() {
	}

	public void PassTime(TimeT minutes) {
		for (TimeT i = 0; i < minutes; i++) {
			var oldhour = GameTime.GetDayHourS(time);
			time += 1;
			var newhour = GameTime.GetDayHourS(time);
			if (oldhour != newhour) {
				if (newhour == 8 || newhour == 12 || newhour == 16) {
					EatFood();
				}
			}
		}
	}

	public void EatFood() {
		GD.Print("Population::EatFood : Eeating food");
	}

	public void Manifest(uint count) {
		Count += count;
		UpdateHousing();
	}

	public void Reduce(uint count) {
		Debug.Assert(false, "Reducing population not implemented");
		Count -= count;
		UpdateHousing();
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
		Debug.Assert(job.Workers >= 0, $"Job can't have negative workers ({job.Workers})");
	}

	public void Unemploy(Job job, uint amount) {
		Debug.Assert(job.Workers - amount >= 0, $"Can't unemploy more workers ({-amount}) than exist in job");
		Debug.Assert(EmployedCount - amount >= 0, $"Can't unemploy more workers ({-amount}) than exist in population");
		EmployedCount -= amount;
		job.SetWorkers(job.Workers - (int)amount);
		Debug.Assert(job.Workers >= 0, $"Job can't have negative workers ({job.Workers})");
	}

}
