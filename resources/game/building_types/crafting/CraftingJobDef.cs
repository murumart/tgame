using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using resources.game.resource_types;
using resources.visual;

namespace resources.game.building_types.crafting;

[GlobalClass]
public partial class CraftingJobDef : Resource, CraftJob.ICraftingJobDef {

	[Export] Godot.Collections.Dictionary<ResourceType, int> inputs;
	[Export] Godot.Collections.Dictionary<ResourceType, int> outputs;
	[Export] int timeTakenMinutes;
	[Export] int maxWorkers = 5;
	[Export] Noun Product;
	[Export] Verb Process;

	ResourceConsumer[] _inputs;
	public ResourceConsumer[] Inputs {
		get {
			if (_inputs is null) {

				var inp = new List<ResourceConsumer>(inputs.Count);
				foreach (var (res, amount) in inputs) {
					Debug.Assert(res != null, $"Thing in inputs keys of {Product}, {Process} is null!");
					if (res is ResourceOrType ot) foreach (var ft in ot.Flatten()) inp.Add(new(ft, amount));
					else inp.Add(new(res, amount));
				}
				_inputs = inp.ToArray();
			}
			return _inputs;
		}
	}

	public ResourceBundle[] Outputs {
		get {
			ResourceBundle[] outp = new ResourceBundle[outputs.Count];
			var keys = outputs.Keys.ToArray<ResourceType>();
			for (int i = 0; i < outp.Length; i++) {
				Debug.Assert(keys[i] != null, $"Thing at {i} in outputs keys of {Product}, {Process} is null!");
				Debug.Assert(keys[i] is not ResourceOrType);
				outp[i] = new(keys[i], outputs[keys[i]]);
			}
			return outp;

		}
	}

	public TimeT TimeTaken => timeTakenMinutes;
	public int MaxWorkers => maxWorkers;


	public CraftJob GetJob() {
		Debug.Assert(MaxWorkers >= 0, "max workers can't be negative");
		return new(Inputs, Outputs, TimeTaken, (uint)MaxWorkers, Product, Process);
	}

}



