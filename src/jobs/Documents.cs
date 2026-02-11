using System;
using System.Collections.Generic;

public partial class Document {

	public DocType Type { get; init; }
	public TimeT Expires { get; init; }
	public Faction SideA { get; init; }
	public Faction SideB { get; init; }

	readonly Func<FulfillResult> fulfill;

	public bool DeadlinePassed { get; private set; }
	public bool Fulfilled { get; private set; }

	public string Title { get; private set; }
	public string FluffText { get; init; }

	public object Meta { get; init; }


	private Document(
			DocType type,
			TimeT expires,
			Faction sideA,
			Faction sideB,
			Func<FulfillResult> fulfill,
			string title,
			string fluffText = "",
			object meta = null
	) {
		this.fulfill = fulfill;
		Type = type;
		Expires = expires;
		SideA = sideA;
		SideB = sideB;
		FluffText = fluffText;
		Title = title;
		Meta = meta;
	}

	private void Pass() => DeadlinePassed = true;

	public string GetText() {
		var str = FluffText + '\n';

		if ((Type & DocType.HasDeadline) != 0) str += Expires >= SideA.GetTime()
					? $"The contract must be fulfilled in {(GameTime.GetFancyTimeString(Expires - SideA.GetTime()))}."
					: $"The contract has " + (Fulfilled ? "been completed." : "failed.");

		return str;
	}

	private FulfillResult Fulfill() {
		return fulfill();
	}

}

partial class Document {

	[Flags]
	public enum DocType {

		Invalid = 0,

		AColoniallyOwnsB     = 0b00001,
		AMandatesExportFromB = 0b00010,

		HasDeadline             = 0b0010000,
		UnilateralACancellation = 0b0100000,
		UnilateralBCancellation = 0b1000000,

	}

	public enum FulfillResult {
		Ok,
		BDidntHaveResources,
	}

}

partial class Document {

	public class Briefcase {

		readonly Dictionary<Document.DocType, List<Document>> documents = new();


		public Briefcase() { }

		public void Check(TimeT time) {
			var active = GetActiveDocuments();
			foreach (var doc in active) {
				if (time >= doc.Expires && (doc.Type & DocType.HasDeadline) != 0) {
					var result = doc.Fulfill();
					if (result != FulfillResult.Ok) {
						doc.SideA.ContractFailure(doc, result);
						doc.SideB.ContractFailure(doc, result);
					} else {
						doc.SideA.ContractSuccess(doc);
						doc.SideB.ContractSuccess(doc);
					}
					doc.Pass();
				}
			}
		}

		public IEnumerable<Document> GetActiveDocuments() {
			List<Document> list = new();
			foreach (var doclist in documents.Values) foreach (var doc in doclist) if (!doc.DeadlinePassed) list.Add(doc);
			return list;
		}

		public IEnumerable<Document> GetInactiveDocuments() {
			List<Document> list = new();
			foreach (var doclist in documents.Values) foreach (var doc in doclist) if (doc.DeadlinePassed) list.Add(doc);
			return list;
		}

		public void AddDocument(Document.DocType doctype, Document document) {
			if (!documents.ContainsKey(doctype)) documents[doctype] = new();
			List<Document> list = documents[doctype];
			list.Add(document);
		}

		public Document CreateExportMandate(
			IEnumerable<ResourceBundle> requirements,
			IEnumerable<ResourceBundle> rewards,
			Faction parentFaction,
			Faction regionFaction,
			TimeT due
		) {
			var ptype = DocType.AMandatesExportFromB;
			string fluff = "Contract to produce resources for the motherland.";
			var doc = new Document(DocType.AMandatesExportFromB | DocType.HasDeadline, due, parentFaction, regionFaction, () => {
				if (regionFaction.Resources.HasEnough(requirements)) {
					regionFaction.Resources.TransferResources(parentFaction.Resources, requirements);
					// rewards aren't subtracted from parent FOR NOW
					regionFaction.Resources.AddResources(rewards);
					return FulfillResult.Ok;
				}
				return FulfillResult.BDidntHaveResources;
			}, title: "Export Mandate", fluffText: fluff, meta: (requirements, rewards)) {

			};

			if (!documents.ContainsKey(ptype)) documents[ptype] = new();
			List<Document> list = documents[ptype];
			doc.Title = "Export Mandate #" + list.Count;
			list.Add(doc);

			return doc;
		}

