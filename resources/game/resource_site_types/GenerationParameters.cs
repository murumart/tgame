using System;
using System.Collections.Generic;
using Godot;
using resources.game.resource_site_types;

namespace resouces.game {

	[GlobalClass]
	public partial class GenerationParameters : Resource {

		[Export] public ResourceSiteType Target;

		public enum CalcCommand {

			Constant0,
			Constant1,
			Constant2,
			Constant3,
			Constant4,
			Constant5,
			Constant6,
			Constant7,
			Constant8,
			Constant9,

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

			RangeExpression,
			ElevationOverZero,

		}

		[Export] Godot.Collections.Array<CalcCommand> instructions;
		[Export] float[] constants;

		Stack<float> stack = new();


		public float Calculate(World world, int x, int y) {
			stack.Clear();

			foreach (var cmd in instructions) {
				switch (cmd) {

					case CalcCommand.Constant0:
						Debug.Assert(constants.Length >= 0, "Not enough constants provided");
						stack.Push(constants[0]);
						break;
					case CalcCommand.Constant1:
						Debug.Assert(constants.Length >= 1, "Not enough constants provided");
						stack.Push(constants[1]);
						break;
					case CalcCommand.Constant2:
						Debug.Assert(constants.Length >= 2, "Not enough constants provided");
						stack.Push(constants[2]);
						break;
					case CalcCommand.Constant3:
						Debug.Assert(constants.Length >= 3, "Not enough constants provided");
						stack.Push(constants[3]);
						break;
					case CalcCommand.Constant4:
						Debug.Assert(constants.Length >= 4, "Not enough constants provided");
						stack.Push(constants[4]);
						break;
					case CalcCommand.Constant5:
						Debug.Assert(constants.Length >= 5, "Not enough constants provided");
						stack.Push(constants[5]);
						break;
					case CalcCommand.Constant6:
						Debug.Assert(constants.Length >= 6, "Not enough constants provided");
						stack.Push(constants[6]);
						break;
					case CalcCommand.Constant7:
						Debug.Assert(constants.Length >= 7, "Not enough constants provided");
						stack.Push(constants[7]);
						break;
					case CalcCommand.Constant8:
						Debug.Assert(constants.Length >= 8, "Not enough constants provided");
						stack.Push(constants[8]);
						break;
					case CalcCommand.Constant9:
						Debug.Assert(constants.Length >= 9, "Not enough constants provided");
						stack.Push(constants[9]);
						break;

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
					case CalcCommand.RangeExpression:
						Debug.Assert(stack.Count > 2, "Not enough values in stack for operands"); {
							float b = stack.Pop();
							float a = stack.Pop();
							float c = stack.Pop();
							float rangeWidth = Math.Abs(a-b);
							float rangeCentre = rangeWidth * 0.5f;
							float distanceFromCentre = Math.Abs(rangeCentre - c) / rangeWidth;
							stack.Push(1f - distanceFromCentre);
						}
						break;
					case CalcCommand.ElevationOverZero:
						stack.Push(world.GetElevation(x, y) > 0f ? 1.0f : 0.0f);
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

