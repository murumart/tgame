using System;
using System.Collections.Generic;
using System.Linq;

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
