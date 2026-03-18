using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using scenes.autoload;

public partial class DebugConsoleThing : ColorRect {

	[Export] LineEdit lineEdit;
	[Export] RichTextLabel richTextLabel;

	const string PREX = "_cmd_";


	public override void _Ready() {
		lineEdit.TextSubmitted += TextSubmitted;
        Hide();
	}

	public override void _UnhandledKeyInput(InputEvent @event) {
		var e = @event as InputEventKey;

		if (e.Pressed && e.Keycode == Key.F7) {
			if (Visible) {
				Hide();
				return;
			}
			Show();
			Callable.From(lineEdit.GrabFocus).CallDeferred();
		}
	}

	void TextSubmitted(string txt) {
		var s = txt.Split(' ');
		lineEdit.Text = "";

		if (s.Length == 0) return;
		var meth = GetType().GetMethod(PREX + s[0], System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (meth == null) Output("Unknown command");
		meth.Invoke(this, [s?.Skip(1).ToArray() ?? []]);
	}

	void Output(string txt) {
		richTextLabel.AppendText(txt + '\n');
	}

	void _cmd_help(string[] args) {
		Output("Please helpü");
		Output("Available commands: ");
		var cmds = GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Where(m => m.Name.StartsWith(PREX));
		foreach (var cmd in cmds) {
			Output("\t" + cmd.Name.Right(PREX.Length));
		}
	}

	void _cmd_give(string[] args) {
		if (GameMan.Singleton.Game?.Map?.World is null) {
			Output("Need to be in-game");
		}
		if (args.Length == 0) {
			Output("give <restype> [amount] (<restype> [amount])*");
		}
		List<ResourceBundle> toGive = new();
		string currestype = null;
		int curamount = 0;
		foreach (string arg in args) {
			if (arg.IsValidInt() && currestype == null) {
				Output("Syntax error (expected to have had restype)");
				return;
			} else if (arg.IsValidInt()) {
				curamount = arg.ToInt();
				if (!Registry.Resources.AssetExists(currestype)) {
					Output($"restype {currestype} does not exist");
					return;
				}
				toGive.Add(new(Registry.Resources.GetAsset(currestype), curamount));
                currestype = null;
                curamount = 0;
			} else if (currestype == null) {
				currestype = arg;
			} else {
				if (!Registry.Resources.AssetExists(currestype)) {
					Output($"restype {currestype} does not exist");
					return;
				}
				toGive.Add(new(Registry.Resources.GetAsset(currestype), 1));
				curamount = 0;
				currestype = arg;
			}
		}
		if (currestype != null) {
			if (!Registry.Resources.AssetExists(currestype)) {
				Output($"restype {currestype} does not exist");
				return;
			}
			toGive.Add(new(Registry.Resources.GetAsset(currestype), 1));
		}
		foreach (var bundle in toGive) {
			GameMan.Singleton.Game.PlayRegion.LocalFaction.Resources.AddResource(bundle);
			Output($"Gave {bundle}");
		}
	}

}
