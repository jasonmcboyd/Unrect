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

    public int Row { get; }
    public int Column { get; }
    public Size Available { get; }

    public string A1 => ColumnName(Column) + Row;

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
