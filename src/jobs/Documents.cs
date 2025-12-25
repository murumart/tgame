using System;
using System.Collections.Generic;
using System.Linq;

public partial class Document {

	public Type type { get; init; }
	public TimeT Expires { get; init; }
	public IEntity SideA {get; init;}
	public IEntity SideB {get; init;}

	bool passed;
	public bool Passed => passed;
	public bool Fulfilled { get; private set; }

	public Point[] Points { get; init; }
	public string Title { get; private set; }


	private Document(Type type, TimeT expires, IEntity sideA, IEntity sideB) {
		this.type = type;
		Expires = expires;
		SideA = sideA;
		SideB = sideB;
	}

	private void Pass() => passed = true;

	public string GetText() {
		var str = type switch {
			Type.MANDATE_CONTRACT => "Contract to produce resources for the motherland.",
			_ => throw new System.NotImplementedException(),
		} + '\n';

		foreach (var point in Points) {
			str += point.GetDescription() + '\n';
		}

		str += type switch {
			Type.MANDATE_CONTRACT =>
				Expires >= Points[0].SideA.GetTime()
					? $"The contract must be fulfilled in {(GameTime.GetFancyTimeString(Expires - Points[0].SideA.GetTime()))}."
					: $"The contract has " + (Fulfilled ? "been completed." : "failed."),
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
				case Type.PROVIDES_RESOURCES_TO: {
						return sideA.Resources.HasEnoughAll(Resources);
					}
			}
			throw new NotImplementedException("This type's fulfillment check is unimplemented!");
		}

		public void Fulfill() {
			switch (type) {
				case Type.PROVIDES_RESOURCES_TO: {
						sideA.Resources.TransferResources(sideB.Resources, Resources);
						return;
					}
			}
			throw new NotImplementedException("This type's fulfillment is unimplemented!");
		}

		public string GetDescription() {
			return type switch {
				Type.PROVIDES_RESOURCES_TO => Resources.Count == 0 ? "" :
					$"* {sideA.DocName} provides the following resources to {sideB.DocName}:"
					+ "\n" + Resources.Aggregate("", (s, r) => s + "\n - " + r.Type.AssetName + " x " + r.Amount),
				Type.PROVIDES_WORKERS_TO => $"* {sideA.DocName} provides {amount} workers to {sideB.DocName}, or less according to the recipient's available living space",
				_ => throw new System.NotImplementedException(),
			};
		}

	}

}

partial class Document {

	public enum Type {

		MANDATE_CONTRACT,

	}

	partial class Point {

		public enum Type {

			PROVIDES_RESOURCES_TO,
			PROVIDES_WORKERS_TO,

		}

	}

}

partial class Document {

	public class Briefcase {

		readonly Dictionary<Document.Type, List<Document>> documents;


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

		public void AddDocument(Document document) {
			if (!documents.ContainsKey(document.type)) documents[document.type] = new();
			List<Document> list = documents[document.type];
			list.Add(document);
		}

		public Document CreateExportMandate(
			List<ResourceBundle> requirements,
			List<ResourceBundle> rewards,
			Faction parentFaction,
			Faction regionFaction,
			TimeT due
		) {
			var type = Type.MANDATE_CONTRACT;
			var doc = new Document(type, due, parentFaction, regionFaction) {

				Points = new Point[] {
					new(regionFaction, Point.Type.PROVIDES_RESOURCES_TO, parentFaction) { Resources = requirements },
					new(parentFaction, Point.Type.PROVIDES_RESOURCES_TO, regionFaction) { Resources = rewards },
				}
			};

			if (!documents.ContainsKey(type)) documents[type] = new();
			List<Document> list = documents[type];
			doc.Title = "Export Mandate #" + list.Count;
			list.Add(doc);

			return doc;
		}


	}

}
