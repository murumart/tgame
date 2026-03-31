using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using scenes.region.ui;

namespace scenes.ui;

public class Option(Func<float> get, Action<float> set, float defa, (float, float) range, bool round = false) {

	public readonly float Default = defa;
	public readonly float Min = range.Item1;
	public readonly float Max = range.Item2;
	public readonly bool Round = round;


	public float Get() {
		float val = get();
		if (Round) Debug.Assert(Mathf.Ceil(val) == Mathf.Floor(val), $"Invalid value (need rounded integer, got {val})");
		Debug.Assert(val >= Min && val <= Max, $"Option get value out of range [{Min}..{Max}] (is {val})");
		return val;
	}

	public void Set(float to) {
		Debug.Assert(to >= Min && to <= Max, $"Option set value out of range [{Min}..{Max}] (is {to})");
		set(to);
	}

	public void Save(ConfigFile file, string category, string key) {
		file.SetValue(category, key, Get());
	}

}

public partial class OptionsMenu : Control {

	public static Action<bool> VisibilityToggled;

	const string USER_PATH = "user://fevered_world";
	const string SAVEFILE_PATH = USER_PATH + "/options.ini";

	static OptionsMenu singleton;
	static OptionsMenu Singleton {
		get {
			Debug.Assert(singleton is not null, "Need OptionsMenu initialised before can get and use");
			return singleton;
		}
		set {
			Debug.Assert(singleton is null, "Can't overwrite OptionsMenu singleton");
			singleton = value;
		}
	}

	static bool Open => singleton?.Visible ?? false;

	static readonly Dictionary<string, Dictionary<string, Option>> options = new() {
		{"volume",
			new() {
				{"main", new(
					get: () => AudioServer.GetBusVolumeLinear(0),
					set: f => {
						AudioServer.SetBusVolumeLinear(0, f);
					},
					defa: 0.75f, range: (0.0f, 1.0f))
				},
				{"ambient", new(
					get: () => AudioServer.GetBusVolumeLinear(1),
					set: f => {
						AudioServer.SetBusVolumeLinear(1, f);
					},
					defa: 1f, range: (0.0f, 1.0f))
				},
			}
		},
		{"graphics",
			new() {
				{"display_scale", new(
					get: () => Singleton.GetWindow().ContentScaleFactor,
					set: to => {
						Debug.Assert(to > 0 && to < 100);
						Singleton.GetWindow().ContentScaleFactor = to;
					},
					defa: 2, range: (1, 4), round: true)
				},
			}
		}
	};
	Option[] indexableOptions;

	ConfigFile file;
	ConfigFile File {
		get {
			Debug.Assert(file is not null, "Need the file not be null");
			return file;
		}
		set {
			Debug.Assert(file is null, "Need the file be null to set it");
			file = value;
		}
	}

	[Export] Container OptionsContainer;
	[Export] Button OkayButton;


	public override void _Ready() {
		indexableOptions = options.SelectMany(catd => catd.Value.Select(no => no.Value)).ToArray();

		OkayButton.Pressed += Undisplay;

		Singleton = this;
		if (!DirAccess.DirExistsAbsolute(USER_PATH)) DirAccess.MakeDirRecursiveAbsolute(USER_PATH);
		file = new();
		file.Load(SAVEFILE_PATH);
		Load();
	}

	public override void _UnhandledKeyInput(InputEvent ievt) {
		if (ievt is not InputEventKey iek) {
			return;
		}
		if (iek.Pressed && iek.Keycode == Key.Escape) {
			if (!Open) {
				Display();
				GetViewport().SetInputAsHandled();
			} else {
				Undisplay();
				GetViewport().SetInputAsHandled();
			}
		}
	}

	void Display() {
		Show();
		foreach (var child in OptionsContainer.GetChildren()) {
			child.QueueFree();
		}
		int ix = 0;
		foreach (var (catname, cat) in options) {
			var label = new Label() {
				LabelSettings = GD.Load<LabelSettings>("res://resources/visual/theme/label_styles/10px.tres"),
				Text = catname.Capitalize(),
			};
			OptionsContainer.AddChild(label);
			foreach (var (opname, op) in cat) {
				var slider = JobSlider.Instantiate();
				OptionsContainer.AddChild(slider);
				slider.Setup(OptionChanged, ix, op.Get(), opname.Capitalize(), op.Max, "", op.Round, op.Min);
				ix += 1;
			}
		}
		VisibilityToggled?.Invoke(true);
	}

	void OptionChanged(int ix, float value) {
		indexableOptions[ix].Set(indexableOptions[ix].Get() + value);
	}

	void Undisplay() {
		Hide();
		Save();
		VisibilityToggled?.Invoke(false);
	}

	void Load() {
		foreach (var (catname, cat) in options) {
			foreach (var (opname, op) in cat) {
				op.Set(File.GetValue(catname, opname, op.Default).AsSingle());
			}
		}
	}

	void Save() {
		foreach (var (catname, cat) in options) {
			foreach (var (opname, op) in cat) {
				File.SetValue(catname, opname, op.Get());
			}
		}
		File.Save(SAVEFILE_PATH);
	}

}
