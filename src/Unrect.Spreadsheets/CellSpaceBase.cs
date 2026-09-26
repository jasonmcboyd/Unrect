using System;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The text facet of a space of <see cref="CellValue"/>s, derived once from the value so that no
  /// two sheets can disagree about a cell: a cell counts as content unless it holds nothing, or
  /// holds text the space's blankness rule calls blank; and it says what its value renders as, the
  /// empty string for nothing. What a cell says and whether it counts are two questions, so a cell
  /// the rule calls blank still says its text. An implementer writes the extent and <c>ValueAt</c>.
  /// </summary>
  public abstract class CellSpaceBase : ICellSpace
  {
    private readonly Func<string, bool>? _textIsBlank;

    /// <summary>A space whose text cells are blank when <paramref name="textIsBlank"/> says so; null for one in which only a cell holding nothing is blank.</summary>
    private protected CellSpaceBase(Func<string, bool>? textIsBlank)
    {
      _textIsBlank = textIsBlank;
    }

    /// <inheritdoc/>
    public abstract Size Extent { get; }

    /// <inheritdoc/>
    public abstract CellValue ValueAt(int column, int row);

    /// <inheritdoc/>
    public bool IsBlankAt(int column, int row)
    {
      var value = ValueAt(column, row);

      return value.Kind == CellKind.Blank
        || (_textIsBlank is not null && value.TryGetText(out var text) && _textIsBlank(text));
    }

    /// <inheritdoc/>
    public string AsTextAt(int column, int row) => ValueAt(column, row).AsText();
  }
}
