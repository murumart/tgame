using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using resources.game.resource_types;
using scenes.autoload;

namespace scenes.region.ui {

	public partial class TradePartnerDisplay : Control {

		public event Action TradedOrCanceled;
		public event Action TradeOfferSent;

		[Export] Label PartnerNameLabel;
		[Export] Container TradeParnerOfferList;
		[Export] Container GottenOffersParent;
		[Export] Container MyOfferList;

		[Export] Label NewOfferLabel;
		[Export] Label SendAmountLabel;
		[Export] Container SentOffersParent;
		[Export] OptionButton GiveResourceList;
		[Export] OptionButton TakeResourceList;
		[Export] SpinBox GiveAmountBox;
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
			GiveAmountBox.ValueChanged += OnGiveAmountValueChanged;
			TakeAmountBox.ValueChanged += OnTakeAmountValueChanged;
			MakeOfferButton.Pressed += OnMakeOfferPressed;

			if (GetTree().CurrentScene == this) {
				var reg1 = new Region(0, Vector2I.Zero, new(){{Vector2I.Zero, GroundTileType.HasLand}});
				var fac1 = new Faction(reg1);
				fac1.Resources.AddResources(Registry.Resources.GetAssets().Select(a => new ResourceBundle(a, GD.RandRange(10, 30))));
				var reg2 = new Region(0, Vector2I.One, new(){{Vector2I.Zero, GroundTileType.HasLand}});
				var fac2 = new Faction(reg2);
				fac1.AddTradePartner(fac2);
				fac2.AddTradePartner(fac1);
				Display(fac1, fac2);
				UILayer.DebugDisplay(() => tofferDesc?.ToString() + $"\ngs:{GiveResourceList.Selected}, ts:{TakeResourceList.Selected}");
			}
		}

		public void Display(Faction me, Faction partner) {
			this.me = me;
			this.partner = partner;

			ResetTradeOfferMaking();
			var has = me.GetGottenTradeOffers(partner, out var gottenOffers);
			has = me.GetSentTradeOffers(partner, out var sentOffers) || has;
			Debug.Assert(has, "Don't have trade relations with this faction");

			PartnerNameLabel.Text = partner.Name;
			if (me.IsAtWarWith(partner)) {
				PartnerNameLabel.Text += " (AT WAR WITH)";
				GottenOffersParent.Hide();
				SentOffersParent.Hide();
				NoOffersLabel.Hide();
				return;
			}

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

			InitSendingInterface();
		}

		void InitSendingInterface() {
			GiveResourceList.Clear();
			GiveResourceList.AddItem("silver");
			foreach (var rest in me.Resources) {
				int idx = GiveResourceList.ItemCount;
				GiveResourceList.AddIconItem(((ResourceType)rest.Key).Icon, rest.Key.AssetName);
				string idstr = rest.Key.GetIdString();
				GiveResourceList.SetItemMetadata(idx, Variant.CreateFrom(idstr));
			}
			GiveResourceList.Select(-1);

			TakeResourceList.Clear();
			TakeResourceList.AddItem("silver");
			foreach (var rest in Registry.Resources.GetAssets()) {
				int idx = TakeResourceList.ItemCount;
				TakeResourceList.AddIconItem(((ResourceType)rest).Icon, rest.AssetName);
				TakeResourceList.SetItemMetadata(idx, Variant.CreateFrom(rest.GetIdString()));
			}
			TakeResourceList.Select(-1);
			UpdateTofferDesc();
		}

