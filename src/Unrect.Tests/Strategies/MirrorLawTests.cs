using System;
using System.Linq;

using Unrect.Core;
using Unrect.Spreadsheets;
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
    private static bool HasValue(Point<ISpace> value) => !value.IsBlank();

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
    private static void Mirrored<T>(Func<ICellSpace, T> alongRows, Func<ICellSpace, T> alongColumns)
      => Mirrored(Grids, alongRows, alongColumns);

    /// <summary>The same, over a chosen set of grids.</summary>
    private static void Mirrored<T>(string?[][,] grids, Func<ICellSpace, T> alongRows, Func<ICellSpace, T> alongColumns)
    {
      foreach (var grid in grids)
      {
        var space = Labels(grid);
        var transposed = Labels(Transposed(grid));

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
      var ragged = Labels(WithCells[0]);
      var transposed = Labels(Transposed(WithCells[0]));

      Assert.Equal(2, RowStrategies.TakeRowsWhileAnyIsNotBlank<ISpace>().SelectLines(ragged));
      Assert.Equal(4, ColumnStrategies.TakeColumnsWhileAnyIsNotBlank<ISpace>().SelectLines(ragged));

      Assert.Equal(4, RowStrategies.TakeRowsWhileAnyIsNotBlank<ISpace>().SelectLines(transposed));
      Assert.Equal(2, ColumnStrategies.TakeColumnsWhileAnyIsNotBlank<ISpace>().SelectLines(transposed));

      // The same, located: 'o' sits in the last row and the third column, and swaps places under
      // the transpose.
      Assert.Equal(3, RowLandmarks.RowSaying<ISpace>("o").Find(ragged));
      Assert.Equal(2, ColumnLandmarks.ColumnSaying<ISpace>("o").Find(ragged));

      Assert.Equal(2, RowLandmarks.RowSaying<ISpace>("o").Find(transposed));
      Assert.Equal(3, ColumnLandmarks.ColumnSaying<ISpace>("o").Find(transposed));
    }

    // --- The while families: separate algorithms, one denotation (SRC-59) --------------------------------

    [Fact]
    public void TakeRowsWhileAnyIsNotBlank_MirrorsTakeColumnsWhileAnyIsNotBlank()
    {
      Mirrored(
        RowStrategies.TakeRowsWhileAnyIsNotBlank<ISpace>().SelectLines,
        ColumnStrategies.TakeColumnsWhileAnyIsNotBlank<ISpace>().SelectLines);
    }

    [Fact]
    public void TakeRowsWhileAll_MirrorsTakeColumnsWhileAll()
    {
      Mirrored(
        RowStrategies.TakeRowsWhileAll<ISpace>(HasValue).SelectLines,
        ColumnStrategies.TakeColumnsWhileAll<ISpace>(HasValue).SelectLines);
    }

    [Fact]
    public void TakeRowsWhileAny_MirrorsTakeColumnsWhileAny()
    {
      Mirrored(
        RowStrategies.TakeRowsWhileAny<ISpace>(HasValue).SelectLines,
        ColumnStrategies.TakeColumnsWhileAny<ISpace>(HasValue).SelectLines);
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
        RowStrategies.TakeRowsWhile<ISpace>((s, row) => s[0, row].AsText() != "m").SelectLines,
        ColumnStrategies.TakeColumnsWhile<ISpace>((s, column) => s[column, 0].AsText() != "m").SelectLines);

      // AllRows/AllColumns are the same pair with the constant predicate, which needs no cell to
      // address — so they are checked over every grid, degenerate ones included.
      Mirrored(RowStrategies.AllRows<ISpace>().SelectLines, ColumnStrategies.AllColumns<ISpace>().SelectLines);
    }

    [Fact]
    public void TakeRowsTo_MirrorsTakeColumnsTo()
    {
      // The inclusive reading, which is the only one the column side offers: the row class carries a
      // keep-the-match flag the column class does not, so the shared denotation is this one.
      Mirrored(
        WithCells,
        RowStrategies.TakeRowsTo<ISpace>((s, row) => s[0, row].AsText() == "m").SelectLines,
        ColumnStrategies.TakeColumnsTo<ISpace>((s, column) => s[column, 0].AsText() == "m").SelectLines);

      // ...and the by-text spelling of the same pair, which addresses its own cell.
      Mirrored(
        WithCells,
        SheetProjectionBuilders<ICellSpace>.TakeRowsToText(0, "m").SelectLines,
        SheetProjectionBuilders<ICellSpace>.TakeColumnsToText(0, "m").SelectLines);
    }

    // --- Explicit counts ---------------------------------------------------------------------------------

    [Fact]
    public void TakeRows_MirrorsTakeColumns()
    {
      foreach (var grid in Grids)
      {
        var space = Labels(grid);
        var transposed = Labels(Transposed(grid));

        for (var count = 0; count <= space.Extent.Height; count++)
          Assert.Equal(
            RowStrategies.TakeRows<ISpace>(count).SelectLines(space),
            ColumnStrategies.TakeColumns<ISpace>(count).SelectLines(transposed));

        for (var count = 0; count <= space.Extent.Width; count++)
          Assert.Equal(
            ColumnStrategies.TakeColumns<ISpace>(count).SelectLines(space),
            RowStrategies.TakeRows<ISpace>(count).SelectLines(transposed));
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
        var space = Labels(grid);
        var transposed = Labels(Transposed(grid));

        Assert.Throws<OutOfBoundsException>(
          () => RowStrategies.TakeRows<ISpace>(space.Extent.Height + 1).SelectLines(space));

        Assert.Throws<OutOfBoundsException>(
          () => ColumnStrategies.TakeColumns<ISpace>(space.Extent.Height + 1).SelectLines(transposed));
      }

      Assert.Equal(
        "count",
        Assert.Throws<ArgumentOutOfRangeException>(() => RowStrategies.TakeRows<ISpace>(-1)).ParamName);

      Assert.Equal(
        "count",
        Assert.Throws<ArgumentOutOfRangeException>(() => ColumnStrategies.TakeColumns<ISpace>(-1)).ParamName);
    }

    // --- Offsets: the two lifts mirror through (0, rows) and (columns, 0) ----------------------------------

    [Fact]
    public void SkipBlankRows_MirrorsSkipBlankColumns()
    {
      Mirrored(
        space => OffsetStrategies.SkipBlankRows<ISpace>().GetOffset(space).Row,
        space => OffsetStrategies.SkipBlankColumns<ISpace>().GetOffset(space).Column);

      // The other half of the offset mirror: each lift is (0, rows) or (columns, 0), so a skip
      // declared on one axis can never quietly displace the other.
      foreach (var grid in Grids)
      {
        var space = Labels(grid);

        Assert.Equal(0, OffsetStrategies.SkipBlankRows<ISpace>().GetOffset(space).Column);
        Assert.Equal(0, OffsetStrategies.SkipBlankColumns<ISpace>().GetOffset(space).Row);
      }
    }

    [Fact]
    public void SkipRowsWhile_MirrorsSkipColumnsWhile_OnBothQuantifiers()
    {
      Mirrored(
        space => OffsetStrategies.SkipRowsWhileAny<ISpace>(HasValue).GetOffset(space).Row,
        space => OffsetStrategies.SkipColumnsWhileAny<ISpace>(HasValue).GetOffset(space).Column);

      // The two-argument "all" form, which the suite otherwise only exercises through its
      // zero-argument spelling, SkipBlankRows.
      Mirrored(
        space => OffsetStrategies.SkipRowsWhileAll<ISpace>(HasValue).GetOffset(space).Row,
        space => OffsetStrategies.SkipColumnsWhileAll<ISpace>(HasValue).GetOffset(space).Column);
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
        var space = Labels(grid);
        var transposed = Labels(Transposed(grid));

        var byRows = SizeStrategies.RowsWhileAny<ISpace>(HasValue).GetSize(space);
        var byColumns = SizeStrategies.ColumnsWhileAny<ISpace>(HasValue).GetSize(transposed);

        Assert.Equal(byRows.Height, byColumns.Width);
        Assert.Equal(byRows.Width, byColumns.Height);

        var byValue = SizeStrategies.RowsWhileAnyIsNotBlank<ISpace>().GetSize(space);
        var byValueColumns = SizeStrategies.ColumnsWhileAnyIsNotBlank<ISpace>().GetSize(transposed);

        Assert.Equal(byValue.Height, byValueColumns.Width);
        Assert.Equal(byValue.Width, byValueColumns.Height);
      }
    }

    // --- Landmarks: separate classes, mirrored positions (SRC-61) --------------------------------------------

    [Fact]
    public void RowContaining_MirrorsColumnContaining()
    {
      Mirrored(
        RowLandmarks.RowSaying<ISpace>("o").Find,
        ColumnLandmarks.ColumnSaying<ISpace>("o").Find);

      // A miss is null on both axes rather than an empty answer, and the trimmed,
      // case-insensitive, whole-cell rule is the same rule on both — the predicate is shared, the
      // scan around it is not.
      Mirrored(
        RowLandmarks.RowSaying<ISpace>("nope").Find,
        ColumnLandmarks.ColumnSaying<ISpace>("nope").Find);

      Mirrored(
        RowLandmarks.RowSaying<ISpace>("  O  ").Find,
        ColumnLandmarks.ColumnSaying<ISpace>("  O  ").Find);

      Mirrored(
        RowLandmarks.RowSaying<ISpace>("").Find,
        ColumnLandmarks.ColumnSaying<ISpace>("").Find);
    }

    [Fact]
    public void RowWithCell_MirrorsColumnWithCell()
    {
      Mirrored(
        RowLandmarks.RowWithCell<ISpace>(cell => cell.AsText() == "h").Find,
        ColumnLandmarks.ColumnWithCell<ISpace>(cell => cell.AsText() == "h").Find);
    }

    [Fact]
    public void RowWhere_MirrorsColumnWhere()
    {
      // A space predicate again, so the cell address is transposed by hand: "the first row whose
      // leading cell is m" against "the first column whose leading cell is m".
      Mirrored(
        WithCells,
        RowLandmarks.RowWhere<ISpace>((s, row) => s[0, row].AsText() == "m").Find,
        ColumnLandmarks.ColumnWhere<ISpace>((s, column) => s[column, 0].AsText() == "m").Find);
    }

    // --- Where the mirror deliberately stops --------------------------------------------------------------

    [Fact]
    public void TheMirrorStopsAtTheDescription()
    {
      // The specific, intended break. Every law above is about a number; this is about the sentence
      // built from it, and the sentence names the axis it is talking about. A mechanized transpose
      // would have made these one string and sent the reader looking down the wrong axis.
      Assert.Equal("no row saying 'Total'", RowLandmarks.RowSaying<ISpace>("Total").Description);
      Assert.Equal("no column saying 'Total'", ColumnLandmarks.ColumnSaying<ISpace>("Total").Description);

      Assert.Equal("no row with a matching cell", RowLandmarks.RowWithCell<ISpace>(_ => false).Description);
      Assert.Equal("no column with a matching cell", ColumnLandmarks.ColumnWithCell<ISpace>(_ => false).Description);

      Assert.Equal("no matching row", RowLandmarks.RowWhere<ISpace>((_, _) => false).Description);
      Assert.Equal("no matching column", ColumnLandmarks.ColumnWhere<ISpace>((_, _) => false).Description);
    }
  }
}
