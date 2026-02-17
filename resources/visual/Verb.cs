using System;
using Godot;

namespace resources.visual {

	[GlobalClass]
	public partial class Verb : Resource {

        [Export] public string Infinitive;
        [Export] public string Progressive;


		public static Verb Make(string inf, string prog) {
			return new() {
                Infinitive = inf,
                Progressive = prog,
            };
		}

		public override string ToString() => $"Verb({Infinitive}, {Progressive})";
        
	}

}

