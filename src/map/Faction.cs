using System;
using System.Collections.Generic;
using System.Linq;
using static Document;

public interface IEntity {

	Briefcase Briefcase { get; }
	string DocName { get; }
	TimeT GetTime();
	void ContractFailure(Document doc, Point fulfillFailure);
	void ContractSuccess(Document doc);

	ResourceStorage Resources { get; }

}

public partial class Faction : IEntity {

	//readonly List<Region> ownedRegions; public List<Region> OwnedRegions { get => ownedRegions; }
	readonly List<RegionFaction> ownedFactions = new();

	public ResourceStorage Resources { get; init; }

	public string DocName => ToString();
	public Briefcase Briefcase { get; init; }

	TimeT time;


	public Faction() {
		Briefcase = new();
		Resources = new();
		Resources.IncreaseCapacity(8000);
	}

	public RegionFaction CreateOwnedFaction(Region region) {
		var fac = new RegionFaction(region, this);
		ownedFactions.Add(fac);
		return fac;
	}

	public RegionFaction GetOwnedRegionFaction(int ix) {
		return ownedFactions[ix];
	}

	public void PassTime(TimeT minutes) {

		var prevHour = time / 60;
		var nextHour = (time + minutes) / 60;
		var diff = nextHour - prevHour;

		for (ulong i = 1; i <= diff; i++) {
			//GD.PrintS("checking priefcase ", prevHour, nextHour, prevHour * 60 + i * 60);
			Briefcase.Check(prevHour * 60 + i * 60);
		}

		time += minutes;
	}

	public TimeT GetTime() => time;

	public void ContractFailure(Document doc, Document.Point failurePoint) {

	}

	public void ContractSuccess(Document doc) {
		var others = doc.Parties.Where((e) => e != this).ToList();

		// placeholder!! TODO hold place with something better
		const float MULTIPLY_RESOURCE_COSTS_EVERY_SUCCESS_BY = 1.1f;
		if (doc.type == Document.Type.MANDATE_CONTRACT) {
			var newdoc = Briefcase.CreateExportMandate(
				doc.Points[0].Resources.Select((j) => new ResourceBundle(j.Type, (int)Math.Round(j.Amount * MULTIPLY_RESOURCE_COSTS_EVERY_SUCCESS_BY))).ToList(),
				doc.Points[1].Resources,
				this,
				(RegionFaction)others[0], // trust in the cast
				GetTime() + GameTime.DAYS_PER_WEEK * GameTime.HOURS_PER_DAY * GameTime.MINUTES_PER_HOUR
			);
			((RegionFaction)others[0]).Briefcase.AddDocument(newdoc);
		}
	}
}
