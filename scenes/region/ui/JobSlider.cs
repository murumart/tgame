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
		bool _ready;
		Action<int, int> valueChangedCallback;
		int jobIx;


		public static JobSlider Instantiate() => Packed.Instantiate<JobSlider>();


		public override void _Ready() {
			Slider.ValueChanged += ValueChanged;
			Slider.DragEnded += DragEnded;
			_ready = true;
			GD.Print("JobSlider::_Ready : ready");
		}

		public override void _Notification(int what) {
			if (what == NotificationPredelete) {
				_ready = false;
				Slider.ValueChanged -= ValueChanged;
				Slider.DragEnded -= DragEnded;
				GD.Print("JobSlider::_Notification : deleting");
			}
		}

		public void Setup(Action<int, int> valueChangedCallback, int jobIx, int jobWorkers, string name, uint sliderMax, string unitSymbol) {
			Debug.Assert(_ready, "dont anything before we're ready");
			this.jobIx = jobIx;
			this.valueChangedCallback = valueChangedCallback;
			NameLabel.Text = name;
			Slider.MaxValue = sliderMax;
			Slider.Value = jobWorkers;
			Slider.Editable = sliderMax != 0;
			lastValue = (int)Slider.Value;
			this.unitSymbol = unitSymbol;
			MoneyLabel.Text = "" + lastValue + unitSymbol;
		}

		public int GetValue() {
			Debug.Assert(_ready, "dont anything before we're ready");

			// Slider.Rounded needs to be set in the editor
			return (int)Slider.Value;
		}

		public void ValueChanged(double to) {
			int val = (int)to;

			MoneyLabel.Text = "" + val + unitSymbol;
			//ValueChangedCallback(jobIx, val);
		}

		int lastValue = 0;
		public void DragEnded(bool valueChanged) {
			if (!valueChanged) return;
			int val = (int)Slider.Value;
			GD.Print($"JobSlider::DragEnded : val {val} last {lastValue}");
			ValueChangedCallback(jobIx, val - lastValue);
			lastValue = val;
		}

		void ValueChangedCallback(int ix, int to) => valueChangedCallback(ix, to);


	}

}

