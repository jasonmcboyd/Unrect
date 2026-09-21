using System;
using System.Globalization;

using Unrect.Core;

namespace Unrect
{
  /// <summary>
  /// A rectangular array of <typeparamref name="T"/> viewed as a space: the canonical four answered
  /// from three rules the source supplies once, and the values themselves through
  /// <see cref="IValueCells{T}"/>.
  /// <para>
  /// The array is indexed <c>[row, column]</c>, the way a 2D array literal reads on the page, while
  /// a space is indexed <c>[column, row]</c>, the way a spreadsheet address does. This type is where
  /// that transposition happens, and it is the reason no source has to think about it.
  /// </para>
  /// <para>
  /// Slicing is arithmetic done by the plane, so there is nothing to slice here: the array is read
  /// at the coordinates a locator names.
  /// </para>
  /// <para>
  /// Useful directly, not only to adapters: a test or a script with values already in hand can build
  /// one through <see cref="GridSpace"/>'s factories and skip the file entirely, and everything
  /// above it behaves exactly as it does over a workbook.
  /// </para>
  /// </summary>
  /// <typeparam name="T">What every cell of the grid holds.</typeparam>
  public sealed class GridSpace<T> : IValueCells<T>
  {
    private readonly T[,] _values;
    private readonly Func<T, bool> _isBlank;
    private readonly Func<T, bool> _isText;
    private readonly Func<T, string> _asText;

    /// <summary>
    /// The whole of <paramref name="values"/>, as a space. The three rules are the whole of what a
    /// source has to decide: which values are empty cells, which say a word of their own, and what
    /// the rest render as.
    /// </summary>
    /// <param name="values">The cells, indexed <c>[row, column]</c>.</param>
    /// <param name="isBlank">Which values count as an empty cell.</param>
    /// <param name="isText">Which values are their own text rather than something rendered.</param>
    /// <param name="asText">What a non-blank value says.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public GridSpace(T[,] values, Func<T, bool> isBlank, Func<T, bool> isText, Func<T, string> asText)
    {
      _values = values ?? throw new ArgumentNullException(nameof(values));
      _isBlank = isBlank ?? throw new ArgumentNullException(nameof(isBlank));
      _isText = isText ?? throw new ArgumentNullException(nameof(isText));
      _asText = asText ?? throw new ArgumentNullException(nameof(asText));

      Area = new Area(values.GetLength(1), values.GetLength(0));
    }

    /// <inheritdoc/>
    public Area Area { get; }

    /// <inheritdoc/>
    public T ValueAt(int column, int row)
    {
      // OutOfBoundsException and not IndexOutOfRangeException: running off the edge of a space is a
      // statement about the data that a declaration may recover from, where an index bug is on the
      // engine's fault list and would make the overrun unrecoverable.
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      return _values[row, column];
    }

    /// <inheritdoc/>
    public bool IsBlank(int column, int row) => _isBlank(ValueAt(column, row));

    /// <inheritdoc/>
    public bool TryGetTextAt(int column, int row, out string value, out CellProblem? problem)
    {
      var held = ValueAt(column, row);

      problem = null;

      if (_isBlank(held) || !_isText(held))
      {
        value = null!;
        return false;
      }

      value = _asText(held);
      return true;
    }

    /// <inheritdoc/>
    public string? AsText(int column, int row)
    {
      var value = ValueAt(column, row);

      return _isBlank(value) ? null : _asText(value);
    }
  }

  /// <summary>
  /// How a plain array becomes a space. Each factory settles the three rules
  /// <see cref="GridSpace{T}"/> needs — and the first of them, <em>blankness</em>, is the one
  /// question a source must answer that a grid cannot guess.
  /// </summary>
  public static class GridSpace
  {
    /// <summary>
    /// Values of any type, with all three rules stated. The general door: everything else here is
    /// this with a rule already chosen.
    /// </summary>
    /// <typeparam name="T">What every cell holds.</typeparam>
    /// <param name="values">The cells, indexed <c>[row, column]</c>.</param>
    /// <param name="isBlank">Which values count as an empty cell.</param>
    /// <param name="isText">Which values are their own text rather than something rendered.</param>
    /// <param name="asText">What a non-blank value says.</param>
    public static GridSpace<T> Create<T>(T[,] values, Func<T, bool> isBlank, Func<T, bool> isText, Func<T, string> asText)
      => new GridSpace<T>(values, isBlank, isText, asText);

    /// <summary>
    /// Text, where the blankness default is that null or empty is an empty cell. Every non-blank
    /// cell is its own text.
    /// </summary>
    /// <param name="values">The cells, indexed <c>[row, column]</c>.</param>
    public static GridSpace<string?> Create(string?[,] values)
      => new GridSpace<string?>(values, value => string.IsNullOrEmpty(value), _ => true, value => value!);

    /// <summary>
    /// Numbers, with <paramref name="isBlank"/> deciding which count as empty cells. A number is
    /// rendered rather than spoken, so <c>IsText</c> is false throughout.
    /// </summary>
    /// <param name="values">The cells, indexed <c>[row, column]</c>.</param>
    /// <param name="isBlank">Which numbers count as an empty cell; none, where omitted.</param>
    public static GridSpace<int> Create(int[,] values, Func<int, bool>? isBlank = null)
      => new GridSpace<int>(values, Blankness(isBlank), _ => false, value => value.ToString(CultureInfo.InvariantCulture));

    /// <inheritdoc cref="Create(int[,], Func{int, bool})"/>
    public static GridSpace<double> Create(double[,] values, Func<double, bool>? isBlank = null)
      => new GridSpace<double>(values, Blankness(isBlank), _ => false, Rendered);

    /// <summary>
    /// Heterogeneous values, each reading as the kind its CLR type implies: null and the empty
    /// string are blank, a string is its own text, and a number, a date or a boolean renders the way
    /// a spreadsheet's does.
    /// <para>
    /// The array-adapter equivalent of a real sheet, and the reason a fixture can be written as a
    /// literal. A CLR type with no rendering here is an error where the grid is built, not a cell
    /// that reads as something surprising.
    /// </para>
    /// </summary>
    /// <param name="values">The cells, indexed <c>[row, column]</c>.</param>
    public static GridSpace<object?> Create(object?[,] values)
      => new GridSpace<object?>(
        values,
        value => value is null || (value is string text && text.Length == 0),
        value => value is string,
        Rendered);

    private static Func<T, bool> Blankness<T>(Func<T, bool>? isBlank) => isBlank ?? (_ => false);

    /// <summary>
    /// A double as a sheet renders it: the shortest digits that read back exactly, found the same
    /// way on both of this package's targets — so <c>0.1 + 0.2</c> says "0.30000000000000004"
    /// through either, never "0.3" through one of them.
    /// </summary>
    private static string Rendered(double value) => Renderings.ShortestRoundTrip(value);

    /// <summary>
    /// One heterogeneous value as the text its kind implies. A date says its date; a moment within a
    /// day says the time too, rather than silently rendering as the midnight it is not.
    /// </summary>
    private static string Rendered(object? value)
      => value switch
      {
        string text => text,
        int number => number.ToString(CultureInfo.InvariantCulture),
        long number => number.ToString(CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        double number => Rendered(number),
        bool flag => flag ? "TRUE" : "FALSE",
        DateTime moment => moment.ToString(
          moment.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
          CultureInfo.InvariantCulture),
        null => throw new ArgumentException("A blank cell has no rendering.", nameof(value)),
        _ => throw new ArgumentException($"No rendering for {value.GetType()} in a grid of values.", nameof(value)),
      };
  }
}
