using Godot;

namespace sound.region;

[GlobalClass]
public abstract partial class EnvironSoundChoice : Resource {

	public EnvironSounds Mama;


	public abstract void SetVolume(float to);
	public abstract void Process(float delta);

}
