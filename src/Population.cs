using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

public readonly struct Person {

	public readonly ulong Id;

	public Person(Population p, ulong id) {
		Debug.Assert(p.ExistsId(id), "This person doesn't exist");
		this.Id = id;
	}

	public override bool Equals([NotNullWhen(true)] object obj) {
		if (obj is Person person) {
			return this.Id == person.Id;
		}
		return false;
	}

	public static bool operator ==(Person left, Person right) {
		return left.Equals(right);
	}

	public static bool operator !=(Person left, Person right) {
		return !(left == right);
	}

	public override int GetHashCode() {
		return Id.GetHashCode();
	}
}

public class Population {

	class Field<T> {

		static int dirtyAccesses = 0;
		static int touches = 0;

		bool dirty = true;
		T value;
		public T Value {
			get {
				if (dirty) {
					dirtyAccesses++;
					GD.Print("value is dirty, calgulating");
					value = getval();
					dirty = false;
				}
				return value;
			}
		}
		readonly Func<T> getval;

		public Field(Func<T> get) { this.getval = get; }
		public void Touch() {
			dirty = true;
			touches++;
		}

	}

	static ulong personId = 0;

	class TablePerson {

		ulong id;
		public Building livesAt = null;
		public Job worksAt = null;
		public bool isAlive = true;

	}

	readonly Dictionary<ulong, TablePerson> individs;

	readonly Field<int> homelessCount;
	public int HomelessCount => homelessCount.Value;
	readonly Field<int> unemployedCount;
	public int UnemployedCount => unemployedCount.Value;
	readonly Field<int> aliveCount;
	public int AliveCount => aliveCount.Value;


	public Population() {
		individs = new();

		homelessCount = new(() => {
			return individs.Keys.Where(i => !IsHoused(new(this, i))).Count();
		});
		unemployedCount = new(() =>{
			int c = 0;
			foreach (var k in individs.Keys) if (!IsEmployed(new(this, k))) c++;
			return c;
		});
		aliveCount = new(() => individs.Keys.Where(i => IsAlive(new(this, i))).Count());
	}

	public Person GetUnemployed() {
		var unempCount = unemployedCount.Value;
		Debug.Assert(unempCount > 0, "There are no unemployed people");
		foreach (var (id, p) in individs) {
			if (p.worksAt == null) return new(this, id);
		}
		throw new UnreachableException();
	}

	public Person GetHomeless() {
		Debug.Assert(HomelessCount > 0, "There are no homeless people");
		foreach (var (id, p) in individs) {
			if (p.livesAt == null) return new(this, id);
		}
		throw new UnreachableException();
	}

	public void Manifest(int amount) {
		for (int i = 0; i < amount; i++) {
			individs[personId++] = new();
		}
	}

	public void Employ(Job job, int amount) {
		Debug.Assert(amount > 0, $"Can't employ less than 1 person (use Unemploy to fire them)");
		Debug.Assert(UnemployedCount >= amount, $"There aren't enough unemployed people (wanted {amount} but have {UnemployedCount})");
		Debug.Assert(job.Workers.RoomLeft >= amount, $"Not enough room left in the job for people (wanted {amount} but have {job.Workers.RoomLeft})");
		for (int i = 0; i < amount; i++) {
			var u = GetUnemployed();
			individs[u.Id].worksAt = job;
			job.Workers.Add(u);
		}
		unemployedCount.Touch();
	}

	public void Unemploy(Job job, int amount) {
		Debug.Assert(amount > 0, $"Can't unemploy less than 1 person (use Employ to hire them)");
		Debug.Assert(job.Workers.Count >= amount, $"Not enough workers on job to fire (wanted {amount} but have {job.Workers.RoomLeft})");
		for (int i = 0; i < amount; i++) {
			var u = job.Workers.Remove();
			individs[u.Id].worksAt = null;
		}
		unemployedCount.Touch();
	}

	public void House(Building building, int amount) {
		Debug.Assert(building.Type.GetPopulationCapacity() > 0, $"This building doesn't house people");
		Debug.Assert(HomelessCount >= amount, $"There aren't enough unemployed people (wanted {amount} but have {HomelessCount})");
		Debug.Assert(building.Population.RoomLeft >= amount, $"Not enough room left in the job for people (wanted {amount} but have {building.Population.RoomLeft})");
		for (int i = 0; i < amount; i++) {
			var u = GetHomeless();
			individs[u.Id].livesAt = building;
			building.Population.Add(u);
		}
		homelessCount.Touch();
	}

	public void Unhouse(Building building, int amount) {
		Debug.Assert(building.Population.Count >= amount, $"Not enough people to throw on the street (wanted {amount} but have {building.Population.RoomLeft})");
		for (int i = 0; i < amount; i++) {
			var u = building.Population.Remove();
			individs[u.Id].livesAt = null;
		}
		homelessCount.Touch();
	}

	public void Unhouse(Building building, Person person) {
		Debug.Assert(building.Population.Contains(person), $"This person isn't contained in the building");
		if (!Exists(person)) {
			GD.PushWarning($"This person {person} does not exist");
		} else {
			Debug.Assert(individs[person.Id].livesAt == building, "This person doesn't live at this building....");
		}
		building.Population.Remove(person);
		homelessCount.Touch();
	}

	public int Count => AliveCount;

	internal bool ExistsId(ulong id) => individs.ContainsKey(id);
	public bool Exists(Person p) => individs.ContainsKey(p.Id);
	public bool IsAlive(Person p) => individs[p.Id].isAlive;
	public bool IsEmployed(Person p) => individs[p.Id].worksAt != null;
	public bool IsHoused(Person p) => individs[p.Id].livesAt != null;

}

public class Group {

	public readonly int Capacity;
	readonly List<Person> people;

	public int Count => people.Count;
	public int RoomLeft => Capacity - Count;


	public Group(int capacity) {
		Debug.Assert(capacity > 0, "Useless to make a group with 0 capacity");
		people = new();
		Capacity = capacity;
	}

	public bool Contains(Person person) {
		foreach (var p in people) if (p == person) return true;
		return false;
	}

	public void Add(Person person) {
		Debug.Assert(CanAdd(1), "Can't fit this person");
		Debug.Assert(!Contains(person), "This person is already in this group... bad....");
		people.Add(person);
	}

	public Person Remove() {
		Debug.Assert(Count > 0, "It's empty");
		var p =  people[^1];
		people.RemoveAt(Count - 1);
		return p;
	}

	public void Remove(Person person) {
		Debug.Assert(Contains(person), "Don't have this person in this place");
		for (int i = Count - 1; i > -1; i -= 1) {
			if (people[i] == person) {
				people.RemoveAt(i);
				return;
			}
		}
	}

	public bool CanAdd(int amount) {
		return Count + amount <= Capacity;
	}

}

