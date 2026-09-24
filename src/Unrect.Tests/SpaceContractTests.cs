using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Spreadsheets;
using Unrect.Tests.Streaming;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// What every door into the library must agree on, asserted across all of them at once.
  /// <para>
  /// A space is where data enters, and there are three ways in: a grid built in memory, a
  /// spreadsheet read whole, and a sheet read a window at a time. Each has its own tests; this
  /// class exists for the rules that are only worth anything if <em>all</em> of them keep them,
  /// because a declaration cannot see which door it was handed.
  /// </para>
  /// </summary>
  public class SpaceContractTests
  {
    /// <summary>
    /// The same three-by-two grid, behind the two implementations that can be built from one — the
    /// doors the SHAPE rules below are stated over, which need a space whose extent the assertions
    /// can name.
    /// <para>
    /// The windowed one is built over a synthetic row source rather than a file: a real workbook
    /// would work too, but the point here is the contract rather than the adapter, and a fake keeps
    /// the arrangement in one screen. The third door — the eager spreadsheet reader, whose
    /// constructor is private and whose content comes from a file — keeps the same rules against a
    /// real workbook at the bottom of this class.
    /// </para>
    /// <para>
    /// The arrangement itself lives in <see cref="ProjectionTestSpaces.Door"/>, because
    /// <see cref="PlaneTests"/> states its own laws over the same three and two copies of a door
    /// list are two things to keep in step.
    /// </para>
    /// </summary>
    public static TheoryData<string> Doors => new TheoryData<string> { "grid", "windowed" };

    private static ICellSpace Door(string door) => ProjectionTestSpaces.Door(door);

    [Theory]
    [MemberData(nameof(Doors))]
    public void EveryDoorReadsTheSameGrid(string door)
    {
      var space = Door(door);

      Assert.Equal(3, space.Area.Size.Width);
      Assert.Equal(2, space.Area.Size.Height);
      Assert.Equal("0,0", space.AsText(0, 0));
      Assert.Equal("2,1", space.AsText(2, 1));
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void ReadingPastTheEdgeOfAnySpaceIsABoundsCondition(string door)
    {
      // The rule that has to hold at every door, because the layer above cannot see which one it
      // was handed. Running off the edge of a space is a statement about the DATA — it is how a
      // declaration discovers it has run out of room, and how a Repeat stops — so it must arrive as
      // OutOfBoundsException, which a tolerance boundary may absorb.
      //
      // IndexOutOfRangeException is on the engine's fault list, where it means a bug in the reading
      // code and is never absorbed. A space that threw one for an ordinary overrun would make every
      // overrun unrecoverable, and it would do so only for declarations that happened to be pointed
      // at that door. This theory is what stops one implementation drifting away from the others.
      //
      // Both ways of running off the edge are here, because a declaration meets both: the plane
      // refuses to NAME a cell outside the region it was handed, and the space refuses to READ one
      // outside itself. The second is the door's own obligation and the reason this is a theory.
      var space = Door(door);
      var plane = Plane<ICellSpace>.Of(space);

      foreach (var (column, row) in new[] { (-1, 0), (3, 0), (0, -1), (0, 2) })
      {
        Assert.Throws<OutOfBoundsException>(() => { _ = plane[column, row]; });
        Assert.Throws<OutOfBoundsException>(() => { _ = space.IsBlank(column, row); });
        Assert.Throws<OutOfBoundsException>(() => { _ = space.AsText(column, row); });
      }
    }

    // --- The canonical surface ------------------------------------------------------------------------
    //
    // The questions anything may ask of a grid without knowing what is behind it: is this cell
    // empty, does it say a word of its own, what does it say. They are stated across every door for
    // the reason the bounds rules are — a declaration cannot see which door it was handed — and the
    // eager reader joins the theory here, because the laws are about content and it has some.

    /// <summary>
    /// Every door, each holding the <em>same awkward layout</em>: one cell of every kind, five
    /// error codes, whitespace that looks empty, and cells that are not there at all. The two
    /// spreadsheet doors are the same file read twice, plainly and with formulas asked for, because
    /// asking for formulas hands back a different implementation of this very contract.
    /// <para>
    /// The layout matters as much as the door list does. Stated over a grid of labels these laws
    /// are vacuous — <c>IsText</c> is true everywhere, <c>AsText</c> is never a rendering, and a
    /// door that had stopped classifying entirely would pass — so every sweep below counts what it
    /// saw and refuses to be vacuous.
    /// </para>
    /// </summary>
    public static TheoryData<string> CanonicalDoors
      => new TheoryData<string> { "grid", "windowed", "xlsx", "xlsx-formulas" };

    /// <summary>
    /// The "Edges" layout, five columns by four rows, as <c>edge-cases.xlsx</c> holds it:
    /// <code>
    ///        0            1          2         3            4
    ///   0    "text"       42         3.14      2026-01-15   TRUE
    ///   1    #VALUE!      #DIV/0!    #N/A      #REF!        #NAME?
    ///   2    "  "         " "        ""        (no cell)    "x"
    ///   3    #NULL!       #NUM!      (none)    (none)       7
    /// </code>
    /// The doors do not agree about row 2, deliberately: the spreadsheet adapter's default
    /// blankness rule calls whitespace-only text empty and the array adapter does not. Blankness is
    /// the adapter's decision, so that is a difference between two configurations rather than a
    /// disagreement about the contract — which is why every law below is stated <em>within</em> a
    /// door and the cross-door theory names only the cells every adapter reads the same way.
    /// </summary>
    private static object?[][] EdgeRows() => new[]
    {
      new object?[] { "text", 42, 3.14, new DateTime(2026, 1, 15), true },
      new object?[]
      {
        CellValue.OfError(CellError.Value),
        CellValue.OfError(CellError.DivisionByZero),
        CellValue.OfError(CellError.NotAvailable),
        CellValue.OfError(CellError.Reference),
        CellValue.OfError(CellError.Name),
      },
      new object?[] { "  ", " ", "", null, "x" },
      new object?[] { CellValue.OfError(CellError.Null), CellValue.OfError(CellError.Number), null, null, 7 },
    };

    /// <summary>Every door as the canonical surface alone, over <see cref="EdgeRows"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="door"/> names no door.</exception>
    private static ICellSpace CanonicalDoor(string door)
    {
      var file = System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "edge-cases.xlsx");

      switch (door)
      {
        case "grid":
          var rows = EdgeRows();
          var cells = new object?[rows.Length, rows[0].Length];

          for (var row = 0; row < rows.Length; row++)
            for (var column = 0; column < rows[row].Length; column++)
              cells[row, column] = rows[row][column];

          return ProjectionTestSpaces.Mixed(cells);

        case "windowed":
          return ProjectionTestSpaces.Streamed(FakeSheet.Of("Edges", EdgeRows()));

        case "xlsx":
          return SpreadsheetSpace.Create(file, "Edges");

        // The same file through the door that hands back a formula-carrying implementation: a
        // second class answering this contract, which is the only reason it is swept separately.
        case "xlsx-formulas":
          return SpreadsheetSpace.CreateWithFormulas(file, "Edges");

        default:
          throw new ArgumentOutOfRangeException(nameof(door), door, "No such door.");
      }
    }

    [Theory]
    [MemberData(nameof(CanonicalDoors))]
    public void ACellSaysNothingExactlyWhenItIsBlank(string door)
    {
      // The equivalence the whole canonical surface rests on, swept over every cell of every door.
      // A door that rendered a blank as "" would make "is there anything here" a question with two
      // different answers, and every skip-while-blank strategy would read differently through it.
      var cells = CanonicalDoor(door);

      var blank = 0;
      var text = 0;
      var rendered = 0;

      for (var row = 0; row < cells.Area.Height; row++)
        for (var column = 0; column < cells.Area.Width; column++)
        {
          Assert.Equal(cells.IsBlank(column, row), cells.AsText(column, row) is null);

          if (cells.IsBlank(column, row))
            blank++;
          else if (cells.IsText(column, row))
            text++;
          else
            rendered++;
        }

      // Non-vacuity, and the reason this layout is the one the sweep runs over: an equivalence
      // between two properties is worth nothing until the sample contains cells on both sides of
      // it, and a rendering cell — one that says something without being text — is the case a grid
      // of labels cannot produce at all.
      Assert.True(blank > 0, $"the '{door}' door held no blank cell");
      Assert.True(text > 0, $"the '{door}' door held no text cell");
      Assert.True(rendered > 0, $"the '{door}' door held no cell that renders without being text");
    }

    [Theory]
    [MemberData(nameof(CanonicalDoors))]
    public void OnlyACellWithAValueCanBeText(string door)
    {
      var cells = CanonicalDoor(door);
      var text = 0;

      for (var row = 0; row < cells.Area.Height; row++)
        for (var column = 0; column < cells.Area.Width; column++)
          if (cells.IsText(column, row))
          {
            Assert.False(cells.IsBlank(column, row));
            text++;
          }

      Assert.True(text > 0, $"the '{door}' door held no text cell");
    }

    [Theory]
    [MemberData(nameof(CanonicalDoors))]
    public void APointAnswersExactlyWhatItsSpaceDoes(string door)
    {
      // A point is an address and not a value: every question asked of it is the space's own answer
      // at those coordinates. A door whose point read differently from its space — or a point that
      // cached what it was minted from — would read as two spaces at once. Swept rather than
      // sampled, because the drift such a door would produce is per cell.
      ICellSpace cells = CanonicalDoor(door);
      var plane = Plane<ICellSpace>.Of(cells);

      for (var row = 0; row < cells.Area.Height; row++)
        for (var column = 0; column < cells.Area.Width; column++)
        {
          var point = plane[column, row];

          Assert.Equal(point.IsBlank(), cells.IsBlank(column, row));
          Assert.Equal(point.HasValue(), !cells.IsBlank(column, row));
          Assert.Equal(point.IsText(), cells.IsText(column, row));
          Assert.Equal(point.AsText(), cells.AsText(column, row));
        }
    }

    /// <summary>
    /// The cells every adapter in the suite reads the same way, with what each of them says. Row 2
    /// is absent on purpose — whitespace is where the doors are configured differently, and that is
    /// blankness policy rather than contract.
    /// </summary>
    public static TheoryData<string, int, int, string> RenderedCells()
    {
      var cases = new TheoryData<string, int, int, string>();

      foreach (var door in new[] { "grid", "windowed", "xlsx", "xlsx-formulas" })
        foreach (var (column, row, said) in new[]
        {
          (1, 0, "42"),
          (2, 0, "3.14"),
          (3, 0, "2026-01-15"),
          (4, 0, "TRUE"),
          (0, 1, "#VALUE!"),
          (1, 1, "#DIV/0!"),
          (2, 1, "#N/A"),
          (3, 1, "#REF!"),
          (4, 1, "#NAME?"),
          (0, 3, "#NULL!"),
          (1, 3, "#NUM!"),
          (4, 3, "7"),
        })
          cases.Add(door, column, row, said);

      return cases;
    }

    [Theory]
    [MemberData(nameof(RenderedCells))]
    public void TextIsTheOneKindWhoseTextIsItsOwnValue(string door, int column, int row, string said)
    {
      // The sweeps above cannot say this: they check that the classification is consistent, not
      // that it classifies. Every other kind HAS a canonical text and none of them is text, which
      // is exactly the difference a matcher reads through — and it has to be the same rendering at
      // every door, or a declaration anchored on one would stop anchoring through another.
      var cells = CanonicalDoor(door);

      Assert.False(cells.IsBlank(column, row));
      Assert.False(cells.IsText(column, row));
      Assert.Equal(said, cells.AsText(column, row));
    }

    [Theory]
    [MemberData(nameof(CanonicalDoors))]
    public void ATextCellIsTheOnlyOneThatAnswersIsText(string door)
    {
      // The positive half of the theory above, at the one cell every door agrees is text.
      var cells = CanonicalDoor(door);

      Assert.True(cells.IsText(0, 0));
      Assert.Equal("text", cells.AsText(0, 0));
    }

    [Fact]
    public void ACellCalledBlankIsNotTextEvenWhenItHoldsAString()
    {
      // The defining case, and the reason IsText is not a type test: it means "has a value and it
      // is text". Blankness is decided by the adapter, so a string payload the adapter or a
      // predicate calls empty is a cell with NO value — it says nothing, it is not text, and a
      // matcher must not find it by the characters it happens to be made of.
      ISpace empties = GridSpace.Create(new string?[,] { { "", "kept" } });

      Assert.True(empties.IsBlank(0, 0));
      Assert.False(empties.IsText(0, 0));
      Assert.Null(empties.AsText(0, 0));

      // The same string under a rule that says whitespace is empty space, which is the spreadsheet
      // adapter's default and the case a real export produces by the thousand.
      ISpace strict = GridSpace.Create(
        new[,] { { "  ", "kept" } },
        isBlank: text => string.IsNullOrWhiteSpace(text),
        isText: _ => true,
        asText: text => text);

      Assert.True(strict.IsBlank(0, 0));
      Assert.False(strict.IsText(0, 0));
      Assert.Null(strict.AsText(0, 0));

      // ...and under the array adapter's own default, where only null and "" are empty, the very
      // same two spaces are a cell with a value, and that value is text.
      ISpace kept = GridSpace.Create(new string?[,] { { "  ", "kept" } });

      Assert.False(kept.IsBlank(0, 0));
      Assert.True(kept.IsText(0, 0));
      Assert.Equal("  ", kept.AsText(0, 0));
    }

    // --- The kind question ---------------------------------------------------------------------------
    //
    // The one read a sheet answers for every cell. The six kinded reads assert a kind and refuse a
    // cell that disagrees; this one asks, which is what a predicate needs before it decides
    // anything — so its whole contract is that it answers, always, and agrees with the three
    // questions that were already being asked about the same cell.

    [Theory]
    [MemberData(nameof(CanonicalDoors))]
    public void EveryCellHasAKindAndTheKindAgreesWithWhatTheDoorAlreadySaid(string door)
    {
      // Three equivalences, swept. Blank and Text are the canonical surface's own words for two of
      // the kinds, and Error is the sheet vocabulary's — a door whose kind disagreed with any of
      // them would give a predicate and a leaf two different pictures of the same cell, and the
      // leaf's failure message would name a kind the predicate had already ruled out.
      var cells = CanonicalDoor(door);
      var seen = new HashSet<CellKind>();

      for (var row = 0; row < cells.Area.Height; row++)
        for (var column = 0; column < cells.Area.Width; column++)
        {
          var kind = cells.ValueAt(column, row).Kind;

          Assert.Equal(kind == CellKind.Blank, cells.IsBlank(column, row));
          Assert.Equal(kind == CellKind.Text, cells.IsText(column, row));
          Assert.Equal(kind == CellKind.Error, cells.ValueAt(column, row).Kind == CellKind.Error);

          seen.Add(kind);
        }

      // Non-vacuity, and the strongest form of "it fails for none": this layout holds a cell of
      // every kind there is, so a door that threw on one — or classified a kind it had no word for
      // as something else — has nowhere to hide.
      Assert.Equal(
        new HashSet<CellKind> { CellKind.Blank, CellKind.Text, CellKind.Number, CellKind.Temporal, CellKind.Boolean, CellKind.Error },
        seen);
    }

    [Theory]
    [MemberData(nameof(CanonicalDoors))]
    public void EveryCanonicalMemberRefusesACoordinateOutsideTheSpace(string door)
    {
      // The indexer's rule, extended to the three questions that do not go through it. A member
      // that answered for a cell outside the space would let a strategy walk off the edge of a
      // subspace and read its parent's data without ever hearing about it.
      var cells = CanonicalDoor(door);

      foreach (var (column, row) in new[] { (-1, 0), (cells.Area.Width, 0), (0, -1), (0, cells.Area.Height) })
      {
        Assert.Throws<OutOfBoundsException>(() => { _ = cells.IsBlank(column, row); });
        Assert.Throws<OutOfBoundsException>(() => { _ = cells.IsText(column, row); });
        Assert.Throws<OutOfBoundsException>(() => { _ = cells.AsText(column, row); });

        // Every read answers for every cell IN the space and for no coordinate outside it: a
        // refusal is about what a cell holds, never about addresses, so a coordinate off the edge
        // is the same bounds condition here as everywhere else.
        Assert.Throws<OutOfBoundsException>(() => cells.ValueAt(column, row));
      }
    }

    // --- Degenerate extents -------------------------------------------------------------------------

    [Fact]
    public void TheEagerSpreadsheetDoorKeepsTheSameBoundsContract()
    {
      // The third implementation, reached through a file because that is the only way this door can
      // be entered. What comes back today is the GridSpace the reader filled — the delegation shell
      // that used to sit in front of it retired when the eager door became a factory — so this fact
      // is now a statement about the door's PRODUCT rather than about a wrapper's forwarding. It
      // stays because the door is free to hand back something else tomorrow (it already does when
      // formulas are asked for, and that space is a wrapper), and this is where such a change would
      // have to keep the bounds contract.
      var space = SpreadsheetSpace.Create(
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "simple-report.xlsx"),
        "Report");

      var plane = Plane<ICellSpace>.Of(space);

      Assert.Throws<OutOfBoundsException>(() => { _ = space.IsBlank(-1, 0); });
      Assert.Throws<OutOfBoundsException>(() => { _ = space.AsText(space.Area.Size.Width, 0); });
      Assert.Throws<OutOfBoundsException>(() => { _ = space.IsText(0, space.Area.Size.Height); });
      Assert.Throws<OutOfBoundsException>(() => { _ = plane[-1, 0]; });
      Assert.Throws<OutOfBoundsException>(
        () => plane.Slice(new Offset(0, 0), new Area(space.Area.Size.Width + 1, 1)));

      // ...including through the convenience overloads, which is where the three spellings used to
      // disagree.
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(space.Area.Size.Width + 1, 0)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Area(space.Area.Size.Width + 1, 1)));
    }

    [Fact]
    public void AZeroColumnSheetReadsTheSameThroughAStreamAsInMemory()
    {
      // A sheet with rows and no columns is a real thing — an export whose only content was deleted,
      // a worksheet holding nothing but formatting — and the two doors have to say the same about
      // it. Not "throw the same": AGREE. Zero width is a legitimate extent, so both report it, and
      // both refuse the only cell anyone could ask for.
      var eager = SheetGrid.Of(new CellValue[10, 0]);

      using var book = Workbook.Over(new FakeRowSource(new FakeSheet("Empty", 10, 0)), new WorkbookOptions());
      var streamed = book.Sheet("Empty");

      Assert.Equal(0, eager.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Width, streamed.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Height, streamed.Area.Size.Height);
      Assert.Equal(10, streamed.Area.Size.Height);

      Assert.Throws<OutOfBoundsException>(() => { _ = Plane<ICellSpace>.Of(eager)[0, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = Plane<ICellSpace>.Of(streamed)[0, 0]; });
    }

    [Fact]
    public void AZeroWidthSpaceRefusesTheCanonicalFourAtItsOwnCorner()
    {
      // Zero rows or zero columns means there is no cell (0, 0), and the canonical members have to
      // say so as loudly as the indexer does. The trap is that all three have a plausible wrong
      // answer — "it is blank", "it is not text", "it says nothing" — and each would be a claim
      // about a cell that is not there, made by a space that could not have looked.
      ISpace eager = SheetGrid.Of(new CellValue[10, 0]);
      ISpace streamed = ProjectionTestSpaces.Streamed(new FakeSheet("Empty", 10, 0));

      foreach (var space in new[] { eager, streamed })
      {
        Assert.Throws<OutOfBoundsException>(() => { _ = space.IsBlank(0, 0); });
        Assert.Throws<OutOfBoundsException>(() => { _ = space.IsText(0, 0); });
        Assert.Throws<OutOfBoundsException>(() => { _ = space.AsText(0, 0); });
      }
    }

    // --- The slicing law ----------------------------------------------------------------------------
    //
    // "Slicing never changes the geometry": a region of a capable space answers about that space's
    // own cells at translated coordinates, and about no others.
    //
    // The half of the old law about capability — that a subspace may neither shed what its parent had
    // nor invent what it lacked — is no longer something a backend can get wrong. A slice is a plane,
    // a plane names the space it was cut from, and a read through it is that space's read: there is
    // no wrapper left to forget or to fabricate, and the capability a declaration may use is the one
    // its own type names. What remains a law, and remains worth asserting at two independent
    // backends, is the arithmetic: which cell a sliced region's coordinates land on.
    //
    // It is stated as a theory over every capable backend for the same reason the bounds rules above
    // are: a declaration cannot see which door it was handed, so a law kept by one implementation and
    // not another is not a law. Two implementations are the minimum that makes the difference between
    // a law and a habit visible, and the second one (FormulaGridSpace, beside this file) is written
    // from the interface's documented obligations alone — which is all an implementor outside this
    // repository has.

    /// <summary>The doors that carry formulas: the eager reader over the fixture, and the double.</summary>
    public static TheoryData<string> CapableDoors => new TheoryData<string> { "xlsx", "double" };

    /// <summary>
    /// The rectangles the law is sampled at: the whole space, a column band, a row band that spans
    /// two unrelated formula groups, and an off-origin corner. Every one of them holds at least one
    /// formula in both doors, which is what the non-vacuity guard below insists on — a theory that
    /// compared null to null everywhere would pass over a space that had lost its formulas entirely.
    /// </summary>
    private static readonly (Offset At, Area Of)[] Samples =
    {
      (new Offset(0, 0), new Area(4, 10)),
      (new Offset(3, 1), new Area(1, 4)),
      (new Offset(1, 6), new Area(3, 3)),
      (new Offset(1, 1), new Area(3, 8)),
    };

    /// <summary>
    /// A capable space, at least 4x10 so every sample fits and both doors are sampled at the same
    /// coordinates. Both put formulas in the same places (a shared column at D2:D5, a total at D7, a
    /// column-shifted group across B9:D9) so that one sample list can be non-vacuous for both; the
    /// TEXT of the formulas is deliberately unalike, because nothing in the law is about the text.
    /// </summary>
    private static ISpreadsheetSpace CapableDoor(string door)
    {
      if (door == "xlsx")
        return SpreadsheetSpace.CreateWithFormulas(
          System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "formulas.xlsx"),
          "Formulas");

      var values = new CellValue[10, 4];
      var formulas = new string?[10, 4];

      for (var row = 0; row < 10; row++)
        for (var column = 0; column < 4; column++)
          values[row, column] = CellValue.Of($"{column},{row}");

      for (var row = 1; row <= 4; row++)
        formulas[row, 3] = $"B{row + 1}*C{row + 1}";

      formulas[6, 3] = "SUM(D2:D5)";
      formulas[8, 1] = "LOG10(B8)+B8";
      formulas[8, 2] = "LOG10(C8)+C8";
      formulas[8, 3] = "LOG10(D8)+D8";

      return new FormulaGridSpace(values, formulas);
    }

    [Theory]
    [MemberData(nameof(CapableDoors))]
    public void ASliceAnswersAboutItsOwnCellsAtTranslatedCoordinates(string door)
    {
      // The failure this forbids: a region that named its cells in the wrong frame would answer
      // about the parent's cells and look entirely plausible doing it. Swept cell by cell over four
      // rectangles, because that is the granularity at which a frame error shows.
      var parent = CapableDoor(door);
      var whole = Plane<ISpreadsheetSpace>.Of(parent);
      var formulasSeen = 0;

      Assert.True(parent.Area.Width >= 4 && parent.Area.Height >= 10, "the samples need a 4x10 space");

      foreach (var sample in Samples)
      {
        var slice = whole.Slice(sample.At, sample.Of);

        for (var row = 0; row < sample.Of.Height; row++)
          for (var column = 0; column < sample.Of.Width; column++)
          {
            var expected = parent.FormulaAt(sample.At.Width + column, sample.At.Height + row);

            var cell = slice[column, row];

            Assert.Equal(expected, cell.Space.FormulaAt(cell.Column, cell.Row));

            if (expected is not null)
              formulasSeen++;
          }
      }

      // Non-vacuity: comparing null to null proves nothing, so the samples have to have found some.
      Assert.True(formulasSeen > 0, $"the samples found no formulas at all in the '{door}' door");
    }

    [Theory]
    [MemberData(nameof(CapableDoors))]
    public void ANestedSliceTranslatesOnceForEachSlice(string door)
    {
      // Two offsets composed, which is where a locator that added its origin twice — or dropped one
      // of them — would finally show up.
      var parent = CapableDoor(door);

      var band = Plane<ISpreadsheetSpace>.Of(parent).Slice(new Offset(1, 1), new Area(3, 8));   // B2:D9
      var corner = band.Slice(new Offset(1, 5), new Area(2, 3));                                // C7:D9

      Assert.Equal(parent.FormulaAt(2, 6), Formula(corner[0, 0]));
      Assert.Equal(parent.FormulaAt(3, 6), Formula(corner[1, 0]));
      Assert.Equal(parent.FormulaAt(3, 8), Formula(corner[1, 2]));

      // The one the coordinates are actually about: D7 carries a formula and C7 does not, so a
      // region that had translated by the wrong amount could not produce this pair.
      Assert.Null(Formula(corner[0, 0]));
      Assert.NotNull(Formula(corner[1, 0]));
    }

    [Theory]
    [MemberData(nameof(CapableDoors))]
    public void ACapableRegionRefusesCoordinatesOutsideItself(string door)
    {
      // A region addresses the cells it names and no others, so it has its own edges and not its
      // space's — the same bounds contract every read keeps, for the same reason. Without this a
      // declaration could reach a formula off a cell its region does not contain.
      var slice = Plane<ISpreadsheetSpace>.Of(CapableDoor(door)).Slice(new Offset(3, 1), new Area(1, 4));

      Assert.Throws<OutOfBoundsException>(() => { _ = slice[-1, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = slice[1, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = slice[0, -1]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = slice[0, 4]; });

      // ...and the space refuses its own edge in the same voice, which is the half a region cannot
      // do for it.
      Assert.Throws<OutOfBoundsException>(() => CapableDoor(door).FormulaAt(-1, 0));
    }

    [Fact]
    public void ThePlainSpreadsheetDoorReadsAsIncapablyAsItWasAsked()
    {
      // The door that COULD have carried formulas and was not asked to. Honest absence: a reader
      // that skipped formulas must not answer about them at all, because a null there would say the
      // file has none — cell by cell, about a file nobody read that way.
      var space = SpreadsheetSpace.Create(
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "formulas.xlsx"),
        "Formulas");

      Assert.False(space is IFormulaSpace);

      // ...and the same file through the door that WAS asked, so the fact above is about the asking
      // and not about the file.
      Assert.True(
        SpreadsheetSpace.CreateWithFormulas(
          System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "formulas.xlsx"),
          "Formulas") is IFormulaSpace);
    }

    [Fact]
    public void AZeroColumnSheetCanStillBeSlicedByRow()
    {
      // The width is nothing, but the rows are real, so a declaration may still bound itself against
      // them. A locator that rejected the slice would turn an empty sheet into an exception rather
      // than an empty answer.
      using var book = Workbook.Over(new FakeRowSource(new FakeSheet("Empty", 10, 0)), new WorkbookOptions());
      var streamed = book.Sheet("Empty");

      var slice = Plane<ICellSpace>.Of(streamed).Slice(new Offset(0, 2), new Area(0, 5));

      Assert.Equal(0, slice.Area.Size.Width);
      Assert.Equal(5, slice.Area.Size.Height);
    }

    /// <summary>The formula behind the cell a point addresses, read through the point's own space.</summary>
    private static string? Formula(Point<ISpreadsheetSpace> point) => point.Space.FormulaAt(point.Column, point.Row);
  }
}
