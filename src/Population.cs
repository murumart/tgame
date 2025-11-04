using System;

public struct Population {

	private int pop;
	public int Pop {
		readonly get => pop;
		set {
			Debug.Assert(value <= MaxPop, $"People overflow ({value} vs {MaxPop})");
			Debug.Assert(value >= 0, $"People underflow ({value} vs 0)");
			pop = value;
		}
	}
	public readonly int MaxPop;


	public Population(int maxPop) {
		Debug.Assert(maxPop > -1, "need max population to be positive or 0");
		this.MaxPop = maxPop;
	}

	public Population(ref Population population, int maxPop) : this(maxPop) {
		Pop = population.Pop;
	}

	public readonly bool CanAdd(int amt) {
		return Pop + amt <= MaxPop && Pop + amt >= 0;
	}

	public readonly bool CanTransfer(ref Population other, int amt) {
		return other.CanAdd(amt) && CanAdd(-amt);

	}

	public Population Transfer(ref Population dest, int maxAmt) {
		int amt = Math.Min(Pop, maxAmt);
		amt = Math.Min(dest.MaxPop - dest.Pop, amt);
		pop -= amt;
		dest.Pop += amt;
		return this;
	}


}
