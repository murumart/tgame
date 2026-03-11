using System;
using Godot;

namespace scenes.region.ui;

public partial class ApprovalMeter : Label {

	[Export] TextureRect faceDisplay;

	[Export] Godot.Collections.Array<Texture2D> faceTextures;


	public override void _Ready() {
		Debug.Assert(faceDisplay != null, "faceDisplay != null");
		Debug.Assert(faceTextures != null && faceTextures.Count > 0, "faceTextures != null && faceTextures.Count > 0");
	}

	public void Display(float approval, float increase) {
		Text = $"     {(int)(approval * 100)}% ({(increase > 0 ? '+' : '-')}{(int)(Mathf.Abs(increase) * 100)}%)";
		faceDisplay.Texture = faceTextures[(int)(faceTextures.Count * (approval - 0.01f))];
	}

}
