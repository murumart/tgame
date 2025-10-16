using System;
using Godot;

internal static class Debug {
	//https://www.reddit.com/r/godot/comments/obxm0i/comment/hj4htrk/
	internal static void Assert(bool cond, string msg)
#if TOOLS
	{
		if (!cond) {
			GD.PrintErr(msg);
			throw new ApplicationException($"Assertion failed: {msg}");
		}
	}
#else
	{}
#endif

	internal static void PrintWithStack(params object[] vals)
#if TOOLS
		{
		foreach (var val in vals) GD.Print(val, "\b");
		GD.Print();
		System.Diagnostics.StackTrace t = new(); GD.Print("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\nStack Trace: " + t.ToString() + "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n\n");
	}
#else
	{}
#endif
}
