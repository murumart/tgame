using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using Environment = System.Environment;

internal static class Debug {
	//https://www.reddit.com/r/godot/comments/obxm0i/comment/hj4htrk/
	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Assert(bool cond, string msg)
#if TOOLS
		{
		if (!cond) {
			var st = new StackTrace(1, true);
			var ts = new StringBuilder();

			ts.Append(msg).Append('\n').Append('\n');
			ts.Append("Stack Trace:\n");

			foreach (var sf in st.GetFrames()) {
				var fn = sf.GetFileName();
				if (fn.StartsWith("/root/godot") || fn.Contains(".godot")) {
					ts.Append($"\t... {sf.GetMethod().DeclaringType}::{sf.GetMethod().Name}\n");
					continue;
				}
				ts.Append($"\t{fn}:{sf.GetFileLineNumber()} : {sf.GetMethod().DeclaringType}::{sf.GetMethod().Name}\n");
			}

			//GD.PrintErr(newmsg);
			GD.PushError(msg);
			OS.Alert(ts.ToString(), $"Assertion Failed in {st.GetFrame(0).GetMethod().DeclaringType}::{st.GetFrame(0).GetMethod().Name}");
			//throw new ApplicationException($"Assertion failed: {msg}");
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

	internal static void PrintInternals(object item) {
		foreach (PropertyDescriptor item1 in TypeDescriptor.GetProperties(item)) {
			GD.Print("    " + item1.Name + " = " + item1.GetValue(item));
		}
	}
}
