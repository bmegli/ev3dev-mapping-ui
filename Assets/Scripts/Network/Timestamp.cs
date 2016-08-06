using System;

public class Timestamp
{
	public static ulong TimestampUs()
	{
		return (ulong)(DateTime.Now.Ticks / 10);
	}
}

