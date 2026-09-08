from pathlib import Path

path = Path("StarTrekCCG/TableWindow.xaml.cs")
text = path.read_text(encoding="utf-8")
nl = "\r\n" if "\r\n" in text[:8000] else "\n"
lines = text.splitlines()
print("lines before", len(lines))

# Find CreateMiniCard method start/end by brace counting
start = None
for i, line in enumerate(lines):
    if line.startswith("    private Border CreateMiniCard(Card card, bool faceDown = false)"):
        start = i
        break
if start is None:
    raise SystemExit("start not found")

# Method body starts at start+1 '{'; count braces
depth = 0
end = None
for i in range(start, len(lines)):
    # crude brace count ignoring strings is OK for this method
    depth += lines[i].count("{") - lines[i].count("}")
    if i > start and depth == 0:
        end = i  # inclusive closing brace line
        break
if end is None:
    raise SystemExit("end not found")

print(f"replacing lines {start+1}-{end+1}")
print("first:", lines[start])
print("last:", lines[end])

# Preserve original ToolTip line
tip = None
for j in range(start, end+1):
    if "ToolTip = faceDown" in lines[j]:
        tip = lines[j].strip()
        break
if not tip:
    tip = 'ToolTip = faceDown ? "Facedown" : card.Name + "\\nClick = detail  ·  Drag = to table / other host"'

new_block = f'''    private Border CreateMiniCard(Card card, bool faceDown = false)
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
            {tip}
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
    }}'''.splitlines()

out = lines[:start] + new_block + lines[end+1:]
print("lines after", len(out))
assert len(out) > 20000, "refusing to write truncated file"
path.write_text(nl.join(out) + nl, encoding="utf-8")
print("OK")
