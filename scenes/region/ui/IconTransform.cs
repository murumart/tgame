using Godot;

public partial class IconTransform : Node2D {

	[Export] int InverseLimit = 1;


	public void UpdateViewTransform() {
		Scale = Vector2.One;
		var tf = GetGlobalTransformWithCanvas();
		var inverse = tf.Scale.Inverse();
		if (inverse.X < InverseLimit) Scale = inverse;
	}

}
