using System;

namespace Unrect.Spreadsheets
{
  /// <summary>How a <see cref="Workbook"/> reads: what counts as blank, how much a pass may hold, how names match, how much text is shared.</summary>
  public sealed class WorkbookOptions
  {
    /// <summary>
    /// What counts as a blank cell, decided where data enters the system. Null means the default:
    /// a cell that is empty or holds only whitespace — the same rule the eager door applies.
    /// </summary>
    public Func<CellValue, bool>? IsBlank { get; init; }

    /// <summary>
    /// The most rows a sheet read through <see cref="Workbook.Sheet"/> may hold at once, or null
    /// for no limit. Exceeding it is a fault naming the shape that holds them — the declaration
    /// asks for more than a forward pass can keep — never a degraded read.
    /// </summary>
    public int? BufferRows { get; init; }

    /// <summary>Whether sheet names match by case. Off by default, as Excel itself treats them.</summary>
    public bool CaseSensitiveSheetNames { get; init; }

    /// <summary>
    /// How many distinct text values one workbook shares across every sheet it reads: equal text
    /// cells hand back one instance. Zero shares nothing; the default is generous enough for a
    /// real workbook's vocabulary. See <see cref="Workbook.InterningStatistics"/>.
    /// </summary>
    public int MaxInternedStrings { get; init; } = DefaultMaxInternedStrings;

    internal const int DefaultMaxInternedStrings = 65_536;

    internal void Validate()
    {
      if (BufferRows is int buffer && buffer < 1)
        throw new ArgumentOutOfRangeException(nameof(BufferRows), buffer, "A buffer cap must be at least one row; pass null for no cap.");

      if (MaxInternedStrings < 0)
        throw new ArgumentOutOfRangeException(
          nameof(MaxInternedStrings),
          MaxInternedStrings,
          "The interning cap cannot be negative; pass 0 to share nothing.");
    }
  }
}
