using Godot;


namespace scenes.region.ui {

	public partial class DocumentDisplay : PanelContainer {

		public static PackedScene PACKED = GD.Load<PackedScene>("uid://c7bkc1812dw4j");

		[Export] Button titleButton;
		[Export] RichTextLabel bodyText;

		Document document;


		public override void _Ready() {
			titleButton.Toggled += OnToggled;
			titleButton.ButtonPressed = true;
			titleButton.ButtonPressed = false;
		}

		void OnToggled(bool to) {
			bodyText.Visible = to;
			titleButton.Text = (document?.Title + ' ' + (to ? DocumentsDisplay.DOWN : DocumentsDisplay.RIGHT)) ?? "What";
			if (to && document != null) bodyText.Text = document.GetText();
		}

		public void Display(Document document) {
			this.document = document;
			bodyText.Text = document.GetText();
		}

	}

}