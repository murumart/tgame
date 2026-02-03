using System;
using System.Collections.Generic;
using Godot;
using resources.game.resource_site_types;

namespace resouces.game {

	[GlobalClass]
	public partial class GenerationParameters : Resource {

		[Export] public ResourceSiteType Target;

		public enum CalcCommand {

			GetHumidity,
			GetElevation,
			GetTemperature,
			GetGroundTile,

			OceanTile,

			OperationAdd,
			OperationSubtract,
			OperationMultiply,
			OperationDivide,

			OperationEquals,
			OperationGreater,
			OperationLesser,
			OperationNot,

		}

		[Export] Godot.Collections.Array<CalcCommand> instructions;
		[Export] float[] initialStack;

		Stack<float> stack = new();


		public float Calculate(World world, int x, int y) {
			stack.Clear();
			if (initialStack != null && initialStack.Length > 0) foreach (var f in initialStack) stack.Push(f);

			foreach (var cmd in instructions) {
				switch (cmd) {
					case CalcCommand.GetHumidity:
						stack.Push(world.GetHumidity(x, y));
						break;
					case CalcCommand.GetElevation:
						stack.Push(world.GetElevation(x, y));
						break;
					case CalcCommand.GetTemperature:
						stack.Push(world.GetTemperature(x, y));
						break;
					case CalcCommand.GetGroundTile:
						stack.Push((float)world.GetTile(x, y));
						break;

					case CalcCommand.OceanTile:
						stack.Push((float)GroundTileType.Ocean);
						break;

					case CalcCommand.OperationAdd:
						Debug.Assert(stack.Count > 1, "Not enough values in stack for operands"); {
							float b = stack.Pop();
							float a = stack.Pop();
							stack.Push(a + b);
						}
						break;
					case CalcCommand.OperationSubtract:
						Debug.Assert(stack.Count > 1, "Not enough values in stack for operands"); {
							float b = stack.Pop();
							float a = stack.Pop();
							stack.Push(a - b);
						}
						break;
					case CalcCommand.OperationMultiply:
						Debug.Assert(stack.Count > 1, "Not enough values in stack for operands"); {
							float b = stack.Pop();
							float a = stack.Pop();
							stack.Push(a * b);
						}
						break;
					case CalcCommand.OperationDivide:
						Debug.Assert(stack.Count > 1, "Not enough values in stack for operands"); {
							float b = stack.Pop();
							float a = stack.Pop();
							stack.Push(a / b);
						}
						break;
					case CalcCommand.OperationEquals:
						Debug.Assert(stack.Count > 1, "Not enough values in stack for operands"); {
							float b = stack.Pop();
							float a = stack.Pop();
							stack.Push(a == b ? 1.0f : 0.0f);
						}
						break;
					case CalcCommand.OperationGreater:
						Debug.Assert(stack.Count > 1, "Not enough values in stack for operands"); {
							float b = stack.Pop();
							float a = stack.Pop();
							stack.Push(a > b ? 1.0f : 0.0f);
						}
						break;
					case CalcCommand.OperationLesser:
						Debug.Assert(stack.Count > 1, "Not enough values in stack for operands"); {
							float b = stack.Pop();
							float a = stack.Pop();
							stack.Push(a < b ? 1.0f : 0.0f);
						}
						break;
					case CalcCommand.OperationNot:
						Debug.Assert(stack.Count > 0, "Not enough values in stack for operands"); {
							float a = stack.Pop();
							stack.Push(a != 0f ? 0.0f : 1.0f);
						}
						break;
					default:
						Debug.Assert(false, "unimplemented instructions");
						break;
				}
			}
			Debug.Assert(stack.Count > 0, "Stack ended up empty somehow");
			return stack.Pop();
		}

	}

}

