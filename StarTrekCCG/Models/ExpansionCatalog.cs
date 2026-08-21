using System;
using System.Collections.Generic;
using System.Linq;

namespace StarTrekCCG.Models;

/// <summary>
/// Anzeige-Namen für Set-Ordner (interne Keys unverändert).
/// Virtual = CC-Virtual-Expansion; Physical = Decipher / physisch gedruckt.
/// </summary>
public static class ExpansionCatalog
{
    public sealed record Info(string Key, string DisplayName, bool IsVirtual)
    {
        public string FilterLabel =>
            IsVirtual ? $"[V] {DisplayName}" : $"[P] {DisplayName}";
    }

    private static readonly Dictionary<string, Info> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ---- Physical (Decipher + early) ----
            ["PR"] = new("PR", "Premiere", false),
            ["Alternate_Universe"] = new("Alternate_Universe", "Alternate Universe", false),
            ["Q_Continuum"] = new("Q_Continuum", "Q-Continuum", false),
            ["First_Contact"] = new("First_Contact", "First Contact", false),
            ["Deep_Space_Nine"] = new("Deep_Space_Nine", "Deep Space Nine", false),
            ["The_Dominion"] = new("The_Dominion", "The Dominion", false),
            ["Blaze_of_Glory"] = new("Blaze_of_Glory", "Blaze of Glory", false),
            ["Rules_of_Acquisition"] = new("Rules_of_Acquisition", "Rules of Acquisition", false),
            ["The_Trouble_with_Tribbles"] = new("The_Trouble_with_Tribbles", "The Trouble with Tribbles", false),
            ["Trouble_with_Tribbles"] = new("Trouble_with_Tribbles", "The Trouble with Tribbles", false),
            ["Mirror_Mirror"] = new("Mirror_Mirror", "Mirror, Mirror", false),
            ["Voyager"] = new("Voyager", "Voyager", false),
            ["The_Borg"] = new("The_Borg", "The Borg", false),
            ["Holodeck_Adventures"] = new("Holodeck_Adventures", "Holodeck Adventures", false),
            ["The_Motion_Pictures"] = new("The_Motion_Pictures", "The Motion Pictures", false),
            ["All_Good_Things"] = new("All_Good_Things", "All Good Things", false),
            ["Enhanced_Premiere"] = new("Enhanced_Premiere", "Enhanced Premiere", false),
            ["Enhanced_First_Contact"] = new("Enhanced_First_Contact", "Enhanced First Contact", false),
            ["Starter_Deck_II"] = new("Starter_Deck_II", "Starter Deck II", false),
            ["sdII"] = new("sdII", "Starter Deck II", false),
            ["Official_Tournament_Sealed_Deck"] = new("Official_Tournament_Sealed_Deck", "Official Tournament Sealed Deck", false),
            ["Introductory_Two_Player_Game"] = new("Introductory_Two_Player_Game", "Introductory Two-Player Game", false),
            ["First_Anthology"] = new("First_Anthology", "First Anthology", false),
            ["1anth"] = new("1anth", "First Anthology", false),
            ["Second_Anthology"] = new("Second_Anthology", "Second Anthology", false),
            ["warppack"] = new("warppack", "Warp Pack", false),
            ["WPEmissary"] = new("WPEmissary", "Warp Pack: Emissary", false),
            ["Enterprise_Collection"] = new("Enterprise_Collection", "Enterprise Collection", false),
            ["Captain_Picard_Collection"] = new("Captain_Picard_Collection", "Captain Picard Collection", false),
            ["Captain_Kirk_Collection"] = new("Captain_Kirk_Collection", "Captain Kirk Collection", false),
            ["Promo"] = new("Promo", "Promos", false),

            // ---- Physical late / special ----
            ["IMD"] = new("IMD", "In a Mirror, Darkly", false),
            ["What_You_Leave_Behind"] = new("What_You_Leave_Behind", "What You Leave Behind", false),
            ["Broken_Bow"] = new("Broken_Bow", "Broken Bow", false),
            ["Enterprise"] = new("Enterprise", "Enterprise", false),

