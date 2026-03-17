using Godot;

namespace sound.region;

[GlobalClass]
public partial class EnvironSoundChoiceStream : EnvironSoundChoice {

	[Export] StringName Name = "";
	[Export] int Stream;
	float targetVolume;

	public override void Process(float delta) {
		Debug.Assert(Mama is not null, "Need mama");
		Debug.Assert(Stream >= 0 && Stream < Mama.SyncStream.StreamCount, $"Stream index {Stream} out of range [0..{Mama.SyncStream.StreamCount}");
		float vol = Mathf.DbToLinear(Mama.SyncStream.GetSyncStreamVolume(Stream));
		Mama.SyncStream.SetSyncStreamVolume(Stream, Mathf.LinearToDb(Mathf.MoveToward(vol, targetVolume, delta)));
	}

	public override void SetVolume(float to) {
		Debug.Assert(Mama is not null, "Need mama");
		Debug.Assert(Stream >= 0 && Stream < Mama.SyncStream.StreamCount, $"Stream index {Stream} out of range [0..{Mama.SyncStream.StreamCount}");
		targetVolume = to;
	}

	public override string ToString() {
		Debug.Assert(Mama is not null, "Need mama");
		Debug.Assert(Stream >= 0 && Stream < Mama.SyncStream.StreamCount, $"Stream index {Stream} out of range [0..{Mama.SyncStream.StreamCount}");
		return $"Stream(id: {Stream}, tgt: {targetVolume}, v: {Mathf.DbToLinear(Mama.SyncStream.GetSyncStreamVolume(Stream))})";
	}

}