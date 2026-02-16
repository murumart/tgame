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

public class GameTime {

	public event Action<TimeT> HourPassedEvent;
	public event Action<TimeT> TimePassedEvent;

	public const float SECS_TO_HOURS = 1.0f / 60.0f;

	public const int MINUTES_PER_HOUR = 60;
	public const int HOURS_PER_DAY = 24;
	public const int DAYS_PER_WEEK = 7;
	public const int WEEKS_PER_MONTH = 4;
	public const int MONTHS_PER_YEAR = 12;

	TimeT minutes = 0;
	public TimeT Minutes { get => minutes; }


	public GameTime() { }

	public void PassTime(TimeT minutes) {
		var prevHour = this.minutes / 60;
		var nextHour = (this.minutes + minutes) / 60;
		var hourDiff = nextHour - prevHour;

		this.minutes += minutes;
		TimePassedEvent?.Invoke(minutes);

		for (ulong i = 1; i <= hourDiff; i++) {
			HourPassedEvent?.Invoke(prevHour * 60 + i * 60);
		}
	}

	public double GetHour() => GetHourS(minutes);
	public double GetDay() => GetDayS(minutes);
	public double GetWeek() => GetWeekS(minutes);
	public double GetMonth() => GetMonthS(minutes);
	public double GetYear() => GetYearS(minutes);

	public static TimeT Years(uint many) => Months(MONTHS_PER_YEAR) * many;
	public static TimeT Months(uint many) => Weeks(WEEKS_PER_MONTH) * many;
	public static TimeT Weeks(uint many) => Days(DAYS_PER_WEEK) * many;
	public static TimeT Days(uint many) => Hours(HOURS_PER_DAY) * many;
	public static TimeT Hours(uint many) => MINUTES_PER_HOUR * many;

	public int GetHourMinute() => GetHourMinuteS(minutes);
	public int GetDayHour() => GetDayHourS(minutes);
	public int GetMonthDay() => GetMonthDayS(minutes);

	public static double GetHourS(TimeT minutes) => minutes / (double)MINUTES_PER_HOUR;
	public static double GetDayS(TimeT minutes) => GetHourS(minutes) / HOURS_PER_DAY;
	public static double GetWeekS(TimeT minutes) => GetDayS(minutes) / DAYS_PER_WEEK;
	public static double GetMonthS(TimeT minutes) => GetWeekS(minutes) / WEEKS_PER_MONTH;
	public static double GetYearS(TimeT minutes) => GetMonthS(minutes) / MONTHS_PER_YEAR;

	public static int GetHourMinuteS(TimeT minutes) => (int)(float)(minutes % MINUTES_PER_HOUR);
	public static int GetDayHourS(TimeT minutes) => (int)(GetHourS(minutes) % HOURS_PER_DAY);
	public static int GetMonthDayS(TimeT minutes) => (int)(GetDayS(minutes) % (WEEKS_PER_MONTH * DAYS_PER_WEEK)) + 1;

	public static string GetFancyTimeString(TimeT minutes) {
		var hours = minutes / MINUTES_PER_HOUR;
		var days = hours / HOURS_PER_DAY;
		var weeks = days / DAYS_PER_WEEK;

		var daysPlusWeek = days - weeks * DAYS_PER_WEEK;

		if (weeks > 0) return "" + weeks + " week" + (weeks == 1 ? "" : 's') + (daysPlusWeek > 0 ? " and " + daysPlusWeek + " day" + (daysPlusWeek == 1 ? "" : 's') : "");
		if (days > 0) return "" + days + " day" + (days == 1 ? "" : 's');
		if (hours > 0) return "" + hours + " hour" + (hours == 1 ? "" : 's');
		return "" + minutes + " minutes";
	}

}
