using System;
using Godot;

public partial class WarEssay : ConfirmationDialog {

    public event Action<string> WarDeclared;

    [Export] TextEdit essayBox;
    [Export] Label wordCountLabel;


	public override void _Ready() {
        GetOkButton().Disabled = true;
        var empty = new StyleBoxEmpty();
        GetOkButton().AddThemeStyleboxOverride("focus", empty);
        GetCancelButton().AddThemeStyleboxOverride("focus", empty);
        essayBox.TextChanged += OnEssayChanged;
        OnEssayChanged();
        Confirmed += OnConfirmed;
	}

    void OnEssayChanged() {
        var txt = essayBox.Text.AsSpan();
        var spl = txt.Split(' ');
        int words = 0;
        foreach (var s in spl) if (s.End.Value - s.Start.Value >= 3) words++;
        wordCountLabel.Text = $"{words}/10 words {(words > 10 ? "Very good!" : "")}";
        GetOkButton().Disabled = words < 10;
    }

    void OnConfirmed() {
        WarDeclared?.Invoke(essayBox.Text);
    }

}
