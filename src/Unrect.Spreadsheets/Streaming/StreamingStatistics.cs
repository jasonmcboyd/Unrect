namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What reading one sheet through <see cref="Workbook.Sheet"/> has cost: the rows the pass
  /// loaded, the most it held at once, the cap it was held under, and the rows a sheet that would
  /// not say how big it was had to be read to measure. The vocabulary to act on: a peak near the
  /// sheet's height says the declaration holds its extent, and the shape that holds is what to
  /// bound.
  /// </summary>
  public readonly struct StreamingStatistics
  {
    internal StreamingStatistics(string sheetName, long rowsRead, int peakRetained, int? cap, long rowsMeasured)
    {
      SheetName = sheetName;
      RowsRead = rowsRead;
      PeakRetained = peakRetained;
      Cap = cap;
      RowsMeasured = rowsMeasured;
    }

    /// <summary>The sheet these figures are for.</summary>
    public string SheetName { get; }

    /// <summary>Rows loaded by the pass — every row the declaration was offered, or asked for directly.</summary>
    public long RowsRead { get; }

    /// <summary>The most rows held at once — what the declaration's holds cost.</summary>
    public int PeakRetained { get; }

    /// <summary>The cap the pass ran under (<see cref="WorkbookOptions.BufferRows"/>), or null for none.</summary>
    public int? Cap { get; }

    /// <summary>
    /// Rows read to measure a sheet that would not say how big it was — a pass the caller never
    /// asked for, visible where its cost is read. Zero for a sheet that described itself.
    /// </summary>
    public long RowsMeasured { get; }

    /// <summary>One line: rows read, peak retained, the cap, and the survey if there was one.</summary>
    public override string ToString() =>
      $"'{SheetName}': {RowsRead} rows read, peak {PeakRetained} retained"
      + (Cap is int cap ? $" of {cap} allowed" : string.Empty)
      + (RowsMeasured > 0 ? $", {RowsMeasured} measured" : string.Empty);
  }
}
