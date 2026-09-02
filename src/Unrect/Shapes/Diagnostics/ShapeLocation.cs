using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// Where a shape landed, in 1-based coordinates relative to the space <c>Map</c> was called with.
  /// </summary>
  public readonly struct ShapeLocation
  {
    internal ShapeLocation(int row, int column, Size available)
    {
      Row = row;
      Column = column;
      Available = available;
    }

    /// <summary>
    /// The 1-based address of a 0-based offset from the space <c>Map</c> was called with — the one
    /// place that conversion is done.
    /// </summary>
    internal static ShapeLocation At(Offset origin, Size available)
      => new ShapeLocation(origin.Height + 1, origin.Width + 1, available);

    /// <summary>
    /// Whether this is the cell at <paramref name="origin"/>, however much space was available
    /// there — the same conversion, asked the other way round.
    /// </summary>
    internal bool IsAt(Offset origin)
    {
      var there = At(origin, Available);

      return Row == there.Row && Column == there.Column;
    }

    /// <summary>The 1-based row.</summary>
    public int Row { get; }

    /// <summary>The 1-based column.</summary>
    public int Column { get; }

    /// <summary>The extent of the space this location was resolved against, for citing "N available" alongside it.</summary>
    public Size Available { get; }

    /// <summary>The spreadsheet-style address, e.g. <c>"B4"</c>.</summary>
    public string A1 => ColumnName(Column) + Row;

    /// <summary>The form every <see cref="ShapeException"/> and <see cref="ShapeDiagnostic"/> message uses.</summary>
    public override string ToString() => $"row {Row}, column {Column} ({A1})";

    private static string ColumnName(int column)
    {
      var name = string.Empty;

      for (var value = column; value > 0; value = (value - 1) / 26)
        name = (char)('A' + ((value - 1) % 26)) + name;

      return name;
    }
  }
}
