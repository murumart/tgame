using System;
using Godot;

namespace scenes.region.ui {

	public partial class TradePartnerDisplay : Control {

		public event Action TradedOrCanceled;

		[Export] Label PartnerNameLabel;
		[Export] Container TradeParnerOfferList;
		[Export] Container GottenOffersParent;
		[Export] Container MyOfferList;

		[Export] Label NewOfferLabel;
		[Export] Container SentOffersParent;
		[Export] OptionButton GiveResourceList;
		[Export] OptionButton TakeResourceList;
		[Export] Slider GiveAmountSlider;
		[Export] SpinBox TakeAmountBox;
		[Export] Button MakeOfferButton;

		[Export] Label NoOffersLabel;
		[Export] PackedScene TradePartnerSliderScene;

		Faction me;
		Faction partner;

		TradeOfferDesc tofferDesc;


		public override void _Ready() {
			GiveResourceList.ItemSelected += OnGiveResourceSelected;
			TakeResourceList.ItemSelected += OnTakeResourceSelected;
			GiveAmountSlider.ValueChanged += OnGiveAmountValueChanged;
			TakeAmountBox.ValueChanged += OnTakeAmountValueChanged;
		}

		public void Display(Faction me, Faction partner) {
			this.me = me;
			this.partner = partner;

			ResetTradeOfferMaking();
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

			UpdateSendingInterface();
		}

		void UpdateSendingInterface() {
			GiveResourceList.Clear();
			GiveResourceList.AddItem("silver");
			foreach (var rest in me.Resources) {
				GiveResourceList.AddItem(rest.Key.AssetName);
			}

			TakeResourceList.Clear();
			TakeResourceList.AddItem("silver");
			foreach (var rest in Registry.Resources.GetAssets()) {
				TakeResourceList.AddItem(rest.AssetName);
			}
		}

		void ResetTradeOfferMaking() {
			tofferDesc = new(me, partner);
			MakeOfferButton.Disabled = true;
			GiveResourceList.Selected = -1;
			TakeResourceList.Selected = -1;
		}

		void OnGiveResourceSelected(long which) {

		}

		void OnTakeResourceSelected(long which) {

		}

		void OnGiveAmountValueChanged(double howmuch) {

		}

		void OnTakeAmountValueChanged(double howmuch) {

		}

		void OnMakeOfferPressed() {
			me.SendTradeOfferTo(partner, tofferDesc.GetOffer());
		}


		class TradeOfferDesc(Faction offerer, Faction recipient) {

			public Faction Offerer = offerer;
			public Faction Recipient = recipient;

			public int StoredUnits;

			public bool OfferSilver;

			public int SilverAmount;
			public ResourceBundle Resources;


			public TradeOffer GetOffer() {
				if (OfferSilver) {
					return new(Offerer, SilverAmount, Recipient, Resources, StoredUnits);
				} else {
					return new(Offerer, Resources, Recipient, SilverAmount, StoredUnits);
				}
			}

		}

	}

}

