using System;
using Godot;

namespace scenes.region.ui {

	public partial class DocumentsDisplay : PanelContainer {

		public const char DOWN = '⇩';
		public const char RIGHT = '⇨';

		[Export] Button currentAgreementsToggle;
		[Export] Control currentAgreementsControl;
		[Export] VBoxContainer currentAgreementsList;

		[Export] Button pastAgreementsToggle;
		[Export] Control pastAgreementsControl;
		[Export] VBoxContainer pastAgreementsList;

		[Export] UI ui;

		Document.Briefcase briefcase;


		public override void _Ready() {
			currentAgreementsToggle.Toggled += OnCurrentAgreementsToggled;
			pastAgreementsToggle.Toggled += OnPastAgreementsToggled;

			currentAgreementsToggle.ButtonPressed = true;
			pastAgreementsToggle.ButtonPressed = true;

			currentAgreementsToggle.SetDeferred("button_pressed", true);
			pastAgreementsToggle.SetDeferred("button_pressed", false);
		}

		public override void _GuiInput(InputEvent evt) {
			if (evt is InputEventMouseButton) {
				GetViewport().SetInputAsHandled();
			}
		}

		void OnCurrentAgreementsToggled(bool to) {
			currentAgreementsToggle.Text = "Current agreements " + (to ? DOWN : RIGHT);
			currentAgreementsControl.Visible = to;
		}

		void OnPastAgreementsToggled(bool to) {
			pastAgreementsToggle.Text = "Past agreements " + (to ? DOWN : RIGHT);
			pastAgreementsControl.Visible = to;
		}

		public void Display(Document.Briefcase briefcase) {
			this.briefcase = briefcase;
			Display();
		}

		public void Display() {
			Debug.Assert(briefcase != null, "Give me da damn briefcase!!");
			foreach (var dis in currentAgreementsList.GetChildren()) dis.QueueFree();
			foreach (var dis in pastAgreementsList.GetChildren()) dis.QueueFree();

			var active = briefcase.GetActiveDocuments();
			var inactive = briefcase.GetInactiveDocuments();

			foreach (var doc in active) {
				var dis = DocumentDisplay.PACKED.Instantiate<DocumentDisplay>();
				dis.Display(doc);
				currentAgreementsList.AddChild(dis);
			}

			foreach (var doc in inactive) {
				var dis = DocumentDisplay.PACKED.Instantiate<DocumentDisplay>();
				dis.Display(doc);
				pastAgreementsList.AddChild(dis);
			}
		}
	}

}