            // ---- Virtual (Continuing Committee) ----
            ["Life_From_Lifelessness"] = new("Life_From_Lifelessness", "Life From Lifelessness", true),
            ["The_Next_Generation"] = new("The_Next_Generation", "The Next Generation", true),
            ["The_Maquis"] = new("The_Maquis", "The Maquis", true),
            ["Metamorphosis"] = new("Metamorphosis", "Metamorphosis", true),
            ["Crossover"] = new("Crossover", "Crossover", true),
            ["crossovers"] = new("crossovers", "Crossover", true),
            ["Through_the_Looking_Glass"] = new("Looking_Glass", "Through the Looking Glass", true),
            ["Looking_Glass"] = new("Looking_Glass", "Through the Looking Glass", true),
            ["The_Great_Gathering"] = new("The_Great_Gathering", "The Great Gathering", true),
            ["Raise_the_Stakes"] = new("Raise_the_Stakes", "Raise the Stakes", true),
            ["RTS"] = new("RTS", "Raise the Stakes", true),
            ["Cold_Front"] = new("Cold_Front", "Cold Front", true),
            ["Coming_of_Age"] = new("Coming_of_Age", "Coming of Age", true),
            ["Cage"] = new("Cage", "The Cage", true),
            ["ENGAGE"] = new("ENGAGE", "Engage", true),
            ["Emissary"] = new("Emissary", "Emissary", true),
            ["Emissary+"] = new("Emissary+", "Emissary (Remaster)", true),
            ["Live_Long_and_Prosper"] = new("Live_Long_and_Prosper", "Live Long and Prosper", true),
            ["Terran_Empire"] = new("Terran_Empire", "Terran Empire", true),
            ["The_Best_of_Both_Worlds"] = new("The_Best_of_Both_Worlds", "The Best of Both Worlds", true),
            ["Pre_Warp"] = new("Pre_Warp", "Pre-Warp Pack", true),
            ["Genesis"] = new("Genesis", "Genesis", true),
            ["gift"] = new("gift", "The Gift", true),
            ["tnz"] = new("tnz", "The Neutral Zone", true),
            ["Nemesis"] = new("Nemesis", "Nemesis", true),
            ["NEM"] = new("NEM", "Nemesis", true),
            ["otsdr"] = new("otsdr", "OTSD Remastered", true),
            ["HF"] = new("HF", "Homefront", true),
            ["HF2"] = new("HF2", "Homefront II", true),
            ["HF3"] = new("HF3", "Homefront III", true),
            ["HF4"] = new("HF4", "Homefront IV", true),
            ["HF5"] = new("HF5", "Homefront V", true),
            ["HF6"] = new("HF6", "Homefront VI", true),
            ["20th"] = new("20th", "20th Anniversary Collection", true),
            ["50"] = new("50", "50th Anniversary Collection", true),
            ["Bah!"] = new("Bah!", "Bah!", true),
            ["SotL"] = new("SotL", "Straight and Steady", true),
            ["SoG"] = new("SoG", "Show of Force", true),
            ["SSttR"] = new("SSttR", "Shore Leave", true),
            ["TMPR"] = new("TMPR", "The Motion Pictures Remastered", true),
            ["TUC"] = new("TUC", "The Undiscovered Country", true),
            ["TSOP"] = new("TSOP", "The Sky's the Limit", true),
            ["TWT"] = new("TWT", "To Boldly Go", true),
            ["FTB"] = new("FTB", "From the Bajorans", true),
            ["AP"] = new("AP", "Archive Promos", true),
            ["AUT"] = new("AUT", "Archive / Tournament", true),
            ["BP"] = new("BP", "Block Promos", true),
            ["DM"] = new("DM", "Designer / Demo", true),
            ["DP"] = new("DP", "Decipher Promos", true),
            ["IC"] = new("IC", "Identity Crisis", true),
            ["PL"] = new("PL", "Premium / Limited", true),
            ["R2"] = new("R2", "Reflections 2.0", true),
            ["Ref"] = new("Ref", "Reflections", true),
            ["referee"] = new("referee", "Referee", true),
            ["SFL"] = new("SFL", "Starfleet", true),
            ["SFLS"] = new("SFLS", "Starfleet Second Wave", true),
            ["SW"] = new("SW", "Space Whales", true),
            ["SaS"] = new("SaS", "Spies and SpecOps", true),
            ["VP"] = new("VP", "Virtual Promos", true),
            ["X"] = new("X", "Extras", true),
            ["armade"] = new("armade", "Armada", true),
            ["awayteam"] = new("awayteam", "Away Team", true),
            ["coc"] = new("coc", "Chain of Command", true),
            ["dow"] = new("dow", "Day of Reckoning", true),
            ["ecr"] = new("ecr", "Enterprise Collection Remaster", true),
            ["equilibriu"] = new("equilibriu", "Equilibrium", true),
            ["plw"] = new("plw", "Peak Performance", true),
            ["qwho"] = new("qwho", "Q Who", true),
            ["tstl"] = new("tstl", "To Seek Out New Life", true),
            ["ttne"] = new("ttne", "These Are The Voyages", true),
            ["wp2017"] = new("wp2017", "Warp Pack 2017", true),
            ["Ls"] = new("Ls", "Leadership", true),
        };

    public static Info Get(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return new Info("", "(unbekannt)", false);
        if (Map.TryGetValue(key, out var info))
            return info;
        // Fallback: Ordnername lesbarer machen
        string nice = key.Replace('_', ' ').Trim();
        return new Info(key, nice, false);
    }

    public static string FilterLabel(string? key) => Get(key).FilterLabel;

    public static string DisplayName(string? key) => Get(key).DisplayName;

    /// <summary>Heuristik aus Rarity-String (z. B. „1 V“).</summary>
    public static bool GuessVirtualFromRarity(string? rarityInfo)
    {
        if (string.IsNullOrWhiteSpace(rarityInfo)) return false;
        // typisch „12 V“ oder nur „V“
        var parts = rarityInfo.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(p => p.Equals("V", StringComparison.OrdinalIgnoreCase)
                           || p.Equals("V+", StringComparison.OrdinalIgnoreCase));
    }
}