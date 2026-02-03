using System;
using Godot;
using resources.game.resource_site_types;

namespace resources.game {

	[GlobalClass]
	public partial class ResourceSiteGenerationParameters : Resource {

		[Export] public ResourceSiteType Target;
		[Export] public float MinElevation = 0f;
		[Export] public float MaxElevation = 1f;
		[Export] public float MinTemperature = -1f;
		[Export] public float MaxTemperature = 1f;
		[Export] public float MinHumidity = 0f;
		[Export] public float MaxHumidity = 1f;
		[Export] public float Rarity = 1f;


		public static float ParamDistance(float min, float max, float val) {
			float rangeWidth = Math.Abs(min - max);
			float rangeCentre = rangeWidth * 0.5f;
			return Math.Abs(rangeCentre - val) / rangeWidth;
		}

	}

}

