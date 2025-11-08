using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Godot;

internal static class Debug {
	//https://www.reddit.com/r/godot/comments/obxm0i/comment/hj4htrk/
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Assert(bool cond, string msg)
#if TOOLS
		{
		if (!cond) {
			msg = new StackFrame(1).GetMethod().Name + ": " + msg;
			GD.PrintErr(msg);
			throw new ApplicationException($"Assertion failed: {msg}");
		}
	}
#else
	{}
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
