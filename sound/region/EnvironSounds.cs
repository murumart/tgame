using System;
using Godot;
using scenes.autoload;
using scenes.region;

namespace sound.region;

public partial class EnvironSounds : AudioStreamPlayer {

	[Export] RegionCamera camera;
	[Export] EnvironSoundChoice soundChoices;
	[Export(PropertyHint.ExpEasing)] float windEase;
	[Export] float environmentSoundVolume;

	AudioStreamSynchronized SyncStream => (AudioStreamSynchronized)Stream;

	float targetWindVolume = 0.1f;
	float WindVolume {
		get => Mathf.DbToLinear(SyncStream.GetSyncStreamVolume(0));
		set => SyncStream.SetSyncStreamVolume(0, Mathf.LinearToDb(value));
	}

	float targetEnvironVolume = 0.1f;
	float EnvironVolume {
		get => Mathf.DbToLinear(SyncStream.GetSyncStreamVolume(1));
		set => SyncStream.SetSyncStreamVolume(1, Mathf.LinearToDb(value));
	}

	public override void _Ready() {
		UILayer.DebugDisplay(() => $"wind: tgt {targetWindVolume} v {WindVolume}; environ: tgt {targetEnvironVolume} v {EnvironVolume}");
	}

	public override void _Process(double delta) {
		float wv = Mathf.Ease(1f - camera.Zoom.X, windEase);
		float ev = environmentSoundVolume;
		Balance(ref wv, ref ev);
		targetWindVolume = wv;
		targetEnvironVolume = ev;

		WindVolume = Mathf.MoveToward(FixNan(WindVolume), targetWindVolume, (float)delta);
		EnvironVolume = Mathf.MoveToward(FixNan(EnvironVolume), targetEnvironVolume, (float)delta);
	}

	void Balance(ref float a, ref float b) {
		float sum = a + b;
		if (sum <= 0) sum = 1f;
		a /= sum;
		b /= sum;
	}

	float FixNan(float a) {
		if (Mathf.IsNaN(a)) return 1f;
		return a;
	}

}
