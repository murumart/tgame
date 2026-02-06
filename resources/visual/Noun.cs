using System;
using Godot;

namespace resources.visual {

    [GlobalClass]
	public partial class Noun : Resource {

        [Export] public string Singular;
        [Export] public string Plural;

	}

}

