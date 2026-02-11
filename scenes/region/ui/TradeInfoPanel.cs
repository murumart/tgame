using System;
using Godot;

namespace scenes.region.ui {

	public partial class TradeInfoPanel : Control {

        [Export] Container PartnerList;


        public override void _Ready() {
            foreach (var c in PartnerList.GetChildren()) c.QueueFree();
        }

	}

}

