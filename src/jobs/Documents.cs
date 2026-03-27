using System;
using System.Collections.Generic;
using System.Text;
using Godot;


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
			IEnumerable<ResourceConsumer> requirements,
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
	public readonly bool OffererGivesRecipientSilver;

	public int StoredUnits { get; private set; }

	readonly int giveSilverUnit;
	public int OffererPaidSilverUnit {
		get {
			Debug.Assert(OffererGivesRecipientSilver, "Looking at silver makes no sense when we're not buying with silver");
			return giveSilverUnit;
		}
	}
	readonly ResourceBundle giveResourcesUnit;
	public ResourceBundle OffererSoldResourcesUnit {
		get {
			Debug.Assert(!OffererGivesRecipientSilver, "Looking at offered resources makes no sense when we're buying with silver");
			return giveResourcesUnit;
		}
	}

	readonly Faction acceptor;
	readonly int takeSilverUnit;
	public int RecipientPaidSilverUnit {
		get {
			Debug.Assert(!OffererGivesRecipientSilver, "No sense looking at getting silver when we're already buying with silver");
			return takeSilverUnit;
		}
	}
	readonly ResourceBundle takeResourcesUnit;
	public ResourceBundle RecepientRequiredResourcesUnit {
		get {
			Debug.Assert(OffererGivesRecipientSilver, "No sense looking at getting resources when we're expecting silver for our resources");
			return takeResourcesUnit;
		}
	}

	public Faction Offerer => starter;
	public Faction Recipient => acceptor;

	public TimeT CreationMinute { get; init; }
	public TimeT LastInteractionMinute { get; private set; }

	bool valid = false;
	public bool IsValid => valid;

	StringBuilder debugHistory = new();
	public string History => debugHistory.ToString();


	public TradeOffer(Faction starter, int gives, Faction acceptor, ResourceBundle wants, bool exact) {

		int minv = Math.Min(gives, wants.Amount);
		int maxv = Math.Max(gives, wants.Amount);
		bool miniss = minv == gives;
		int maxUnits;
		if (exact && maxv % minv != 0) maxUnits = 1;
		else maxUnits = minv;

		Debug.Assert(maxUnits > 0, "No sense to make offer to trade 0 (or less) of something");
		StoredUnits = maxUnits;
		Debug.Assert(starter != null);
		this.starter = starter;
		Debug.Assert(starter.Silver >= gives, "Don't have enough silver to create trade offer");
		Debug.Assert(gives > 0, "Need to give MORE THAN 0 silver to make a trade");
		Debug.Assert(acceptor != null);
		this.acceptor = acceptor;
		OffererGivesRecipientSilver = true;

		giveSilverUnit = gives / maxUnits;
		starter.SubtractAndReturnSilver(gives);
		takeResourcesUnit = wants.Divide(maxUnits);

		CreationMinute = starter.GetTime();
		LastInteractionMinute = CreationMinute;
		valid = true;
		Log("created with first constructor ");
	}

	public TradeOffer(Faction starter, ResourceBundle gives, Faction acceptor, int wants, bool exact) {

		int minv = Math.Min(gives.Amount, wants);
		int maxv = Math.Max(gives.Amount, wants);
		bool minisr = minv == gives.Amount;
		int maxUnits;
		if (exact && (maxv % minv != 0)) maxUnits = 1;
		else maxUnits = minv;

		Debug.Assert(maxUnits > 0, "No sense to make offer to trade 0 (or less) of something");
		StoredUnits = maxUnits;
		Debug.Assert(starter != null);
		this.starter = starter;
		Debug.Assert(starter.Resources.HasEnough(gives.Type, gives.Amount), "Don't have enough resources to create trade offer");
		Debug.Assert(acceptor != null);
		Debug.Assert(wants > 0, "Need to take MORE THAN 0 silver to make a trade");
		this.acceptor = acceptor;
		OffererGivesRecipientSilver = false;

		giveResourcesUnit = gives.Divide(maxUnits);
		starter.Resources.GetTransfer(gives);
		takeSilverUnit = wants / maxUnits;

		CreationMinute = starter.GetTime();
		LastInteractionMinute = CreationMinute;
		valid = true;
		Log("created with second constructor ");
	}

	public void Cancel() {
		Debug.Assert(valid, "Trade offer invalid, can't cancel, please delete");
		if (OffererGivesRecipientSilver) {
			starter.ReceiveTransferSilver(OffererPaidSilverUnit * StoredUnits);
		} else {
			starter.Resources.AddResource(OffererSoldResourcesUnit.Multiply(StoredUnits));
		}
		Log("cancelled, now invalid ");

		valid = false;
	}

	public int GetMaxUnitsTradeable() {
		int max;
		if (OffererGivesRecipientSilver) {
			max = acceptor.Resources.GetCount(RecepientRequiredResourcesUnit.Type) / (RecepientRequiredResourcesUnit.Amount * StoredUnits);
		} else {
			max = acceptor.Silver / (RecipientPaidSilverUnit * StoredUnits);
		}
		return Math.Min(StoredUnits, max);
	}

	public bool CanTrade(int units) {
		Debug.Assert(valid, "Trade offer invalid, please delete this");
		Debug.Assert(units > 0, "No sense to trade <= 0 units");
		Debug.Assert(units <= StoredUnits, "Can't trade more units than contained in offer");
		Log("checking trade possibolity ");
		if (OffererGivesRecipientSilver) {
			var m = RecepientRequiredResourcesUnit.Multiply(units);
			return acceptor.Resources.HasEnough(m.Type, m.Amount);
		} else {
			return acceptor.Silver >= RecipientPaidSilverUnit * units;
		}
	}

	public void MakeTrade(int units) {
		Debug.Assert(valid, "Trade offer invalid, please delete this");
		Debug.Assert(units > 0, "No sense to trade <= 0 units");
		Debug.Assert(units <= StoredUnits, "Can't trade more units than contained in offer");
		Debug.Assert(CanTrade(units), "Can't even trade Brooooo");
		string str = this.ToString();
		if (OffererGivesRecipientSilver) {
			MakeTradeOffererGivesSilver(units);
		} else {
			MakeTradeAcceptorGivesSilver(units);
		}
		LastInteractionMinute = Offerer.GetTime();
		Debug.Assert(StoredUnits >= 0);
		Log("made trade");
		GD.Print($"TradeOffer::MakeTrade : made trade {str} (traded {units} units)");
		if (StoredUnits == 0) valid = false;
		if (!valid) Log("drained...");
	}

	void MakeTradeOffererGivesSilver(int units) {
		Debug.Assert(OffererPaidSilverUnit * units > 0, "This shouldn't fail 1.");
		acceptor.ReceiveTransferSilver(OffererPaidSilverUnit * units);
		acceptor.Resources.TransferResource(starter.Resources, RecepientRequiredResourcesUnit.Multiply(units));
		StoredUnits -= units;
	}

	void MakeTradeAcceptorGivesSilver(int units) {
		acceptor.Resources.AddResource(OffererSoldResourcesUnit.Multiply(units));
		Debug.Assert(RecipientPaidSilverUnit * units > 0, "This shouldn't fail 2.");
		starter.ReceiveTransferSilver(acceptor.SubtractAndReturnSilver(RecipientPaidSilverUnit * units));
		StoredUnits -= units;
	}

	public void Log(string message) {
		debugHistory.Append(starter.GetTime()).Append("; ").Append("stored: ").Append(StoredUnits).Append("; valid: ").Append(valid).Append("; ").Append(message).Append('\n');
	}

	public string GetOutputDescription(Faction side, int units) {
		Debug.Assert(side == acceptor || side == starter);
		if (side == acceptor) {
			if (OffererGivesRecipientSilver) return $"+{OffererPaidSilverUnit * units} silver";
			else return $"+{OffererSoldResourcesUnit.Multiply(units)}";
		}
		if (OffererGivesRecipientSilver) return $"+{RecepientRequiredResourcesUnit.Multiply(units)}";
		else return $"+{RecipientPaidSilverUnit * units} silver";
	}

	public override string ToString() {
		if (OffererGivesRecipientSilver) {
			return $"TradeOffer: {Offerer} gives {Recipient} {giveSilverUnit} silver for {RecepientRequiredResourcesUnit} ({StoredUnits} units stored)";
		}
		return $"TradeOffer: {Offerer} gives {Recipient} {OffererSoldResourcesUnit} for {RecipientPaidSilverUnit} silver ({StoredUnits} units stored)";
	}

}


