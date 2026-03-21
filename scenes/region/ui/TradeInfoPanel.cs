using System;
using System.Collections.Generic;
using Godot;

namespace scenes.region.ui {

	public partial class TradeInfoPanel : Control {

		[Export] Container PartnerList;
		[Export] RichTextLabel DescriptionLabel;
		[Export] Label NoPartnersLabel;
		[Export] Container PartnersDisplayParent;
		[Export] PackedScene PartnerDisplay;


		public override void _Ready() {
			foreach (var c in PartnerList.GetChildren()) c.QueueFree();
		}

		public void Display(Faction me, Dictionary<Faction, List<TradeOffer>> tradeInfo) {
			GD.Print("TradeInfoPanel::Display : displaying trade info");
			DescriptionLabel.Text = "This is, in fact, trading.";
			foreach (var c in PartnerList.GetChildren()) c.QueueFree();
			foreach (var partner in tradeInfo.Keys) {
				var display = PartnerDisplay.Instantiate<TradePartnerDisplay>();
				PartnerList.AddChild(display);
				display.Display(me, partner);
				display.TradedOrCanceled += () => Display(me, tradeInfo); // rebuild UI when something is done
				display.TradeOfferSent += () => Display(me, tradeInfo);
			}
			PartnersDisplayParent.Visible = tradeInfo.Keys.Count != 0;
			NoPartnersLabel.Visible = tradeInfo.Keys.Count == 0;
		}

		internal void DisplayNoMarket() {
			DescriptionLabel.Text = "Build a Marketplace and assign people to work there to trade!";
			foreach (var c in PartnerList.GetChildren()) c.QueueFree();
			NoPartnersLabel.Hide();
			PartnersDisplayParent.Hide();
		}
	}

}

