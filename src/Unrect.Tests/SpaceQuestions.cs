using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// Questions a TEST puts to a space at a coordinate, each derived from the space's reads — the
  /// way the point extensions are, and for the same reason: there is one definition of what a cell
  /// can be read as.
  /// <para>
  /// <c>Describe</c> is the word a failure sentence says after "found", asked of a cell by
  /// coordinate; a test of a lexer wants that one word for what a door made of a value.
  /// </para>
  /// </summary>
  internal static class SpaceQuestions
  {
    /// <summary>Whether the cell holds text of its own — true exactly when the read would hand it back.</summary>
    public static bool IsText(this ISpace space, int column, int row)
      => space.TryGetTextAt(column, row, out _, out _);

    /// <summary>The error's spelling, or null where the cell carries none.</summary>
    public static string? ErrorTextAt(this ICellSpace space, int column, int row)
      => space.ValueAt(column, row) is { Kind: CellKind.Error } value ? value.AsText() : null;

    /// <summary>The cell's formula, or null where it has none.</summary>
    public static string? FormulaAt(this IFormulaSpace space, int column, int row)
      => space.TryGetFormulaAt(column, row, out var formula) ? formula : null;

    /// <summary>The same word as a message says it after "found": an error spells itself out.</summary>
    public static string Describe(this ICellSpace space, int column, int row)
      => CellReading.Describe(space.ValueAt(column, row));

    /// <inheritdoc cref="Describe(ICellSpace, int, int)"/>
    public static string Describe(this Point<ICellSpace> point)
      => point.Space.Describe(point.Column, point.Row);
  }
}
