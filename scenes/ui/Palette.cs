using Godot;

namespace scenes.ui;

public static class Palette {

	static ColorPalette swatch = GD.Load<ColorPalette>("res://resources/visual/theme/palette.tres");
	public static Godot.ColorPalette Swatch => swatch;

	public static Color GetColor(int ix) {
		Debug.Assert(ix >= 0 && ix < 31, "Palette index wrong");
		return swatch.Colors[ix];
	}

	public static Color StormDust => GetColor(0);
	public static Color NaturalGrey => GetColor(1);
	public static Color HeatheredGrey => GetColor(2);
	public static Color Tacao => GetColor(3);
	public static Color DarkSalmon => GetColor(4);
	public static Color BrownRust => GetColor(5);
	public static Color Ferra => GetColor(6);
	public static Color Dune => GetColor(7);
	public static Color PurpleTaupe => GetColor(8);
	public static Color Puce => GetColor(9);
	public static Color FrenchBeige => GetColor(10);
	public static Color IndianYellow => GetColor(11);
	public static Color Chardonnay => GetColor(12);
	public static Color DairyCream => GetColor(13);
	public static Color Pavlova => GetColor(14);
	public static Color BrassGreen => GetColor(15);
	public static Color Camo => GetColor(16);
	public static Color MediumForestGreen => GetColor(17);
	public static Color LunarGreen => GetColor(18);
	public static Color Cactus => GetColor(19);
	public static Color GreyishGreen => GetColor(20);
	public static Color ShadowGreen => GetColor(21);
	public static Color SurfCrest => GetColor(22);
	public static Color WhiteSmoke => GetColor(23);
	public static Color Iron => GetColor(24);
	public static Color Heather => GetColor(25);
	public static Color GullGrey => GetColor(26);
	public static Color Hoki => GetColor(27);
	public static Color Dusk => GetColor(28);
	public static Color Tuna => GetColor(29);
	public static Color Dark => GetColor(30);

}