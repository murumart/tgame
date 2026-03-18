using System;
using Godot;
using scenes.autoload;
using scenes.region;

namespace sound.region;

public partial class EnvironSounds : AudioStreamPlayer {

	[Export] public RegionCamera Camera;
	[Export] EnvironSoundChoiceBinary soundChoices;

	public AudioStreamSynchronized SyncStream => (AudioStreamSynchronized)Stream;

	public override void _Ready() {
		void SetMama(EnvironSoundChoice c) {
			c.Mama = this;
			if (c is EnvironSoundChoiceBinary b) {
				SetMama(b.Above);
				SetMama(b.Below);
			}
		}
		SetMama(soundChoices);
		UILayer.DebugDisplay(() => {
			string D(EnvironSoundChoice c, int d) { if (c is EnvironSoundChoiceStream s) return Ds(s); else if (c is EnvironSoundChoiceBinary b) return Db(b, d + 1); return "?"; }
			string Ds(EnvironSoundChoiceStream s) => s.ToString();
			string Db(EnvironSoundChoiceBinary b, int d) {
				string tabs = new ('\t', d);
				string o = $"{b.Name}(\n{tabs}a: {D(b.Above, d)}\n{tabs}b: {D(b.Below, d)}";
				return o;
			}
			return $"snds: " + Db(soundChoices, 1);
		});
	}

	public override void _Process(double delta) {
		soundChoices.Process((float)delta);
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

	public Vector2I GetCameraPosition() {
		var world = GameMan.Singleton?.Game?.Map?.World ?? null;
		Debug.Assert(world is not null);
		var camPosOnTile = Camera.GetHoveredTilePos();
		return camPosOnTile + GameMan.Singleton.Game.PlayRegion.WorldPosition;
	}

	public float GetElevation() {
		var world = GameMan.Singleton.Game.Map.World;
		var campos = GetCameraPosition();
		return world.GetElevation(campos.X, campos.Y);
	}

	public float GetHumidity() {
		var world = GameMan.Singleton.Game.Map.World;
		var campos = GetCameraPosition();
		return world.GetHumidity(campos.X, campos.Y);
	}

	public float GetTemperature() {
		var world = GameMan.Singleton.Game.Map.World;
		var campos = GetCameraPosition();
		return world.GetTemperature(campos.X, campos.Y);
	}

}
