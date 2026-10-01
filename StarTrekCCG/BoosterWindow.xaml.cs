using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StarTrekCCG;

public partial class BoosterWindow : Window
{
    private readonly Func<Task<PackBuyResult>> _buy;

    public BoosterWindow(int? latinum, Func<Task<PackBuyResult>> buy)
    {
        InitializeComponent();
        _buy = buy;
        ShowLatinum(latinum, known: latinum.HasValue);
        PriceText.Text = "Price: 50 latinum. 1 Rare, 3 Uncommon, 11 Common.";
    }

    private async void BuyButton_Click(object sender, RoutedEventArgs e)
    {
        BuyButton.IsEnabled = false;
        NoteText.Text = "Buying...";
        try
        {
            var result = await _buy().ConfigureAwait(true);
            if (result.LatinumKnown)
                ShowLatinum(result.Latinum, known: true);
            if (!result.Ok)
            {
                NoteText.Text = result.Message;
                return;
            }

            var lines = new StringBuilder();
            for (var i = 0; i < result.CardIds.Count; i++)
            {
                lines.Append((i + 1).ToString(CultureInfo.InvariantCulture));
                lines.Append(". ");
                lines.AppendLine(result.CardIds[i]);
            }
            CardsBox.Text = lines.ToString().TrimEnd();
            NoteText.Text = result.CardIds.Count.ToString(CultureInfo.InvariantCulture) + " cards. Latinum is the number above.";
        }
        catch (Exception ex)
        {
            NoteText.Text = ex.Message;
        }
        finally
        {
            BuyButton.IsEnabled = true;
        }
    }

    private void ShowLatinum(int? latinum, bool known)
    {
        LatinumText.Text = known && latinum is int n
            ? "Latinum: " + n.ToString(CultureInfo.InvariantCulture)
            : "Latinum: (connect to read it)";
    }
}

public sealed class PackBuyResult
{
    private PackBuyResult(bool ok, string message, int latinum, bool latinumKnown, IReadOnlyList<string> cardIds)
    {
        Ok = ok;
        Message = message;
        Latinum = latinum;
        LatinumKnown = latinumKnown;
        CardIds = cardIds;
    }

    public bool Ok { get; }
    public string Message { get; }
    public int Latinum { get; }
    public bool LatinumKnown { get; }
    public IReadOnlyList<string> CardIds { get; }

    public static PackBuyResult Fail(string message)
        => new(false, message, 0, false, Array.Empty<string>());

    public static PackBuyResult Bought(int latinum, IReadOnlyList<string> cardIds)
        => new(true, "", latinum, true, cardIds);
}
