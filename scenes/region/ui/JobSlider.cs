using System;
using Godot;

namespace scenes.region.ui {

	public partial class JobSlider : VBoxContainer {

		static readonly PackedScene Packed = GD.Load<PackedScene>("res://scenes/region/ui/job_slider.tscn");

		[ExportGroup("Nodes")]
		[Export] Label NameLabel;
		[Export] Label MoneyLabel;
		[Export] Slider Slider;

		string unitSymbol;
		bool round;
		bool _ready;
		Action<int, float> valueChangedCallback;
		int jobIx;


		public static JobSlider Instantiate() => Packed.Instantiate<JobSlider>();

		public override void _Ready() {
			Slider.ValueChanged += ValueChanged;
			Slider.DragEnded += DragEnded;
			_ready = true;
			//GD.Print("JobSlider::_Ready : ready");
		}

		public override void _Notification(int what) {
			if (what == NotificationPredelete) {
				_ready = false;
				Slider.ValueChanged -= ValueChanged;
				Slider.DragEnded -= DragEnded;
			}
		}

		public void Setup(Action<int, float> valueChangedCallback, int jobIx, float currentValue, string name, float valueMax, string unitSymbol, bool round = true, float minValue = 0f) {
			Debug.Assert(_ready, "dont anything before we're ready");
			this.jobIx = jobIx;
			this.round = round;
			this.valueChangedCallback = valueChangedCallback;
			NameLabel.Text = name;
			Slider.MinValue = minValue;
			Slider.MaxValue = valueMax;
			Slider.Value = currentValue;
			Slider.Editable = valueMax != 0;
			lastValue = round ? (int)Slider.Value : (float)Slider.Value;
			this.unitSymbol = unitSymbol;
			MoneyLabel.Text = "" + lastValue + unitSymbol;
		}

		//public int GetValue() {
		//	Debug.Assert(_ready, "dont anything before we're ready");
		//
		//	// Slider.Rounded needs to be set in the editor
		//	return (int)Slider.Value;
		//}

		public void ValueChanged(double to) {
			float val = (float)to;
			if (round) val = (float)Mathf.Round(Slider.Value);

			MoneyLabel.Text = "" + (round ? (int)val : val) + unitSymbol;
			//ValueChangedCallback(jobIx, val);
		}

		float lastValue = 0;
		public void DragEnded(bool valueChanged) {
			if (!valueChanged) return;
			float val = (float)Slider.Value;
			if (round) val = Mathf.RoundToInt(Slider.Value);
			Slider.SetValueNoSignal(val);
			GD.Print($"JobSlider::DragEnded : val {val} last {lastValue}");
			ValueChangedCallback(jobIx, val - lastValue);
			lastValue = val;
		}

		void ValueChangedCallback(int ix, float to) => valueChangedCallback(ix, to);

		public void Disable() {
			Slider.Editable = false;
		}
	}

}

