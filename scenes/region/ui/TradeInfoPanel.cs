using System;
using System.Collections.Generic;
using Godot;

namespace scenes.region.ui {

	public partial class TradeInfoPanel : Control {

		[Export] Container PartnerList;
		[Export] Label NoPartnersLabel;
		[Export] PackedScene PartnerDisplay;


		public override void _Ready() {
			foreach (var c in PartnerList.GetChildren()) c.QueueFree();
		}

		public void Display(Faction me, Dictionary<Faction, List<TradeOffer>> tradeInfo) {
			GD.Print("TradeInfoPanel::Display : displaying trade info");
			foreach (var c in PartnerList.GetChildren()) c.QueueFree();
			foreach (var partner in tradeInfo.Keys) {
				var display = PartnerDisplay.Instantiate<TradePartnerDisplay>();
				PartnerList.AddChild(display);
				display.Display(me, partner);
				display.TradedOrCanceled += () => Display(me, tradeInfo); // rebuild UI when something is done
			}
			NoPartnersLabel.Visible = tradeInfo.Keys.Count == 0;
		}

	}

}

