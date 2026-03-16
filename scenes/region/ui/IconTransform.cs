using Godot;

public partial class IconTransform : Node2D {

	public void UpdateViewTransform() {
		Scale = Vector2.One;
		var tf = GetGlobalTransformWithCanvas();
		var inverse = tf.Scale.Inverse();
		if (inverse.X < 1) Scale = inverse;
	}

}
