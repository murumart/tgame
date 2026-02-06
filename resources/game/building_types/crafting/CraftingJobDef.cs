using System.Linq;
using Godot;
using Godot.Collections;
using resources.game.resource_types;
using resources.visual;

namespace resouces.game.building_types.crafting {

	[GlobalClass]
	public partial class CraftingJobDef : Resource, CraftJob.ICraftingJobDef {

		[Export] Dictionary<ResourceType, int> inputs;
		[Export] Dictionary<ResourceType, int> outputs;
		[Export] int timeTakenMinutes;
		[Export] int maxWorkers = 5;
		[Export] Noun Product; 
		[Export] Verb Process; 

		public ResourceBundle[] Inputs {
			get {
				ResourceBundle[] inp = new ResourceBundle[inputs.Count];
				var keys = inputs.Keys.ToArray<ResourceType>();
				for (int i = 0; i < inp.Length; i++) {
					inp[i] = new(keys[i], inputs[keys[i]]);
				}
				return inp;
			}
		}

		public ResourceBundle[] Outputs {
			get {
				ResourceBundle[] outp = new ResourceBundle[outputs.Count];
				var keys = outputs.Keys.ToArray<ResourceType>();
				for (int i = 0; i < outp.Length; i++) {
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


}

