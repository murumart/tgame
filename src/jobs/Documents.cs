using System;
using System.Collections.Generic;
using System.Linq;

public partial class Document {

	public DocType Type { get; init; }
	public TimeT Expires { get; init; }
	public Faction SideA { get; init; }
	public Faction SideB { get; init; }
	public string FluffText { get; init; }

	bool passed;
	public bool Passed => passed;
	public bool Fulfilled { get; private set; }

	public Point[] Points { get; init; }
	public string Title { get; private set; }


	private Document(DocType type, TimeT expires, Faction sideA, Faction sideB, string fluffText) {
		Type = type;
		Expires = expires;
		SideA = sideA;
		SideB = sideB;
		FluffText = fluffText;
	}

	private void Pass() => passed = true;

	public string GetText() {
		var str = FluffText + '\n';

		foreach (var point in Points) {
			str += point.GetDescription() + '\n';
		}

		str += Type switch {
			DocType.DeadlineContract =>
				Expires >= Points[0].SideA.GetTime()
					? $"The contract must be fulfilled in {(GameTime.GetFancyTimeString(Expires - Points[0].SideA.GetTime()))}."
					: $"The contract has " + (Fulfilled ? "been completed." : "failed."),
			DocType.NoDeadlineContract => "",
			_ => throw new System.NotImplementedException(),
		} + '\n'; ;

		return str;
	}

	private Point GetFulfillingFailure() {
		foreach (var pt in Points) {
			if (!pt.CanBeFulfilled()) return pt;
		}
		return null;
	}

	private void Fulfill() {
		foreach (var pt in Points) {
			pt.Fulfill();
		}
		Fulfilled = true;
	}

}

partial class Document {

	public partial class Point(
		IEntity sideA,
		Point.Type type,
		IEntity sideB
	) {

		public IEntity SideA => sideA;
		public Point.Type PType => type;
		public IEntity SideB => sideB;

		public List<ResourceBundle> Resources { get; init; }
		int amount;


		public bool CanBeFulfilled() {
			switch (type) {
				case Type.ProvidesResourcesTo: {
						return sideA.Resources.HasEnoughAll(Resources);
					}
			}
			throw new NotImplementedException("This type's fulfillment check is unimplemented!");
		}

		public void Fulfill() {
			switch (type) {
				case Type.ProvidesResourcesTo: {
						sideA.Resources.TransferResources(sideB.Resources, Resources);
						return;
					}
			}
			throw new NotImplementedException("This type's fulfillment is unimplemented!");
		}

		public string GetDescription() {
			return type switch {
				Type.ProvidesResourcesTo => Resources.Count == 0 ? "" :
					$"* {sideA.DocName} provides the following resources to {sideB.DocName}:"
					+ "\n" + Resources.Aggregate("", (s, r) => s + "\n - " + r.Type.AssetName + " x " + r.Amount),
				Type.ProvidesWorkersTo => $"* {sideA.DocName} provides {amount} workers to {sideB.DocName}, or less according to the recipient's available living space",
				Type.HasColony => $"* {sideB.DocName} is a colony of {sideA.DocName}",
				_ => throw new System.NotImplementedException(),
			};
		}

	}

}

partial class Document {

	public enum DocType {

		DeadlineContract,
		NoDeadlineContract,

	}

	partial class Point {

		public enum Type {

			HasColony,
			ProvidesResourcesTo,
			ProvidesWorkersTo,

		}

	}

	public bool ContainsPointType(Point.Type type) => Points.Any(p => p.PType == type);

}

partial class Document {

	public class Briefcase {

		readonly Dictionary<Document.Point.Type, List<Document>> documents;


		public Briefcase() {
			documents = new();
		}

		public void Check(TimeT time) {
			var active = GetActiveDocuments();
			foreach (var doc in active) {
				if (time >= doc.Expires) {
					var fulfillFailure = doc.GetFulfillingFailure();
					if (fulfillFailure != null) {
						doc.SideA.ContractFailure(doc, fulfillFailure);
						doc.SideB.ContractFailure(doc, fulfillFailure);
					} else {
						doc.SideA.ContractSuccess(doc);
						doc.SideB.ContractSuccess(doc);
						doc.Fulfill();
					}
					doc.Pass();
				}
			}
		}

		public bool ContainsPointType(Point.Type type) => documents.ContainsKey(type);

		public IEnumerable<Document> GetActiveDocuments() {
			List<Document> list = new();
			foreach (var doclist in documents.Values) foreach (var doc in doclist) if (!doc.Passed) list.Add(doc);
			return list;
		}

		public IEnumerable<Document> GetInactiveDocuments() {
			List<Document> list = new();
			foreach (var doclist in documents.Values) foreach (var doc in doclist) if (doc.Passed) list.Add(doc);
			return list;
		}

		public void AddDocument(Document.Point.Type ptype, Document document) {
			if (!documents.ContainsKey(ptype)) documents[ptype] = new();
			List<Document> list = documents[ptype];
			list.Add(document);
		}

		public Document CreateExportMandate(
			List<ResourceBundle> requirements,
			List<ResourceBundle> rewards,
			Faction parentFaction,
			Faction regionFaction,
			TimeT due
		) {
			var ptype = Document.Point.Type.ProvidesResourcesTo;
			var doc = new Document(DocType.DeadlineContract, due, parentFaction, regionFaction, "Contract to produce resources for the motherland.") {

				Points = new Point[] {
					new(regionFaction, Point.Type.ProvidesResourcesTo, parentFaction) { Resources = requirements },
					new(parentFaction, Point.Type.ProvidesResourcesTo, regionFaction) { Resources = rewards },
				}
			};

			if (!documents.ContainsKey(ptype)) documents[ptype] = new();
			List<Document> list = documents[ptype];
			doc.Title = "Export Mandate #" + list.Count;
			list.Add(doc);

			return doc;
		}

		public Document CreateOwningRelationship(Faction ownerFaction, Faction ownedFaction) {
			var doc = new Document(
				DocType.NoDeadlineContract,
				(TimeT)GameTime.SECS_TO_HOURS
					* GameTime.HOURS_PER_DAY
					* GameTime.DAYS_PER_WEEK
					* GameTime.WEEKS_PER_MONTH
					* GameTime.MONTHS_PER_YEAR
					* 500,
				ownerFaction,
				ownedFaction,
				"A colonial ownership contract."
			) {
				Points = new Point[] {
					new(ownerFaction, Point.Type.HasColony, ownedFaction),
				}
			};

			AddDocument(Point.Type.HasColony, doc);
			return doc;
		}

		public Document GetOwnerDocument() => documents[Point.Type.HasColony][0];
	}

}
