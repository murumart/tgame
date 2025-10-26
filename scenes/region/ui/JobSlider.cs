using Godot;
using System;

namespace scenes.region.ui {

	public partial class JobSlider : VBoxContainer {

		public static readonly PackedScene MyScene = GD.Load<PackedScene>("res://scenes/region/ui/job_slider.tscn");

		[ExportGroup("Nodes")]
		[Export] Label NameLabel;
		[Export] Label MoneyLabel;
		[Export] Slider Slider;

		string unitSymbol;
		bool _ready;


		public static JobSlider Instantiate() {
			return MyScene.Instantiate<JobSlider>();
		}

		public override void _Ready() {
			Slider.ValueChanged += ValueChanged;
			_ready = true;
		}

		public override void _ExitTree() {
			_ready = false;
			Slider.ValueChanged -= ValueChanged;
		}

		public void Setup(string name, int sliderMax, string unitSymbol) {
			Debug.Assert(_ready, "dont setup before we're ready");

			NameLabel.Text = name;
			Slider.MaxValue = sliderMax;
			this.unitSymbol = unitSymbol;
		}

		public int GetValue() {
			Debug.Assert(_ready, "dont anything before we're ready");

			// Slider.Rounded needs to be set in the editor
			return (int)Slider.Value;
		}

		public void ValueChanged(double to) {
			int val = (int)to;

			MoneyLabel.Text = "" + val + unitSymbol;
		}
}

}

