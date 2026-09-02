namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What is known about a worksheet before it is read: enough to decide whether to read it at all.
  /// A predicate over these is how <see cref="SpreadsheetSpace"/> picks sheets out of a workbook
  /// whose names vary between exports.
  /// </summary>
  public readonly struct SpreadsheetContext
  {
    /// <summary>Describes the sheet at <paramref name="index"/> called <paramref name="name"/>.</summary>
    /// <param name="index">The sheet's zero-based position in the workbook.</param>
    /// <param name="name">The sheet's name, as the workbook records it.</param>
    public SpreadsheetContext(int index, string name)
    {
      Index = index;
      Name = name;
    }

    /// <summary>The sheet's zero-based position in the workbook.</summary>
    public int Index { get; }

    /// <summary>The sheet's name, as the workbook records it — untrimmed and in its own casing.</summary>
    public string Name { get; }
  }
}
