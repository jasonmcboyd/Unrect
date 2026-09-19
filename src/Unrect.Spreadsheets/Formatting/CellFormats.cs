using System;
using System.Globalization;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// A colour as a workbook states one. A workbook says a colour in one of three ways, and they are
  /// not interchangeable: an explicit RGB value, an index into the legacy palette — which resolves
  /// to one — or a slot of the workbook's theme with a tint, which is only a colour once that theme
  /// has been read. A theme colour is therefore reported as what it is rather than guessed at.
  /// <para>
  /// Two colours are equal when they are the same RGB value, however the file came to say it, or
  /// the same theme slot and tint. The alpha a file writes is ignored: <c>FFFF0000</c> and
  /// <c>00FF0000</c> are both red.
  /// </para>
  /// </summary>
  public readonly struct CellColor : IEquatable<CellColor>
  {
    private readonly int _rgb;
    private readonly int _theme;
    private readonly double _tint;
    private readonly byte _kind;

    private CellColor(byte kind, int rgb, int theme, double tint)
    {
      _kind = kind;
      _rgb = rgb;
      _theme = theme;
      _tint = tint;
    }

    /// <summary>No colour stated: the application's automatic colour — black text, no fill.</summary>
    public static CellColor Automatic => default;

    /// <summary>The red of the standard palette, <c>FF0000</c>.</summary>
    public static CellColor Red => FromRgb(0xFF0000);

    /// <summary>The colour with the 24-bit value <paramref name="rgb"/>, <c>0xRRGGBB</c>.</summary>
    public static CellColor FromRgb(int rgb) => new CellColor(1, rgb & 0xFFFFFF, 0, 0);

    /// <summary>Slot <paramref name="index"/> of the workbook's theme, lightened or darkened by <paramref name="tint"/>.</summary>
    public static CellColor FromTheme(int index, double tint = 0) => new CellColor(2, 0, index, tint);

    /// <summary>Whether the file stated no colour.</summary>
    public bool IsAutomatic => _kind == 0;

    /// <summary>The colour as <c>0xRRGGBB</c>; null for an automatic or a theme colour.</summary>
    public int? Rgb => _kind == 1 ? _rgb : (int?)null;

    /// <summary>The theme slot; null unless this is a theme colour.</summary>
    public int? Theme => _kind == 2 ? _theme : (int?)null;

    /// <summary>A theme colour's tint, from -1 (darkest) to 1 (lightest); 0 otherwise.</summary>
    public double Tint => _kind == 2 ? _tint : 0;

    /// <inheritdoc/>
    public bool Equals(CellColor other)
      => _kind == other._kind && _rgb == other._rgb && _theme == other._theme && _tint.Equals(other._tint);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CellColor other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => (((_kind * 397) ^ _rgb) * 397 ^ _theme) * 397 ^ _tint.GetHashCode();

    /// <summary>Whether two colours are the same colour.</summary>
    public static bool operator ==(CellColor first, CellColor second) => first.Equals(second);

    /// <summary>Whether two colours differ.</summary>
    public static bool operator !=(CellColor first, CellColor second) => !first.Equals(second);

    /// <summary><c>#FF0000</c>, <c>theme 4 (tint 0.4)</c>, or <c>automatic</c>.</summary>
    public override string ToString()
      => _kind == 1 ? "#" + _rgb.ToString("X6", CultureInfo.InvariantCulture)
       : _kind == 2 ? _tint.Equals(0d)
          ? "theme " + _theme.ToString(CultureInfo.InvariantCulture)
          : $"theme {_theme.ToString(CultureInfo.InvariantCulture)} (tint {_tint.ToString("0.###", CultureInfo.InvariantCulture)})"
       : "automatic";
  }

  /// <summary>How a cell's text is set: its colour and the three marks a reader is likely to mean something by.</summary>
  public readonly struct CellFont
  {
    /// <summary>A font with these properties.</summary>
    public CellFont(CellColor color, bool bold, bool italic, bool strikethrough)
    {
      Color = color;
      Bold = bold;
      Italic = italic;
      Strikethrough = strikethrough;
    }

    /// <summary>The text's colour.</summary>
    public CellColor Color { get; }

    /// <summary>Whether the text is bold.</summary>
    public bool Bold { get; }

    /// <summary>Whether the text is italic.</summary>
    public bool Italic { get; }

    /// <summary>Whether the text is struck through.</summary>
    public bool Strikethrough { get; }
  }

  /// <summary>How a cell is filled: the colour behind its text, automatic where it has none.</summary>
  public readonly struct CellFill
  {
    /// <summary>A fill of this colour.</summary>
    public CellFill(CellColor color) => Color = color;

    /// <summary>The fill's colour; automatic for a cell with no fill.</summary>
    public CellColor Color { get; }
  }
}
