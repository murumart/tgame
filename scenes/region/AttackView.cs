using System;
using Godot;

namespace scenes.region;

public partial class AttackView : MapObjectView {

	[Export] Line2D attackDirectionDisplay;


	public override void _Ready() {
		type = Type.Attack;
		base._Ready();
	}

}
