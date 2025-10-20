public static class Time {
	public const float SECS_TO_HOURS = 1.0f / 60.0f;
}

public interface ITimePassing {
	public void PassTime(float hours);
}