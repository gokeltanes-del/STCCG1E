using System.Collections.Generic;
using System.Text.Json;
using StarTrekCCG.Models;

namespace StarTrekCCG.Network;

/// <summary>
/// Deck lists stored on the lobby are cardId strings only (SetFolder/ReleaseRaw/Name).
/// </summary>
public static class DeckCardList
{
    public static readonly string[] Sections =
    {
        "battle_bridge", "draw", "q_continuum", "qs_tent", "seed", "side", "site_pile", "tribble"
    };

    public static string FromDeck(Deck deck)
    {
        var lists = new Dictionary<string, List<string>>();
        foreach (var section in Sections)
            lists[section] = new List<string>();
        Add(lists["seed"], deck.SeedCards);
        Add(lists["draw"], deck.DrawCards);
        Add(lists["qs_tent"], deck.QsTentCards);
        Add(lists["battle_bridge"], deck.BattleBridgeCards);
        Add(lists["q_continuum"], deck.QContinuumCards);
        Add(lists["site_pile"], deck.SitePileCards);
        Add(lists["tribble"], deck.TribbleCards);
        Add(lists["side"], deck.SideCards);
        return JsonSerializer.Serialize(lists);
    }

    public static string FromDeckJson(string deckJson)
    {
        var deck = new Services.DeckService().LoadFromJson(deckJson);
        return FromDeck(deck);
    }

    public static Deck ToDeck(string name, JsonElement cardIds)
    {
        var deck = new Deck
        {
            Format = Services.DeckService.FormatV3,
            Name = string.IsNullOrWhiteSpace(name) ? "Deck" : name
        };
        if (cardIds.ValueKind != JsonValueKind.Object)
            return deck;
        void Take(string section, List<DeckEntry> target)
        {
            if (!cardIds.TryGetProperty(section, out var arr) || arr.ValueKind != JsonValueKind.Array)
                return;
            var grouped = new List<DeckEntry>();
            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;
                var id = (item.GetString() ?? "").Trim();
                if (!TrySplit(id, out var set, out var release, out var cardName))
                    continue;
                var existing = grouped.Find(e => string.Equals(e.CardId, id, System.StringComparison.Ordinal));
                if (existing != null)
                {
                    existing.Quantity++;
                    continue;
                }
                grouped.Add(new DeckEntry
                {
                    Name = cardName,
                    Set = set,
                    CardId = id,
                    Quantity = 1
                });
                _ = release;
            }
            target.AddRange(grouped);
        }
        Take("seed", deck.SeedCards);
        Take("draw", deck.DrawCards);
        Take("qs_tent", deck.QsTentCards);
        Take("battle_bridge", deck.BattleBridgeCards);
        Take("q_continuum", deck.QContinuumCards);
        Take("site_pile", deck.SitePileCards);
        Take("tribble", deck.TribbleCards);
        Take("side", deck.SideCards);
        return deck;
    }

    public static string SerializeDeck(Deck deck)
        => JsonSerializer.Serialize(deck);

    public static bool TrySplit(string id, out string set, out string release, out string name)
    {
        set = "";
        release = "";
        name = "";
        var first = id.IndexOf('/');
        if (first <= 0)
            return false;
        var second = id.IndexOf('/', first + 1);
        if (second < 0 || second >= id.Length - 1)
            return false;
        set = id[..first];
        release = id[(first + 1)..second];
        name = id[(second + 1)..];
        return set.Length > 0 && name.Length > 0;
    }

    private static void Add(List<string> target, List<DeckEntry>? entries)
    {
        if (entries == null)
            return;
        foreach (var entry in entries)
        {
            if (entry == null)
                continue;
            var id = string.IsNullOrWhiteSpace(entry.CardId)
                ? Card.FormatCardId(entry.Set, null, entry.Name)
                : entry.CardId.Trim();
            if (string.IsNullOrWhiteSpace(id) || id == "//")
                continue;
            var copies = entry.Quantity < 1 ? 1 : entry.Quantity;
            for (var i = 0; i < copies; i++)
                target.Add(id);
        }
    }
}
