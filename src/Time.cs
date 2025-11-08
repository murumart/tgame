using System;
using Godot;

/// <summary>
/// It's an integer time unit!! Trust me...
/// </summary>
public readonly struct TimeT {

	private ulong S { init; get; }


	public static TimeT operator +(TimeT m) => m;
	public static TimeT operator +(TimeT a, TimeT b) => a.S + b.S;
	public static TimeT operator *(TimeT a, TimeT b) => a.S * b.S;
	public static TimeT operator %(TimeT a, TimeT b) => a.S % b.S;

	public static implicit operator ulong(TimeT m) => m.S;
	public static implicit operator TimeT(ulong l) => new() { S = l };
	public static implicit operator TimeT(int l) => (ulong)l;
	public static implicit operator TimeT(uint l) => (ulong)l;
	public static implicit operator TimeT(long l) => (ulong)l;

	public static explicit operator TimeT(float l) => (ulong)l;
	public static explicit operator TimeT(double l) => (ulong)l;

	public override readonly string ToString() => "" + S;

}

public class GameTime : ITimePassing {

	public const float SECS_TO_HOURS = 1.0f / 60.0f;

	public const int MINUTES_PER_HOUR = 60;
	public const int HOURS_PER_DAY = 24;
	public const int DAYS_PER_WEEK = 7;
	public const int WEEKS_PER_MONTH = 4;
	public const int MONTHS_PER_YEAR = 0;

	TimeT minutes = 0;


	public GameTime() {
		/*for (int i = 0; i < 900; i++) {
			System.Console.Out.WriteLine($"m: {minutes} d: {GetDay():0.00} m: {GetMonth():0.00} dh: {GetDayHour():0.00} md: {GetMonthDay():0.00} ");
			PassTime(60 * 12);
		}*/
	}

	public void PassTime(TimeT minutes) {
		this.minutes += minutes;
	}

	public double GetHour() => minutes / (double)MINUTES_PER_HOUR;
	public double GetDay() => GetHour() / HOURS_PER_DAY;
	public double GetWeek() => GetDay() / DAYS_PER_WEEK;
	public double GetMonth() => GetWeek() / WEEKS_PER_MONTH;
	public double GetYear() => GetMonth() / MONTHS_PER_YEAR;

	public int GetHourMinute() => (int)(float)(minutes % MINUTES_PER_HOUR);
	public int GetDayHour() => (int)(GetHour() % HOURS_PER_DAY);
	public int GetMonthDay() => (int)(GetDay() % (WEEKS_PER_MONTH * DAYS_PER_WEEK)) + 1;

	public static string FancyTimeString(TimeT minutes) {
		var hours = minutes / MINUTES_PER_HOUR;
		var days = hours / DAYS_PER_WEEK;
		var weeks = days / DAYS_PER_WEEK;

		if (weeks > 0) return "" + weeks + " weeks";
		if (days > 0) return "" + days + " days";
		if (hours > 0) return "" + hours + " hours";
		return "" + minutes + " minutes";
	}

}

public interface ITimePassing {

	public void PassTime(TimeT minutes);

}