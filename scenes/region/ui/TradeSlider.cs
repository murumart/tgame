using System;
using Godot;

namespace scenes.region.ui {

	public partial class TradeSlider : VBoxContainer {

        [Export] Label GiveLabel;
        [Export] Label GetLabel;
        [Export] Slider UnitsSlider;
        [Export] Button ConfirmButton;


        public void Display(TradeOffer tradeOffer) {
            Debug.Assert(tradeOffer.IsValid, "This trade offer isn't valid, pleasse don't display");
            UnitsSlider.ValueChanged += SliderValueChanged;

            GiveLabel.Text = tradeOffer.BuyingWithSilver ? tradeOffer.TakeResourcesUnit.Type.AssetName : "" + tradeOffer.TakeSilverUnit;
            GetLabel.Text = tradeOffer.BuyingWithSilver ? "" + tradeOffer.GiveSilverUnit : tradeOffer.GiveResourcesUnit.Type.AssetName;
            UnitsSlider.MaxValue = tradeOffer.StoredUnits;
        }

        void SliderValueChanged(double to) {
            var val = (int)to;
            ConfirmButton.Disabled = val == 0;
            ConfirmButton.Text = $"Buy {val}";
        }

	}


}

