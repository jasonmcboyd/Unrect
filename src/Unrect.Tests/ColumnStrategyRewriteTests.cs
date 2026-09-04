using System;

using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests
{
  /// <summary>
  /// The two content-driven column strategies used to be column-major: for each column, scan down
  /// every row looking for a match. That reads the full height of a column to decide a width, which
  /// against a bound discovered one row at a time resolves the whole bound before anything has
  /// consumed it — and a table's default placement measures its columns, so the commonest
  /// declaration in the library would have forced at placement time. They are now read row-major
  /// with an early exit.
  /// <para>
  /// A rewrite is only safe if the answer did not move, and the old implementation is gone, so the
  /// oracle here is the DENOTATION rather than the old code: the sentence each strategy's
  /// documentation states, written out column-major and compared against the strategy over a matrix
  /// of grids. The oracle is itself anchored by a table of literal answers, because two agreeing
  /// computations can agree on the same mistake.
  /// </para>
  /// <para>
  /// The second half of the class is the early exit, which is a claim about cells that were NOT
  /// read and so cannot be made by looking at the answer.
  /// </para>
  /// </summary>
  public class ColumnStrategyRewriteTests
  {
    private static Func<CellValue, bool> Predicate(string name) => name switch
    {
      "has-value" => value => value.HasValue,
      "blank" => value => value.IsBlank,

      // Neither total nor the negation of the others, so a strategy that quietly substituted one
      // predicate for another — the shape of the inversion bug this family has a history of — has
      // nowhere to hide.
      "even" => value => value.TryGetInt() % 2 == 0,

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such predicate."),
    };

    private static ISpace Space(string name) => name switch
    {
      "dense" => Grid(new[,]
      {
        { 1, 2, 3 },
        { 4, 5, 6 },
        { 7, 8, 9 },
      }),

      // A staircase: every column has a value somewhere, but only the first has one in every row.
      "ragged" => Grid(new[,]
      {
        { 1, 2, 3, 4 },
        { 5, 6, 0, 0 },
        { 7, 0, 0, 0 },
      }),

      // Column 3 is empty in every row, so both readings stop before it and the two columns beyond
      // it are unreachable however full they are.
      "hole" => Grid(new[,]
      {
        { 1, 2, 3, 0, 5 },
        { 6, 7, 8, 0, 10 },
      }),

      // The same hole in one row only: where "any" and "all" have to disagree.
      "partial-hole" => Grid(new[,]
      {
        { 1, 2, 3, 4, 5 },
        { 6, 7, 8, 0, 10 },
      }),

      "all-blank" => Grid(new[,]
      {
        { 0, 0, 0 },
        { 0, 0, 0 },
      }),

      "one-row" => Grid(new[,] { { 1, 2, 0, 4 } }),

      "one-column" => Grid(new[,] { { 1 }, { 0 }, { 3 } }),

      // Three columns and no rows: the degenerate case where the two readings part company, because
      // "some row matches" is false of an empty column and "every row matches" is true of one.
      "no-rows" => Grid(new int[0, 3]),

      "no-columns" => Grid(new int[2, 0]),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such grid."),
    };

    private static readonly string[] Grids =
    {
      "dense", "ragged", "hole", "partial-hole", "all-blank", "one-row", "one-column", "no-rows", "no-columns",
    };

    private static readonly string[] Predicates = { "has-value", "blank", "even" };

    public static TheoryData<string, string> Cases
    {
      get
      {
        var cases = new TheoryData<string, string>();

        foreach (var grid in Grids)
          foreach (var predicate in Predicates)
            cases.Add(grid, predicate);

        return cases;
      }
    }

    // --- The oracles: each strategy's documented sentence, read the way it used to be computed -----

    /// <summary>
    /// Column <c>c</c> is included when at least one of its cells satisfies the predicate, and
    /// columns are taken while that holds contiguously from 0.
    /// </summary>
    private static int LeadingColumnsWhereSomeRowMatches(ISpace space, Func<CellValue, bool> predicate)
    {
      for (var column = 0; column < space.Area.Width; column++)
      {
        var matched = false;

        for (var row = 0; row < space.Area.Height; row++)
          matched |= predicate(space[column, row]);

        if (!matched)
          return column;
      }

      return space.Area.Width;
    }

    /// <summary>
    /// Column <c>c</c> is included when every one of its cells satisfies the predicate, and columns
    /// are taken while that holds contiguously from 0.
    /// </summary>
    private static int LeadingColumnsWhereEveryRowMatches(ISpace space, Func<CellValue, bool> predicate)
    {
      for (var column = 0; column < space.Area.Width; column++)
      {
        for (var row = 0; row < space.Area.Height; row++)
        {
          if (!predicate(space[column, row]))
            return column;
        }
      }

      return space.Area.Width;
    }

    // --- The rewrite agrees with the denotation ----------------------------------------------------

    [Theory]
    [MemberData(nameof(Cases))]
    public void TakeColumnsWhileAny_CountsTheLeadingColumnsSomeRowMatches(string grid, string predicate)
    {
      var space = Space(grid);
      var rule = Predicate(predicate);

      Assert.Equal(
        LeadingColumnsWhereSomeRowMatches(space, rule),
        ColumnStrategies.TakeColumnsWhileAny(rule).SelectColumns(space));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TakeColumnsWhileAll_CountsTheLeadingColumnsEveryRowMatches(string grid, string predicate)
    {
      var space = Space(grid);
      var rule = Predicate(predicate);

      Assert.Equal(
        LeadingColumnsWhereEveryRowMatches(space, rule),
        ColumnStrategies.TakeColumnsWhileAll(rule).SelectColumns(space));
    }

    [Theory]
    [InlineData("dense")]
    [InlineData("ragged")]
    [InlineData("hole")]
    [InlineData("partial-hole")]
    [InlineData("all-blank")]
    [InlineData("one-row")]
    [InlineData("one-column")]
    [InlineData("no-rows")]
    [InlineData("no-columns")]
    public void TakeColumnsWhileAnyValue_IsTakeColumnsWhileAnyOfHasValue(string grid)
    {
      // The convenience keeps no separate reading of the grid — the whole of it is the predicate.
      var space = Space(grid);

      Assert.Equal(
        ColumnStrategies.TakeColumnsWhileAny(value => value.HasValue).SelectColumns(space),
        ColumnStrategies.TakeColumnsWhileAnyValue().SelectColumns(space));
    }

    [Theory]
    [InlineData("dense", 3, 3)]
    [InlineData("ragged", 4, 1)]
    [InlineData("hole", 3, 3)]
    [InlineData("partial-hole", 5, 3)]
    [InlineData("all-blank", 0, 0)]
    [InlineData("one-row", 2, 2)]
    [InlineData("one-column", 1, 0)]
    [InlineData("no-rows", 0, 3)]
    [InlineData("no-columns", 0, 0)]
    public void TheAnswersThemselves(string grid, int any, int all)
    {
      // What anchors the oracle. Every case above compares two computations of the same sentence,
      // which would agree just as happily if the sentence were wrong; these are the answers written
      // out by hand. The pair worth reading twice is "no-rows": with nothing to look at, no column
      // has a matching cell and every column matches vacuously, so the two readings land at opposite
      // ends of the width. That asymmetry is the denotation, not an accident of either loop.
      var space = Space(grid);

      Assert.Equal(any, ColumnStrategies.TakeColumnsWhileAny(value => value.HasValue).SelectColumns(space));
      Assert.Equal(all, ColumnStrategies.TakeColumnsWhileAll(value => value.HasValue).SelectColumns(space));
    }

    // --- The early exit: the point of the rewrite --------------------------------------------------

    [Fact]
    public void OnDenseData_TheAnyReadingSettlesAfterOneRow()
    {
      // The property the rewrite exists for, and the reason a table's width is affordable against a
      // bound that has not been resolved. Once the leading run of matched columns reaches the full
      // width no later row can extend it, so a grid whose first row is full costs exactly its width
      // in reads — one row — however tall it is.
      var space = new CountingSpace(CoordinateGrid(width: 4, height: 50));

      Assert.Equal(4, ColumnStrategies.TakeColumnsWhileAnyValue().SelectColumns(space));

      Assert.Equal(4, space.CellReads);
      Assert.Equal(1, space.RowsTouched);
    }

    [Fact]
    public void TheAnyReadingReadsASecondRowOnlyForColumnsTheFirstDidNotSettle()
    {
      // Two rows are needed here because neither alone claims both columns. What the walk does not
      // do is read column 0 again: it resumes from the leading run, so a column already inside the
      // answer is never revisited.
      var space = new CountingSpace(Grid(new[,]
      {
        { 1, 0 },
        { 0, 2 },
      }));

      Assert.Equal(2, ColumnStrategies.TakeColumnsWhileAnyValue().SelectColumns(space));

      Assert.Equal(3, space.CellReads);   // both of row 0, then column 1 of row 1
      Assert.Equal(2, space.RowsTouched);
    }

    [Fact]
    public void WhenTheFirstRowFailsAtColumnZero_TheAllReadingStopsThere()
    {
      // The dual early exit. The "all" answer starts at the full width and only ever falls, so it is
      // settled the moment it reaches zero — one cell read, one row touched, and the full rows below
      // it never looked at.
      var space = new CountingSpace(Grid(new[,]
      {
        { 0, 0, 0, 0 },
        { 1, 2, 3, 4 },
        { 5, 6, 7, 8 },
      }));

      Assert.Equal(0, ColumnStrategies.TakeColumnsWhileAll(value => value.HasValue).SelectColumns(space));

      Assert.Equal(1, space.CellReads);
      Assert.Equal(1, space.RowsTouched);
    }

    [Fact]
    public void TheAllReadingNeverReadsAColumnItHasAlreadyRuledOut()
    {
      // A failing cell in column c rules out c and everything after it, and no later row can bring
      // one back — so the rows below shrink to the columns still in play. Four cells for row 0, two
      // for row 1 (where column 1 fails), then one apiece: eight reads, against the sixteen a walk
      // that ignored its own answer would take.
      var space = new CountingSpace(Grid(new[,]
      {
        { 1, 2, 3, 4 },
        { 5, 0, 7, 8 },
        { 9, 10, 11, 12 },
        { 13, 14, 15, 16 },
      }));

      Assert.Equal(1, ColumnStrategies.TakeColumnsWhileAll(value => value.HasValue).SelectColumns(space));

      Assert.Equal(8, space.CellReads);
      Assert.Equal(4, space.RowsTouched);
    }
  }
}
