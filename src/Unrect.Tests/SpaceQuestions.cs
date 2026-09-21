using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// Questions a TEST puts to a space at a coordinate, each derived from the space's reads — the
  /// way the point extensions are, and for the same reason: there is one definition of what a cell
  /// can be read as.
  /// <para>
  /// The summaries here (<c>KindAt</c>, <c>Describe</c>) are an observer's, not the
  /// library's: a test of a lexer wants one word for what a door made of a value, and gets it by
  /// asking every read in turn. The library offers no such summary, because outside a fixture a
  /// cell can be read several ways at once.
  /// </para>
  /// </summary>
  internal static class SpaceQuestions
  {
    /// <summary>Whether the cell holds text of its own — true exactly when the read would hand it back.</summary>
    public static bool IsText(this ISpace space, int column, int row)
      => space.TryGetTextAt(column, row, out _, out _);

    /// <summary>Whether the cell carries an error.</summary>
    public static bool IsErrorAt(this ICellSpace space, int column, int row)
      => space.TryGetErrorAt(column, row, out _);

    /// <summary>The error's spelling, or null where the cell carries none.</summary>
    public static string? ErrorTextAt(this ICellSpace space, int column, int row)
      => space.TryGetErrorAt(column, row, out var error) ? error : null;

    /// <summary>Which read the cell answers, as the lexer's one word for it.</summary>
    public static CellKind KindAt(this ICellSpace space, int column, int row)
      => space.IsBlank(column, row) ? CellKind.Blank
        : space.TryGetTextAt(column, row, out _, out _) ? CellKind.Text
        : space.TryGetDoubleAt(column, row, out _, out _) ? CellKind.Number
        : space.TryGetDateTimeAt(column, row, out _, out _) ? CellKind.Temporal
        : space.TryGetBooleanAt(column, row, out _, out _) ? CellKind.Boolean
        : CellKind.Error;

    /// <summary>The same word as a message says it after "found": an error spells itself out.</summary>
    public static string Describe(this ICellSpace space, int column, int row)
      => space.TryGetErrorAt(column, row, out var error) ? $"Error({error})" : space.KindAt(column, row).ToString();

    /// <inheritdoc cref="Describe(ICellSpace, int, int)"/>
    public static string Describe(this Point<ICellSpace> point)
      => point.Space.Describe(point.Column, point.Row);
  }
}
