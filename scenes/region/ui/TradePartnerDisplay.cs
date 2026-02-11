using Godot;

namespace scenes.region.ui {

	public partial class TradePartnerDisplay : Control {

		[Export] Label PartnerNameLabel;
		[Export] Container TradeParnerOfferList;
		[Export] OptionButton GiveResourceList;
		[Export] OptionButton TakeResourceList;
		[Export] Slider GiveAmountSlider;
		[Export] LineEdit TakeAmountLine;
		[Export] PackedScene TradePartnerSliderScene;

		Faction me;
		Faction partner;


		public void Display(Faction me, Faction partner) {
			this.me = me;
			this.partner = partner;

			var has = me.GetTradeOffers(partner, out var tradeOffers);
			Debug.Assert(has, "Don't have trade relations with this faction");

			foreach (var a in TradeParnerOfferList.GetChildren()) a.QueueFree();
			foreach (var toffer in tradeOffers) {
				var slider = TradePartnerSliderScene.Instantiate() as TradeSlider;
                Debug.Assert(slider != null, "Trade slider scene isn't a trade slider??");
                TradeParnerOfferList.AddChild(slider);
                Debug.Assert(toffer.IsValid, "This trade offer isn't valid damn");
                slider.Display(toffer);
			}
		}

	}

}

