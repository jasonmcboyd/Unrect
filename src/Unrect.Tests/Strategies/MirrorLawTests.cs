using System;
using System.Linq;

using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// The drift guard for the row/column mirror. The two axis families are INDEPENDENT
  /// implementations of one intended denotation — <c>TakeRowsWhileAll</c>/<c>Any</c> scan a row's
  /// cells and answer per row while their column twins accumulate row-major behind a monotone
  /// "settled" early exit; <c>TakeRowsWhile</c> is a negated predicate through the take-to strategy
  /// while <c>TakeColumnsWhile</c> is a dedicated loop; the two landmark families are separate
  /// seven-line classes. Nothing is shared, so nothing but a test holds them to the same answer.
  /// <para>
  /// They stay separate on purpose. Mechanizing the mirror by transposing the space would transpose
  /// the diagnostic locations with it and corrupt the A1 story a user reads, so the axis vocabulary
  /// is hand-written on both sides and the symmetry is recorded as a law rather than compiled away.
  /// This suite is that record: each pair is run over a grid and over that grid's TRANSPOSE, and the
  /// answers must be each other's transposes.
  /// </para>
  /// <para>
  /// <see cref="TheMirrorStopsAtTheDescription"/> is the negative half, stating the specific place
  /// the symmetry is deliberately broken: the message names its axis, because that is the half the
  /// user reads.
  /// </para>
  /// <para>
  /// The <c>Observations</c> harness does not fit: these are strategies, not projections — there is
  /// no value, no consumed extent and no diagnostics to observe, only a count, an offset or an
  /// index. Plain asserts.
  /// </para>
  /// </summary>
  public class MirrorLawTests
  {
    private static bool HasValue(CellValue value) => value.HasValue;

    // --- The grids ------------------------------------------------------------------------------------
    //
    // Row-major, so grid[row, column]; null is blank. Every law below is checked over all of them in
    // BOTH directions (row form over the grid against column form over the transpose, and the other
    // way round), which is what makes the degenerate shapes carry their weight: a grid with no
    // columns transposes into one with no rows, and the two families answer that question with
    // completely different code.

    /// <summary>Grids that hold at least one cell, so a predicate may address one.</summary>
    private static readonly string?[][,] WithCells =
    {
      // Ragged: leading content, an all-blank band, then content again.
      new string?[,]
      {
        { "a", "b", null, "d" },
        { "e", null, null, "h" },
        { null, null, null, null },
        { "m", "n", "o", "p" },
      },
      new string?[,] { { "a", null, "c" } },                    // one row
      new string?[,] { { "a" }, { null }, { "c" } },            // one column
      new string?[,] { { null, null }, { null, null } },        // all blank
      new string?[,] { { "a", "b" }, { "c", "d" } },            // all full
      new string?[,] { { "m" } },                               // one cell, and it is the landmark
    };

    /// <summary>
    /// Every grid, including the two degenerate ones. A grid with no columns transposes into one
    /// with no rows, and the two families answer that question with completely different code — the
    /// row forms by looping over a row's cells, the column forms by never running their
    /// accumulator — so it is the sharpest probe here. It is excluded only from the laws whose
    /// predicate has to address a fixed cell, which such a grid does not have.
    /// </summary>
    private static readonly string?[][,] Grids = WithCells
      .Concat(new string?[][,]
      {
        new string?[0, 0],                                      // empty
        new string?[3, 0],                                      // three rows, no columns
      })
      .ToArray();

    /// <summary>
    /// The grid with its axes swapped, so <c>transposed[column, row] == grid[row, column]</c> — the
    /// only piece of machinery this suite needs, and deliberately generic so the text grids and any
    /// later numeric ones use the same one.
    /// </summary>
    private static T[,] Transposed<T>(T[,] values)
    {
      var rows = values.GetLength(0);
      var columns = values.GetLength(1);
      var transposed = new T[columns, rows];

      for (var row = 0; row < rows; row++)
        for (var column = 0; column < columns; column++)
          transposed[column, row] = values[row, column];

      return transposed;
    }

    /// <summary>
    /// Runs <paramref name="alongRows"/> over each grid and <paramref name="alongColumns"/> over its
    /// transpose, and the other way round, asserting the two agree. Generic in the answer so a count
    /// (<c>int</c>) and a located index (<c>int?</c>) are pinned by the same helper.
    /// </summary>
    private static void Mirrored<T>(Func<ISpace, T> alongRows, Func<ISpace, T> alongColumns)
      => Mirrored(Grids, alongRows, alongColumns);

    /// <summary>The same, over a chosen set of grids.</summary>
    private static void Mirrored<T>(string?[][,] grids, Func<ISpace, T> alongRows, Func<ISpace, T> alongColumns)
    {
      foreach (var grid in grids)
      {
        var space = Text(grid);
        var transposed = Text(Transposed(grid));

        Assert.Equal(alongRows(space), alongColumns(transposed));
        Assert.Equal(alongColumns(space), alongRows(transposed));
      }
    }

    // --- The anchor: these laws are claims, not coincidences ----------------------------------------------

    [Fact]
    public void TheRaggedGridAnswersDifferentlyOnEachAxis_SoTheMirrorIsARealClaim()
    {
      // Every other test here asserts an equality between two computed numbers, which would hold
      // vacuously if both families answered 0 everywhere. This one spells the numbers out on the
      // asymmetric grid: the row form sees two rows before the blank band, the column form sees all
      // four columns, and transposing the grid swaps exactly those two answers.
      var ragged = Text(WithCells[0]);
      var transposed = Text(Transposed(WithCells[0]));

      Assert.Equal(2, RowStrategies.TakeRowsWhileAnyValue().SelectRows(ragged));
      Assert.Equal(4, ColumnStrategies.TakeColumnsWhileAnyValue().SelectColumns(ragged));

      Assert.Equal(4, RowStrategies.TakeRowsWhileAnyValue().SelectRows(transposed));
      Assert.Equal(2, ColumnStrategies.TakeColumnsWhileAnyValue().SelectColumns(transposed));

      // The same, located: 'o' sits in the last row and the third column, and swaps places under
      // the transpose.
      Assert.Equal(3, RowLandmarks.RowContaining("o").FindRow(ragged));
      Assert.Equal(2, ColumnLandmarks.ColumnContaining("o").FindColumn(ragged));

      Assert.Equal(2, RowLandmarks.RowContaining("o").FindRow(transposed));
      Assert.Equal(3, ColumnLandmarks.ColumnContaining("o").FindColumn(transposed));
    }

    // --- The while families: separate algorithms, one denotation (SRC-59) --------------------------------

    [Fact]
    public void TakeRowsWhileAnyValue_MirrorsTakeColumnsWhileAnyValue()
    {
      Mirrored(
        RowStrategies.TakeRowsWhileAnyValue().SelectRows,
        ColumnStrategies.TakeColumnsWhileAnyValue().SelectColumns);
    }

    [Fact]
    public void TakeRowsWhileAll_MirrorsTakeColumnsWhileAll()
    {
      Mirrored(
        RowStrategies.TakeRowsWhileAll(HasValue).SelectRows,
        ColumnStrategies.TakeColumnsWhileAll(HasValue).SelectColumns);
    }

    [Fact]
    public void TakeRowsWhileAny_MirrorsTakeColumnsWhileAny()
    {
      Mirrored(
        RowStrategies.TakeRowsWhileAny(HasValue).SelectRows,
        ColumnStrategies.TakeColumnsWhileAny(HasValue).SelectColumns);
    }

    // --- The positional forms: a negated take-to against a dedicated loop (SRC-57) -------------------------

    [Fact]
    public void TakeRowsWhile_MirrorsTakeColumnsWhile()
    {
      // A space predicate addresses cells, so the predicate is transposed along with the grid — the
      // one place a caller has to mirror something by hand, and the reason this law runs over the
      // grids that have a cell to address. "Take while the leading cell is not m."
      Mirrored(
        WithCells,
        RowStrategies.TakeRowsWhile((s, row) => s[0, row].TryGetString() != "m").SelectRows,
        ColumnStrategies.TakeColumnsWhile((s, column) => s[column, 0].TryGetString() != "m").SelectColumns);

      // AllRows/AllColumns are the same pair with the constant predicate, which needs no cell to
      // address — so they are checked over every grid, degenerate ones included.
      Mirrored(RowStrategies.AllRows().SelectRows, ColumnStrategies.AllColumns().SelectColumns);
    }

    [Fact]
    public void TakeRowsTo_MirrorsTakeColumnsTo()
    {
      // The inclusive reading, which is the only one the column side offers: the row class carries a
      // keep-the-match flag the column class does not, so the shared denotation is this one.
      Mirrored(
        WithCells,
        RowStrategies.TakeRowsTo((s, row) => s[0, row].TryGetString() == "m").SelectRows,
        ColumnStrategies.TakeColumnsTo((s, column) => s[column, 0].TryGetString() == "m").SelectColumns);

      // ...and the by-value spelling of the same pair, which addresses its own cell.
      Mirrored(
        WithCells,
        RowStrategies.TakeRowsToValue(0, CellValue.Of("m")).SelectRows,
        ColumnStrategies.TakeColumnsToValue(0, CellValue.Of("m")).SelectColumns);
    }

    // --- Explicit counts ---------------------------------------------------------------------------------

    [Fact]
    public void TakeRows_MirrorsTakeColumns()
    {
      foreach (var grid in Grids)
      {
        var space = Text(grid);
        var transposed = Text(Transposed(grid));

        for (var count = 0; count <= space.Area.Height; count++)
          Assert.Equal(
            RowStrategies.TakeRows(count).SelectRows(space),
            ColumnStrategies.TakeColumns(count).SelectColumns(transposed));

        for (var count = 0; count <= space.Area.Width; count++)
          Assert.Equal(
            ColumnStrategies.TakeColumns(count).SelectColumns(space),
            RowStrategies.TakeRows(count).SelectRows(transposed));
      }
    }

    [Fact]
    public void TakeRows_AndTakeColumns_OverrunTheSameWay()
    {
      // Both axes refuse rather than clamp, and refuse with the same exception type — the property a
      // Repeat and a tolerance boundary both key on. This is the one pair where even the message
      // mirrors, and only because there is none: both throw a bare OutOfBoundsException carrying no
      // extents and no location. If that ever grows diagnostics, an axis word arrives with them and
      // this pin joins TheMirrorStopsAtTheDescription.
      foreach (var grid in Grids)
      {
        var space = Text(grid);
        var transposed = Text(Transposed(grid));

        Assert.Throws<OutOfBoundsException>(
          () => RowStrategies.TakeRows(space.Area.Height + 1).SelectRows(space));

        Assert.Throws<OutOfBoundsException>(
          () => ColumnStrategies.TakeColumns(space.Area.Height + 1).SelectColumns(transposed));
      }

      Assert.Equal(
        "count",
        Assert.Throws<ArgumentOutOfRangeException>(() => RowStrategies.TakeRows(-1)).ParamName);

      Assert.Equal(
        "count",
        Assert.Throws<ArgumentOutOfRangeException>(() => ColumnStrategies.TakeColumns(-1)).ParamName);
    }

    // --- Offsets: the two lifts mirror through (0, rows) and (columns, 0) ----------------------------------

    [Fact]
    public void SkipBlankRows_MirrorsSkipBlankColumns()
    {
      Mirrored(
        space => OffsetStrategies.SkipBlankRows().GetOffset(space).Height,
        space => OffsetStrategies.SkipBlankColumns().GetOffset(space).Width);

      // The other half of the offset mirror: each lift is (0, rows) or (columns, 0), so a skip
      // declared on one axis can never quietly displace the other.
      foreach (var grid in Grids)
      {
        var space = Text(grid);

        Assert.Equal(0, OffsetStrategies.SkipBlankRows().GetOffset(space).Width);
        Assert.Equal(0, OffsetStrategies.SkipBlankColumns().GetOffset(space).Height);
      }
    }

    [Fact]
    public void SkipRowsWhile_MirrorsSkipColumnsWhile_OnBothQuantifiers()
    {
      Mirrored(
        space => OffsetStrategies.SkipRowsWhileAny(HasValue).GetOffset(space).Height,
        space => OffsetStrategies.SkipColumnsWhileAny(HasValue).GetOffset(space).Width);

      // The two-argument "all" form, which the suite otherwise only exercises through its
      // zero-argument spelling, SkipBlankRows.
      Mirrored(
        space => OffsetStrategies.SkipRowsWhileAll(HasValue).GetOffset(space).Height,
        space => OffsetStrategies.SkipColumnsWhileAll(HasValue).GetOffset(space).Width);
    }

    // --- Sizes: the mirror is a transposed rectangle, not a transposed count -------------------------------

    [Fact]
    public void RowsWhileAny_MirrorsColumnsWhileAny_AsATransposedRectangle()
    {
      // Each takes its own axis by the predicate and the other axis whole, so the mirror claim is
      // about the whole Size: what the row form returns as (width, height) the column form returns
      // as (height, width) over the transpose.
      foreach (var grid in Grids)
      {
        var space = Text(grid);
        var transposed = Text(Transposed(grid));

        var byRows = SizeStrategies.RowsWhileAny(HasValue).GetSize(space);
        var byColumns = SizeStrategies.ColumnsWhileAny(HasValue).GetSize(transposed);

        Assert.Equal(byRows.Height, byColumns.Width);
        Assert.Equal(byRows.Width, byColumns.Height);

        var byValue = SizeStrategies.RowsWhileAnyValue().GetSize(space);
        var byValueColumns = SizeStrategies.ColumnsWhileAnyValue().GetSize(transposed);

        Assert.Equal(byValue.Height, byValueColumns.Width);
        Assert.Equal(byValue.Width, byValueColumns.Height);
      }
    }

    // --- Landmarks: separate classes, mirrored positions (SRC-61) --------------------------------------------

    [Fact]
    public void RowContaining_MirrorsColumnContaining()
    {
      Mirrored(
        RowLandmarks.RowContaining("o").FindRow,
        ColumnLandmarks.ColumnContaining("o").FindColumn);

      // A miss is null on both axes rather than an empty answer, and the trimmed,
      // case-insensitive, whole-cell rule is the same rule on both — the predicate is shared, the
      // scan around it is not.
      Mirrored(
        RowLandmarks.RowContaining("nope").FindRow,
        ColumnLandmarks.ColumnContaining("nope").FindColumn);

      Mirrored(
        RowLandmarks.RowContaining("  O  ").FindRow,
        ColumnLandmarks.ColumnContaining("  O  ").FindColumn);

      Mirrored(
        RowLandmarks.RowContaining("").FindRow,
        ColumnLandmarks.ColumnContaining("").FindColumn);
    }

    [Fact]
    public void RowWithCell_MirrorsColumnWithCell()
    {
      Mirrored(
        RowLandmarks.RowWithCell(cell => cell.TryGetString() == "h").FindRow,
        ColumnLandmarks.ColumnWithCell(cell => cell.TryGetString() == "h").FindColumn);
    }

    [Fact]
    public void RowWhere_MirrorsColumnWhere()
    {
      // A space predicate again, so the cell address is transposed by hand: "the first row whose
      // leading cell is m" against "the first column whose leading cell is m".
      Mirrored(
        WithCells,
        RowLandmarks.RowWhere((s, row) => s[0, row].TryGetString() == "m").FindRow,
        ColumnLandmarks.ColumnWhere((s, column) => s[column, 0].TryGetString() == "m").FindColumn);
    }

    // --- Where the mirror deliberately stops --------------------------------------------------------------

    [Fact]
    public void TheMirrorStopsAtTheDescription()
    {
      // The specific, intended break. Every law above is about a number; this is about the sentence
      // built from it, and the sentence names the axis it is talking about. A mechanized transpose
      // would have made these one string and sent the reader looking down the wrong axis.
      Assert.Equal("no row containing 'Total'", RowLandmarks.RowContaining("Total").Description);
      Assert.Equal("no column containing 'Total'", ColumnLandmarks.ColumnContaining("Total").Description);

      Assert.Equal("no row with a matching cell", RowLandmarks.RowWithCell(_ => false).Description);
      Assert.Equal("no column with a matching cell", ColumnLandmarks.ColumnWithCell(_ => false).Description);

      Assert.Equal("no matching row", RowLandmarks.RowWhere((_, _) => false).Description);
      Assert.Equal("no matching column", ColumnLandmarks.ColumnWhere((_, _) => false).Description);
    }
  }
}
