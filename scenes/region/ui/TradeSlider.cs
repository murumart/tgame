using System;
using Godot;

namespace scenes.region.ui {

	public partial class TradeSlider : VBoxContainer {

		public event Action OfferChanged;

		[Export] Label GiveLabel;
		[Export] Label GetLabel;
		[Export] Slider UnitsSlider;
		[Export] Button ConfirmButton;
		[Export] Button RejectButton;

		Faction me;
		Faction other;
		TradeOffer offer;
		int sliderVal = 0;
		bool myOffer;


		public void Display(Faction me, Faction other, TradeOffer tradeOffer, bool myOffer) {
			this.me = me;
			this.other = other;
			this.myOffer = myOffer;
			Debug.Assert(tradeOffer.IsValid, "This trade offer isn't valid, pleasse don't display");
			offer = tradeOffer;

			if (!myOffer) {
				UnitsSlider.ValueChanged += OnSliderValueChanged;
				ConfirmButton.Pressed += OnBought;

				GiveLabel.Text = tradeOffer.OffererBuysForSilver ? $"{tradeOffer.RecepientRequiredResourcesUnit.Type.AssetName} x {tradeOffer.RecepientRequiredResourcesUnit.Amount}" : tradeOffer.RecipientPaidSilverUnit + " silver";
				GetLabel.Text = tradeOffer.OffererBuysForSilver ? tradeOffer.OffererPaidSilverUnit + " silver" : $"{tradeOffer.OffererSoldResourcesUnit.Type.AssetName} x {tradeOffer.OffererSoldResourcesUnit.Amount}";
				UnitsSlider.MaxValue = tradeOffer.StoredUnits;
			} else {
				UnitsSlider.Hide();
				ConfirmButton.Hide();
				GiveLabel.Text = tradeOffer.OffererBuysForSilver ? $"{tradeOffer.RecepientRequiredResourcesUnit.Type.AssetName} x {tradeOffer.RecepientRequiredResourcesUnit.Amount}" : tradeOffer.RecipientPaidSilverUnit + " silver";
				GetLabel.Text = tradeOffer.OffererBuysForSilver ? tradeOffer.OffererPaidSilverUnit + " silver" : $"{tradeOffer.OffererSoldResourcesUnit.Type.AssetName} x {tradeOffer.OffererSoldResourcesUnit.Amount}";
			}
			RejectButton.Pressed += OnRejected;
			ConfirmButton.Disabled = true;
			OnSliderValueChanged(0);
		}

		void OnSliderValueChanged(double to) {
			sliderVal = (int)to;
			bool shouldDisable = sliderVal == 0;
			if (offer.OffererBuysForSilver) {
				shouldDisable = shouldDisable || !me.Resources.HasEnough(offer.RecepientRequiredResourcesUnit.Multiply(sliderVal));
				ConfirmButton.Text = $"Sell resources => ({offer.OffererPaidSilverUnit * sliderVal} silver)";
			} else {
				shouldDisable = shouldDisable || me.Silver * sliderVal < offer.RecipientPaidSilverUnit;
				ConfirmButton.Text = $"Buy resources => ({offer.OffererSoldResourcesUnit.Type.AssetName} x {offer.OffererSoldResourcesUnit.Multiply(sliderVal)})";
			}
			ConfirmButton.Disabled = shouldDisable;
		}

		void OnBought() {
			me.AcceptTradeOffer(other, offer, sliderVal);
			OfferChanged?.Invoke();
		}

		void OnRejected() {
			if (!myOffer) {
				me.RejectTradeOffer(other, offer);
			} else {
				me.CancelTradeOffer(other, offer);
			}
			OfferChanged?.Invoke();
		}

	}


}