		public Document CreateOwningRelationship(Faction ownerFaction, Faction ownedFaction) {
			var doc = new Document(
				DocType.AColoniallyOwnsB,
				GameTime.Years(500),
				sideA: ownerFaction,
				sideB: ownedFaction,
				fulfill: () => FulfillResult.Ok,
				title: $"Ownership of {ownedFaction.Name} by {ownerFaction}",
				fluffText: "A colonial ownership contract."
			) {

			};

			AddDocument(DocType.AColoniallyOwnsB, doc);
			return doc;
		}

		public bool ContainsDocType(DocType type) => documents.ContainsKey(type);

		public Document GetOwnershipDocument() {
			var has = documents.TryGetValue(DocType.AColoniallyOwnsB, out var list);
			Debug.Assert(has, "No ownership document in briefcase");
			return list[0];
		}
	}

}

public partial class TradeOffer {

	readonly Faction starter;
	public readonly bool BuyingWithSilver;

	public int StoredUnits { get; private set; }

	public readonly int GiveSilverUnit = 0;
	public readonly ResourceBundle GiveResourcesUnit;

	readonly Faction acceptor;
	public readonly int TakeSilverUnit = 0;
	public readonly ResourceBundle TakeResourcesUnit;

	bool valid = false;
	public bool IsValid => valid;


	public TradeOffer(Faction starter, int gives, Faction acceptor, ResourceBundle wants, int maxUnits) {
		Debug.Assert(maxUnits > 0, "No sense to make offer to trade 0 (or less) of something");
		StoredUnits = maxUnits;
		Debug.Assert(starter != null);
		this.starter = starter;
		Debug.Assert(starter.Silver >= gives * maxUnits, "Don't have enough silver to create trade offer");
		Debug.Assert(acceptor != null);
		this.acceptor = acceptor;
		BuyingWithSilver = true;

		GiveSilverUnit = gives;
		starter.SubtractAndReturnSilver(gives * maxUnits);
		TakeResourcesUnit = wants;

		valid = true;
	}

	public TradeOffer(Faction starter, ResourceBundle gives, Faction acceptor, int wants, int maxUnits) {
		Debug.Assert(maxUnits > 0, "No sense to make offer to trade 0 (or less) of something");
		StoredUnits = maxUnits;
		Debug.Assert(starter != null);
		this.starter = starter;
		Debug.Assert(starter.Resources.HasEnough(gives.Multiply(maxUnits)), "Don't have enough resources to create trade offer");
		Debug.Assert(acceptor != null);
		this.acceptor = acceptor;
		BuyingWithSilver = false;

		GiveResourcesUnit = gives;
		starter.Resources.GetTransfer(gives.Multiply(StoredUnits));
		TakeSilverUnit = wants;

		valid = true;
	}

	public void Cancel() {
		Debug.Assert(valid, "Trade offer invalid, can't cancel, please delete");
		if (BuyingWithSilver) {
			starter.ReceiveTransferSilver(GiveSilverUnit);
		} else {
			starter.Resources.AddResource(GiveResourcesUnit);
		}

		valid = false;
	}

	public bool CanTrade(int units) {
		Debug.Assert(valid, "Trade offer invalid, please delete this");
		Debug.Assert(units > 0, "No sense to trade <= 0 units");
		Debug.Assert(units <= StoredUnits, "Can't trade more units than contained in offer");
		if (BuyingWithSilver) {
			return acceptor.Resources.HasEnough(TakeResourcesUnit.Multiply(units));
		} else {
			return acceptor.Silver >= TakeSilverUnit * units;
		}
	}

	public void MakeTrade(int units) {
		Debug.Assert(valid, "Trade offer invalid, please delete this");
		Debug.Assert(units > 0, "No sense to trade <= 0 units");
		Debug.Assert(units <= StoredUnits, "Can't trade more units than contained in offer");
		Debug.Assert(CanTrade(units), "Can't even trade Brooooo");
		if (BuyingWithSilver) {
			acceptor.ReceiveTransferSilver(GiveSilverUnit * units);
			acceptor.Resources.TransferResources(starter.Resources, TakeResourcesUnit.Multiply(units));
			StoredUnits -= units;
		} else {
			acceptor.Resources.AddResource(GiveResourcesUnit.Multiply(units));
			starter.ReceiveTransferSilver(acceptor.SubtractAndReturnSilver(TakeSilverUnit * units));
			StoredUnits -= units;
		}
		Debug.Assert(units >= 0);
		if (units == 0) valid = false;
	}

}


