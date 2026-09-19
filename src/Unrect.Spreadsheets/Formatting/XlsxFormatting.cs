using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A workbook's formatting, read from the package itself: the value reader hands back values and
  /// a style's index into a font table it does not expose, so what a cell looks like comes from
  /// here. One read of the styles part, then one pass over a sheet for the style each cell names.
  /// <para>
  /// A cell's style is an index into <c>cellXfs</c>; that entry names a font and a fill by index;
  /// the font carries the colour and the marks. A cell the sheet does not write has the default
  /// style, which is entry 0.
  /// </para>
  /// </summary>
  internal sealed class XlsxFormatting : IDisposable
  {
    private readonly ZipArchive _archive;
    private readonly List<KeyValuePair<string, string>> _sheets;
    private readonly List<CellFont> _fonts = new List<CellFont>();
    private readonly List<CellFill> _fills = new List<CellFill>();
    private readonly List<(int Font, int Fill)> _styles = new List<(int, int)>();
    private readonly List<int> _palette = new List<int>();

    private XlsxFormatting(ZipArchive archive)
    {
      _archive = archive;
      _sheets = XlsxFormulas.ReadSheetMap(archive);
      ReadStyles();
    }

    /// <summary>The workbook at <paramref name="path"/>, opened for its formatting.</summary>
    /// <exception cref="NotSupportedException">The file is not an xlsx.</exception>
    internal static XlsxFormatting Open(string path)
    {
      var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      ZipArchive archive;

      try
      {
        archive = new ZipArchive(stream, ZipArchiveMode.Read);
      }
      catch (InvalidDataException)
      {
        stream.Dispose();

        throw new NotSupportedException(
          $"Formatting can only be read from .xlsx files, and '{path}' is not one; read it without formatting, or convert it.");
      }

      try
      {
        return new XlsxFormatting(archive);
      }
      catch
      {
        archive.Dispose();
        throw;
      }
    }

    public void Dispose() => _archive.Dispose();

    /// <summary>
    /// The style each cell of one sheet names, as a grid of the value grid's shape; 0, the default
    /// style, wherever the sheet writes no cell.
    /// </summary>
    internal int[,] ReadSheet(string sheetName, int sheetIndex, int width, int height)
    {
      var styles = new int[height, width];
      var part = _archive.GetEntry(PartOf(sheetName, sheetIndex));

      if (part is null)
        return styles;

      using var content = part.Open();
      using var reader = XmlReader.Create(content, XlsxFormulas.Settings());

      var row = -1;
      var column = -1;

      while (reader.Read())
      {
        if (reader.NodeType != XmlNodeType.Element)
          continue;

        if (reader.LocalName == "row")
        {
          row = XlsxFormulas.Index(reader.GetAttribute("r"), row);
          column = -1;
        }
        else if (reader.LocalName == "c")
        {
          if (A1Reference.TryParse(reader.GetAttribute("r") ?? string.Empty, out var at, out var atRow))
          {
            column = at;
            row = atRow;
          }
          else
          {
            column++;
          }

          if (row >= 0 && row < height && column >= 0 && column < width
            && int.TryParse(reader.GetAttribute("s"), NumberStyles.None, CultureInfo.InvariantCulture, out var style))
            styles[row, column] = style;
        }
      }

      return styles;
    }

    /// <summary>Every style's font, by the style's index — what a space keeps once this reader is closed.</summary>
    internal CellFont[] Fonts()
    {
      var fonts = new CellFont[_styles.Count];

      for (var style = 0; style < fonts.Length; style++)
        fonts[style] = FontOf(style);

      return fonts;
    }

    /// <summary>Every style's fill, by the style's index.</summary>
    internal CellFill[] Fills()
    {
      var fills = new CellFill[_styles.Count];

      for (var style = 0; style < fills.Length; style++)
        fills[style] = FillOf(style);

      return fills;
    }

    /// <summary>The font of style <paramref name="style"/>; the default font for one the workbook does not define.</summary>
    internal CellFont FontOf(int style)
      => style >= 0 && style < _styles.Count && _styles[style].Font is var font && font >= 0 && font < _fonts.Count
        ? _fonts[font]
        : default;

    /// <summary>The fill of style <paramref name="style"/>; no fill for one the workbook does not define.</summary>
    internal CellFill FillOf(int style)
      => style >= 0 && style < _styles.Count && _styles[style].Fill is var fill && fill >= 0 && fill < _fills.Count
        ? _fills[fill]
        : default;

    private string PartOf(string sheetName, int sheetIndex)
    {
      foreach (var sheet in _sheets)
        if (string.Equals(sheet.Key, sheetName, StringComparison.Ordinal))
          return sheet.Value;

      return sheetIndex >= 0 && sheetIndex < _sheets.Count ? _sheets[sheetIndex].Value : string.Empty;
    }

    // --- The styles part ------------------------------------------------------------------------------

    private void ReadStyles()
    {
      var workbook = XlsxFormulas.WorkbookPart(_archive);

      if (workbook is null)
        return;

      var folder = XlsxFormulas.FolderOf(workbook);
      string? stylesPart = null;

      foreach (var relationship in XlsxFormulas.ReadRelationships(_archive, $"{folder}_rels/{XlsxFormulas.NameOf(workbook)}.rels"))
        if (relationship.Type.EndsWith("/styles", StringComparison.Ordinal))
          stylesPart = XlsxFormulas.Resolve(folder, relationship.Target);

      // A workbook with no styles part formats nothing: every cell has the default style.
      var entry = stylesPart is null ? null : _archive.GetEntry(stylesPart);

      if (entry is null)
        return;

      // The fonts and fills come before the palette that an indexed colour in them resolves
      // through, so colours are kept as they were written and resolved once everything is read.
      var fonts = new List<(Written Color, bool Bold, bool Italic, bool Strike)>();
      var fills = new List<Written>();

      using (var content = entry.Open())
      using (var reader = XmlReader.Create(content, XlsxFormulas.Settings()))
      {
        while (reader.Read())
        {
          if (reader.NodeType != XmlNodeType.Element)
            continue;

          switch (reader.LocalName)
          {
            case "fonts":
              ReadEach(reader, "font", font => fonts.Add(ReadFont(font)));
              break;

            case "fills":
              ReadEach(reader, "fill", fill => fills.Add(ReadFill(fill)));
              break;

            case "cellXfs":
              ReadEach(reader, "xf", xf => _styles.Add((Number(xf.GetAttribute("fontId")), Number(xf.GetAttribute("fillId")))));
              break;

            case "indexedColors":
              ReadEach(reader, "rgbColor", color => _palette.Add(Hex(color.GetAttribute("rgb")) ?? 0));
              break;
          }
        }
      }

      foreach (var font in fonts)
        _fonts.Add(new CellFont(Resolve(font.Color), font.Bold, font.Italic, font.Strike));

      foreach (var fill in fills)
        _fills.Add(new CellFill(Resolve(fill)));
    }

    /// <summary>Calls <paramref name="read"/> on a subtree reader for every <paramref name="element"/> directly inside the current one.</summary>
    private static void ReadEach(XmlReader reader, string element, Action<XmlReader> read)
    {
      if (reader.IsEmptyElement)
        return;

      var depth = reader.Depth;

      while (reader.Read() && reader.Depth > depth)
      {
        if (reader.NodeType != XmlNodeType.Element || reader.Depth != depth + 1 || reader.LocalName != element)
          continue;

        using var subtree = reader.ReadSubtree();
        subtree.Read();
        read(subtree);

        // Drain what the callback left, so the outer reader resumes after this element.
        while (subtree.Read())
        {
        }
      }
    }

    private static (Written, bool, bool, bool) ReadFont(XmlReader font)
    {
      Written color = default;
      bool bold = false, italic = false, strike = false;

      if (font.IsEmptyElement)
        return (color, bold, italic, strike);

      while (font.Read())
      {
        if (font.NodeType != XmlNodeType.Element)
          continue;

        switch (font.LocalName)
        {
          case "color": color = Written.From(font); break;
          case "b": bold = Flag(font); break;
          case "i": italic = Flag(font); break;
          case "strike": strike = Flag(font); break;
        }
      }

      return (color, bold, italic, strike);
    }

    /// <summary>
    /// A fill's colour is its pattern's foreground — what a solid fill paints with. A pattern of
    /// <c>none</c> is no fill whatever colours it happens to carry.
    /// </summary>
    private static Written ReadFill(XmlReader fill)
    {
      Written color = default;
      var none = true;

      if (fill.IsEmptyElement)
        return color;

      while (fill.Read())
      {
        if (fill.NodeType != XmlNodeType.Element)
          continue;

        if (fill.LocalName == "patternFill")
          none = (fill.GetAttribute("patternType") ?? "none") == "none";
        else if (fill.LocalName == "fgColor")
          color = Written.From(fill);
      }

      return none ? default : color;
    }

    /// <summary>A boolean element: present means true unless it says <c>val="0"</c>.</summary>
    private static bool Flag(XmlReader element)
    {
      var value = element.GetAttribute("val");

      return value is null || (value != "0" && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase));
    }

    private static int Number(string? text)
      => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : 0;

    private static int? Hex(string? text)
      => text is not null && int.TryParse(text.Length > 6 ? text.Substring(text.Length - 6) : text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)
        ? rgb
        : (int?)null;

    private CellColor Resolve(Written written)
    {
      if (written.Rgb is int rgb)
        return CellColor.FromRgb(rgb);

      if (written.Theme is int theme)
        return CellColor.FromTheme(theme, written.Tint);

      if (written.Indexed is int indexed)
      {
        // 64 and 65 are the system foreground and background: automatic, not a colour.
        var palette = _palette.Count > 0 ? (IReadOnlyList<int>)_palette : LegacyPalette;

        return indexed >= 0 && indexed < palette.Count ? CellColor.FromRgb(palette[indexed]) : CellColor.Automatic;
      }

      return CellColor.Automatic;
    }

    /// <summary>A colour element as the file wrote it, before the palette it may index is known.</summary>
    private readonly struct Written
    {
      private Written(int? rgb, int? indexed, int? theme, double tint)
      {
        Rgb = rgb;
        Indexed = indexed;
        Theme = theme;
        Tint = tint;
      }

      internal int? Rgb { get; }

      internal int? Indexed { get; }

      internal int? Theme { get; }

      internal double Tint { get; }

      internal static Written From(XmlReader color)
      {
        var tint = double.TryParse(color.GetAttribute("tint"), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0d;

        return new Written(
          Hex(color.GetAttribute("rgb")),
          int.TryParse(color.GetAttribute("indexed"), NumberStyles.None, CultureInfo.InvariantCulture, out var indexed) ? indexed : (int?)null,
          int.TryParse(color.GetAttribute("theme"), NumberStyles.None, CultureInfo.InvariantCulture, out var theme) ? theme : (int?)null,
          tint);
      }
    }

    /// <summary>The palette an indexed colour names where the workbook does not define its own.</summary>
    private static readonly int[] LegacyPalette =
    {
      0x000000, 0xFFFFFF, 0xFF0000, 0x00FF00, 0x0000FF, 0xFFFF00, 0xFF00FF, 0x00FFFF,
      0x000000, 0xFFFFFF, 0xFF0000, 0x00FF00, 0x0000FF, 0xFFFF00, 0xFF00FF, 0x00FFFF,
      0x800000, 0x008000, 0x000080, 0x808000, 0x800080, 0x008080, 0xC0C0C0, 0x808080,
      0x9999FF, 0x993366, 0xFFFFCC, 0xCCFFFF, 0x660066, 0xFF8080, 0x0066CC, 0xCCCCFF,
      0x000080, 0xFF00FF, 0xFFFF00, 0x00FFFF, 0x800080, 0x800000, 0x008080, 0x0000FF,
      0x00CCFF, 0xCCFFFF, 0xCCFFCC, 0xFFFF99, 0x99CCFF, 0xFF99CC, 0xCC99FF, 0xFFCC99,
      0x3366FF, 0x33CCCC, 0x99CC00, 0xFFCC00, 0xFF9900, 0xFF6600, 0x666699, 0x969696,
      0x003366, 0x339966, 0x003300, 0x333300, 0x993300, 0x993366, 0x333399, 0x333333,
    };
  }
}
