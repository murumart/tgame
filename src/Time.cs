using System;
using Godot;

/// <summary>
/// It's an integer time unit!! Trust me...
/// </summary>
public struct TimeT {

	private ulong S { init; get; }


	public static TimeT operator +(TimeT m) => m;
	public static TimeT operator +(TimeT a, TimeT b) => a.S + b.S;
	public static TimeT operator *(TimeT a, TimeT b) => a.S * b.S;
	public static TimeT operator %(TimeT a, TimeT b) => a.S % b.S;

	public static implicit operator ulong(TimeT m) => m.S;
	public static implicit operator TimeT(ulong l) => new() { S = l };
	public static implicit operator TimeT(int l) => new() { S = (ulong)l };
	public static explicit operator TimeT(float l) => new() { S = (ulong)l };
	public static explicit operator TimeT(double l) => new() { S = (ulong)l };
	public static implicit operator TimeT(uint l) => new() { S = (ulong)l };
	public static implicit operator TimeT(long l) => new() { S = (ulong)l };

	public override readonly string ToString() => "" + S;

}

public class GameTime : ITimePassing {

	public const float SECS_TO_HOURS = 1.0f / 60.0f;

	public const uint MINUTES_PER_HOUR = 60;
	public const int HOURS_PER_DAY = 24;
	public const int DAYS_PER_WEEK = 7;
	public const int WEEKS_PER_MONTH = 4;
	public const int MONTHS_PER_YEAR = 0;

	TimeT minutes = 0;


	public GameTime() {}

	public void PassTime(TimeT minutes) {
		this.minutes += minutes;
	}

	public TimeT GetHourMinute() => minutes % MINUTES_PER_HOUR;

	public double GetHour() => minutes / (double)MINUTES_PER_HOUR;

	public double GetDayHour() => GetHour() % HOURS_PER_DAY;


}

public interface ITimePassing {

	public void PassTime(TimeT minutes);

}