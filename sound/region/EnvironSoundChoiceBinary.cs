using System;
using Godot;

namespace sound.region;

[GlobalClass]
public partial class EnvironSoundChoiceBinary : EnvironSoundChoice {

	public enum SampleTypes {
		CameraZoom,
		Elevation,
		Humidity,
		Temperature,
	}
	[Export] StringName Name;
	[Export] public SampleTypes SampleType;
	[Export] float InputStart = 0f;
	[Export] float InputEnd = 1f;
	float InputValue {
		get => SampleType switch {
			SampleTypes.CameraZoom => Mama.Camera.Zoom.X,
			SampleTypes.Elevation => Mama.GetElevation(),
			SampleTypes.Humidity => Mama.GetHumidity(),
			SampleTypes.Temperature => Mama.GetTemperature(),
		};
	}

	[Export] public EnvironSoundChoice Above;
	[Export] public EnvironSoundChoice Below;
	[Export(PropertyHint.ExpEasing)] public float Easing = 1f;

	float volume = 1f;


	public override void SetVolume(float to) {
		Debug.Assert(Mama is not null, "Need mama");
		this.volume = to;
		float dt = Mathf.Remap(InputValue, InputStart, InputEnd, 0f, 1f);
		float avol = Mathf.Lerp(0f, 1f, Mathf.Ease(dt, Easing));
		Above.SetVolume(to * avol);
		Below.SetVolume(to * (1 - avol));
	}

	public override void Process(float delta) {
		SetVolume(volume);
		Above.Process(delta);
		Below.Process(delta);
	}

	public override string ToString() {
		Debug.Assert(Mama is not null, "Need mama");
		return $"Binary(\n\ta: {Above},\n\tb: {Below},\n\tv: {volume})";
	}

}
