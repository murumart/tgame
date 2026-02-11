using System;
using Godot;

namespace scenes.region.ui {

	public partial class TradeSlider : VBoxContainer {

		public event Action Confirmed;

        [Export] Label GiveLabel;
        [Export] Label GetLabel;
        [Export] Slider UnitsSlider;
        [Export] Button ConfirmButton;

		Faction me;
		TradeOffer offer;


        public void Display(Faction me, TradeOffer tradeOffer) {
			this.me = me;
            Debug.Assert(tradeOffer.IsValid, "This trade offer isn't valid, pleasse don't display");
			offer = tradeOffer;

            UnitsSlider.ValueChanged += OnSliderValueChanged;
			ConfirmButton.Pressed += OnBought;

            GiveLabel.Text = tradeOffer.BuyingWithSilver ? tradeOffer.TakeResourcesUnit.Type.AssetName : tradeOffer.TakeSilverUnit + " silver";
            GetLabel.Text = tradeOffer.BuyingWithSilver ? tradeOffer.GiveSilverUnit + " silver" : tradeOffer.GiveResourcesUnit.Type.AssetName;
            UnitsSlider.MaxValue = tradeOffer.StoredUnits;
			ConfirmButton.Disabled = true;
			OnSliderValueChanged(0);
        }

        void OnSliderValueChanged(double to) {
            var val = (int)to;
			bool shouldDisable = val == 0;
			if (offer.BuyingWithSilver) {
				shouldDisable = shouldDisable || !me.Resources.HasEnough(offer.TakeResourcesUnit.Multiply(val));
				ConfirmButton.Text = $"Sell resources => ({offer.GiveSilverUnit * val} silver)";
			} else {
				shouldDisable = shouldDisable || me.Silver * val < offer.TakeSilverUnit;
				ConfirmButton.Text = $"Buy resources => ({offer.GiveResourcesUnit.Type.AssetName} x {offer.GiveResourcesUnit.Multiply(val)})";
			}
            ConfirmButton.Disabled = shouldDisable;
        }

		void OnBought() {

			Confirmed?.Invoke();
		}

	}


}

