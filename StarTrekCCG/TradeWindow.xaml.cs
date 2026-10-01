using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StarTrekCCG;

public partial class TradeWindow : Window
{
    private readonly Func<string, string, string, Task<JsonElement>> _offer;
    private readonly Func<Task<JsonElement>> _list;
    private readonly Func<long, Task<JsonElement>> _accept;
    private readonly Func<long, Task<JsonElement>> _decline;
    private readonly Func<Task<JsonElement>> _pool;

    public TradeWindow(
        Func<string, string, string, Task<JsonElement>> offer,
        Func<Task<JsonElement>> list,
        Func<long, Task<JsonElement>> accept,
        Func<long, Task<JsonElement>> decline,
        Func<Task<JsonElement>> pool)
    {
        InitializeComponent();
        _offer = offer;
        _list = list;
        _accept = accept;
        _decline = decline;
        _pool = pool;
        Loaded += async (_, _) => await RefreshAllAsync().ConfigureAwait(true);
    }

    private async void OfferButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryLines(GiveBox.Text, out var give, out var error) || !TryLines(AskBox.Text, out var ask, out error))
        {
            NoteText.Text = error;
            return;
        }
        if (give == "[]" && ask == "[]")
        {
            NoteText.Text = "name a card";
            return;
        }
        var to = (ToBox.Text ?? "").Trim();
        if (to.Length == 0)
        {
            NoteText.Text = "name required";
            return;
        }
        await RunAsync(async () =>
        {
            var body = await _offer(to, give, ask).ConfigureAwait(true);
            NoteText.Text = MessageOf(body, "tradeOffered", "Offer sent.");
        }).ConfigureAwait(true);
        await RefreshAllAsync().ConfigureAwait(true);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await RefreshAllAsync().ConfigureAwait(true);

    private async void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySelected(out var id))
            return;
        await RunAsync(async () =>
        {
            var body = await _accept(id).ConfigureAwait(true);
            NoteText.Text = MessageOf(body, "tradeDone", "Trade moved both sides.");
        }).ConfigureAwait(true);
        await RefreshAllAsync().ConfigureAwait(true);
    }

    private async void DeclineButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TrySelected(out var id))
            return;
        await RunAsync(async () =>
        {
            var body = await _decline(id).ConfigureAwait(true);
            NoteText.Text = MessageOf(body, "tradeDeclined", "Offer declined.");
        }).ConfigureAwait(true);
        await RefreshAllAsync().ConfigureAwait(true);
    }

    private async Task RefreshAllAsync()
    {
        await RunAsync(async () =>
        {
            var list = await _list().ConfigureAwait(true);
            OfferList.Items.Clear();
            if (list.TryGetProperty("offers", out var offers) && offers.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in offers.EnumerateArray())
                {
                    var id = row.TryGetProperty("offerId", out var idEl) && idEl.TryGetInt64(out var n)
                        ? n.ToString(CultureInfo.InvariantCulture)
                        : "?";
                    var from = row.TryGetProperty("fromName", out var fromEl) ? fromEl.GetString() : "";
                    var to = row.TryGetProperty("toName", out var toEl) ? toEl.GetString() : "";
                    var incoming = row.TryGetProperty("incoming", out var inEl) && inEl.ValueKind == JsonValueKind.True;
                    var give = row.TryGetProperty("offerCards", out var giveEl) ? giveEl.GetString() : "";
                    var ask = row.TryGetProperty("requestCards", out var askEl) ? askEl.GetString() : "";
                    var side = incoming ? "incoming" : "outgoing";
                    OfferList.Items.Add(id + "  " + side + "  " + from + " -> " + to
                        + "\n  give " + give + "\n  ask " + ask);
                }
            }
            var pool = await _pool().ConfigureAwait(true);
            var text = new StringBuilder();
            if (pool.TryGetProperty("cards", out var cards) && cards.ValueKind == JsonValueKind.Array)
            {
                foreach (var card in cards.EnumerateArray())
                {
                    var id = card.TryGetProperty("cardId", out var idEl) ? idEl.GetString() : "";
                    var qty = card.TryGetProperty("quantity", out var qtyEl) && qtyEl.TryGetInt32(out var q)
                        ? q.ToString(CultureInfo.InvariantCulture)
                        : "?";
                    text.Append(qty);
                    text.Append("  ");
                    text.AppendLine(id);
                }
            }
            PoolBox.Text = text.Length == 0 ? "(no cards)" : text.ToString().TrimEnd();
        }).ConfigureAwait(true);
    }

    private bool TrySelected(out long id)
    {
        id = 0;
        var text = OfferList.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(text))
        {
            NoteText.Text = "Pick an offer.";
            return false;
        }
        var head = text.Split(' ', 2)[0];
        if (!long.TryParse(head, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
        {
            NoteText.Text = "Pick an offer.";
            return false;
        }
        return true;
    }

    private async Task RunAsync(Func<Task> work)
    {
        OfferButton.IsEnabled = false;
        AcceptButton.IsEnabled = false;
        DeclineButton.IsEnabled = false;
        try
        {
            await work().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            NoteText.Text = ex.Message;
        }
        finally
        {
            OfferButton.IsEnabled = true;
            AcceptButton.IsEnabled = true;
            DeclineButton.IsEnabled = true;
        }
    }

    private static string MessageOf(JsonElement body, string okType, string okText)
    {
        var type = body.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
        if (string.Equals(type, okType, StringComparison.Ordinal))
            return okText;
        var message = body.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
        return string.IsNullOrWhiteSpace(message) ? "trade failed" : message!;
    }

    private static bool TryLines(string? raw, out string json, out string error)
    {
        json = "[]";
        error = "";
        var rows = new List<Dictionary<string, object>>();
        var lines = (raw ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var text = line.Trim();
            if (text.Length == 0)
                continue;
            var id = text;
            var qty = 1;
            var cut = text.LastIndexOf(' ');
            var comma = text.LastIndexOf(',');
            if (comma > cut)
                cut = comma;
            if (cut > 0)
            {
                var tail = text[(cut + 1)..].Trim();
                if (tail.StartsWith("x", StringComparison.OrdinalIgnoreCase))
                    tail = tail[1..].Trim();
                if (int.TryParse(tail, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                {
                    id = text[..cut].Trim().TrimEnd(',').Trim();
                    qty = n;
                }
            }
            if (id.Length == 0 || qty < 1)
            {
                error = "quantity must be 1 to 99";
                return false;
            }
            rows.Add(new Dictionary<string, object> { ["cardId"] = id, ["quantity"] = qty });
        }
        json = JsonSerializer.Serialize(rows);
        return true;
    }
}
