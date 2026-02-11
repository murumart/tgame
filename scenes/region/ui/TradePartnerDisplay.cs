using System;
using Godot;

namespace scenes.region.ui {

	public partial class TradePartnerDisplay : Control {

		public event Action TradedOrCanceled;

		[Export] Label PartnerNameLabel;
		[Export] Container TradeParnerOfferList;
		[Export] Container MyOfferList;
		[Export] OptionButton GiveResourceList;
		[Export] OptionButton TakeResourceList;
		[Export] Container GottenOffersParent;
		[Export] Container SentOffersParent;
		[Export] Slider GiveAmountSlider;
		[Export] LineEdit TakeAmountLine;
		[Export] Label NoOffersLabel;
		[Export] PackedScene TradePartnerSliderScene;

		Faction me;
		Faction partner;


		public void Display(Faction me, Faction partner) {
			this.me = me;
			this.partner = partner;

			var has = me.GetGottenTradeOffers(partner, out var gottenOffers);
			has = me.GetSentTradeOffers(partner, out var sentOffers) || has;
			Debug.Assert(has, "Don't have trade relations with this faction");

			PartnerNameLabel.Text = partner.Name;

			bool hadGotten = false;
			bool hadSent = false;

			foreach (var a in TradeParnerOfferList.GetChildren()) a.QueueFree();
			if (gottenOffers != null) foreach (var toffer in gottenOffers) {
				var slider = TradePartnerSliderScene.Instantiate() as TradeSlider;
                Debug.Assert(slider != null, "Trade slider scene isn't a trade slider??");
                TradeParnerOfferList.AddChild(slider);
                Debug.Assert(toffer.IsValid, "This trade offer isn't valid damn");
                slider.Display(me, partner, toffer, false);
				slider.OfferChanged += () => TradedOrCanceled?.Invoke();
				hadGotten = true;
			}

			foreach (var a in MyOfferList.GetChildren()) a.QueueFree();
			if (sentOffers != null) foreach (var toffer in sentOffers) {
				var slider = TradePartnerSliderScene.Instantiate() as TradeSlider;
                Debug.Assert(slider != null, "Trade slider scene isn't a trade slider??");
                MyOfferList.AddChild(slider);
                Debug.Assert(toffer.IsValid, "This trade offer isn't valid damn");
                slider.Display(me, partner, toffer, true);
				slider.OfferChanged += () => TradedOrCanceled?.Invoke();
				hadSent = true;
			}

			GottenOffersParent.Visible = hadGotten;
			SentOffersParent.Visible = hadSent;
			NoOffersLabel.Visible = !hadGotten && !hadSent;
		}

	}

}

