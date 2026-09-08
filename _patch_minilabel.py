import re
from pathlib import Path

path = Path(r"StarTrekCCG\TableWindow.xaml.cs")
text = path.read_text(encoding="utf-8")

pattern = re.compile(
    r"    private Border CreateMiniCard\(Card card, bool faceDown = false\)\r?\n"
    r"    \{.*?"
    r"        border\.Child = img;\r?\n"
    r"        if \(!faceDown\)\r?\n"
    r"            AttachMiniHover\(border, card\);\r?\n"
    r"        return border;\r?\n"
    r"    \}",
    re.S,
)

m = pattern.search(text)
if not m:
    print("PATTERN NOT FOUND")
    idx = text.find("private Border CreateMiniCard")
    print("idx", idx)
    if idx >= 0:
        print(repr(text[idx:idx+200]))
    raise SystemExit(1)

# Preserve the ToolTip line from the original match (encoding of middle-dot may vary)
old = m.group(0)
# Extract tooltip assignment line
tip_m = re.search(r"ToolTip = faceDown \? \"Facedown\" : card\.Name \+ \"[^\"]*\"", old)
tip_line = tip_m.group(0) if tip_m else 'ToolTip = faceDown ? "Facedown" : card.Name + "\\nClick = detail  ·  Drag = to table / other host"'

new = f'''    private Border CreateMiniCard(Card card, bool faceDown = false)
    {{
        var border = new Border
        {{
            Width = 68,
            Height = 94,
            Margin = new Thickness(2),
            BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(25, 25, 25)),
            CornerRadius = new CornerRadius(2),
            Cursor = Cursors.Hand,
            Tag = card,
            {tip_line}
        }};

        var img = new Image {{ Stretch = Stretch.Uniform }};
        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.LowQuality);
        bool hasArt = false;

        if (faceDown && _cardBackImage != null)
        {{
            img.Source = _cardBackImage;
            hasArt = true;
        }}
        else if (!string.IsNullOrEmpty(card.FullImagePath) && System.IO.File.Exists(card.FullImagePath))
        {{
            try
            {{
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(card.FullImagePath, UriKind.Absolute);
                bmp.DecodePixelWidth = 80;
                bmp.EndInit();
                img.Source = bmp;
                hasArt = true;
            }}
            catch {{ }}
        }}

        // Synthetic AskChoice / missing-art pick entries: show Name so the strip is not black empty slots.
        if (hasArt)
            border.Child = img;
        else
            border.Child = CreateMiniNameLabel(card);

        if (!faceDown && hasArt)
            AttachMiniHover(border, card);
        return border;
    }}

    /// <summary>Label fallback for strip minis without FullImagePath (e.g. AskChoice Type=Choice).</summary>
    private static TextBlock CreateMiniNameLabel(Card card)
    {{
        return new TextBlock
        {{
            Text = string.IsNullOrWhiteSpace(card.Name) ? "?" : card.Name,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(4),
            Padding = new Thickness(2)
        }};
    }}'''

# Normalize newlines to match file
nl = "\r\n" if "\r\n" in text[:5000] else "\n"
new = new.replace("\n", nl)

updated = text[:m.start()] + new + text[m.end()]
path.write_text(updated, encoding="utf-8")
print("PATCHED OK")
print("CreateMiniNameLabel count:", updated.count("CreateMiniNameLabel"))