		void UpdateTofferDesc() {
			tofferDesc.OfferSilver = GiveResourceList.Selected == 0;
			tofferDesc.SilverAmount = tofferDesc.OfferSilver ? (int)GiveAmountBox.Value : (int)TakeAmountBox.Value;
			if (tofferDesc.OfferSilver) {
				if (TakeResourceList.Selected == -1) tofferDesc.Resources = new(new resources.game.resource_types.ResourceType(), 0);
				else tofferDesc.Resources = new(
					Registry.Resources.GetAsset(TakeResourceList.GetItemMetadata(TakeResourceList.Selected).AsString()),
					(int)TakeAmountBox.Value
				);
				SendAmountLabel.Text = $"{tofferDesc.SilverAmount}/{me.Silver}";
			} else {
				if (GiveResourceList.Selected == -1) tofferDesc.Resources = new(new resources.game.resource_types.ResourceType(), 0);
				else tofferDesc.Resources = new(
					Registry.Resources.GetAsset(GiveResourceList.GetItemMetadata(GiveResourceList.Selected).AsString()),
					(int)GiveAmountBox.Value
			   );
				SendAmountLabel.Text = $"{tofferDesc.Resources.Amount}/{me.Resources.GetCount(tofferDesc.Resources.Type)}";
			}
			NewOfferLabel.Text = "Create new offer: " + tofferDesc.ToString();
			MakeOfferButton.Disabled = !tofferDesc.CanSend();
		}

		void ResetTradeOfferMaking() {
			tofferDesc = new(me, partner);
			MakeOfferButton.Disabled = true;
			GiveResourceList.Selected = -1;
			TakeResourceList.Selected = -1;
		}

		void OnGiveResourceSelected(long which) {
			if (which == 0) /* want to send silver */ {
				if (!tofferDesc.OfferSilver) {
					TakeResourceList.Select(-1);
				}
				GiveAmountBox.MaxValue = me.Silver;
				GiveAmountBox.Value = Mathf.Min(GiveAmountBox.Value, GiveAmountBox.MaxValue);
				UpdateTofferDesc();
				return;
			}
			TakeResourceList.Select(0);
			UpdateTofferDesc();
			GiveAmountBox.MaxValue = me.Resources.GetCount(tofferDesc.Resources.Type);
			GiveAmountBox.Value = Mathf.Min(GiveAmountBox.Value, GiveAmountBox.MaxValue);
		}

		void OnTakeResourceSelected(long which) {
			if (which == 0) {
				if (tofferDesc.OfferSilver) {
					GiveResourceList.Select(-1);
				}
				UpdateTofferDesc();
				return;
			}
			GiveResourceList.Select(0);
			UpdateTofferDesc();
			GiveAmountBox.MaxValue = me.Silver;
			GiveAmountBox.Value = Mathf.Min(GiveAmountBox.Value, GiveAmountBox.MaxValue);
		}

		void OnGiveAmountValueChanged(double howmuch) {
			int amt = (int)howmuch;
			if (tofferDesc.OfferSilver) tofferDesc.SilverAmount = amt;
			else tofferDesc.Resources = new(tofferDesc.Resources.Type, amt);
			UpdateTofferDesc();
		}

		void OnTakeAmountValueChanged(double howmuch) {
			int amt = (int)howmuch;
			if (!tofferDesc.OfferSilver) tofferDesc.SilverAmount = amt;
			else tofferDesc.Resources = new(tofferDesc.Resources.Type, amt);
			UpdateTofferDesc();
		}

		void OnMakeOfferPressed() {
			me.SendTradeOfferTo(partner, tofferDesc.GetOffer());
			InitSendingInterface();
			TradeOfferSent?.Invoke();
		}


		class TradeOfferDesc(Faction offerer, Faction recipient) {

			public Faction Offerer = offerer;
			public Faction Recipient = recipient;

			public bool OfferSilver;

			public int SilverAmount = 1;
			public ResourceBundle Resources = new(new resources.game.resource_types.ResourceType(), 0);


			public TradeOffer GetOffer() {
				if (OfferSilver) {
					return new(Offerer, SilverAmount, Recipient, Resources, true);
				} else {
					return new(Offerer, Resources, Recipient, SilverAmount, true);
				}
			}

			public bool CanSend() {
				return Resources.Amount != 0;
			}

			public override string ToString() {
				if (OfferSilver) {
					return $"{SilverAmount} silver for {(Resources.Amount != 0 ? (Resources) : "...?")}";
				}
				return $"{(Resources.Amount != 0 ? (Resources) : "...?")} for {SilverAmount} silver";
			}

		}

	}

}

