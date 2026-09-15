using System;

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

    private static ICellValues Door(string door) => ProjectionTestSpaces.Door(door);

    [Theory]
    [MemberData(nameof(Doors))]
    public void EveryDoorReadsTheSameGrid(string door)
    {
      var space = Door(door);

      Assert.Equal(3, space.Area.Size.Width);
      Assert.Equal(2, space.Area.Size.Height);
      Assert.Equal("0,0", space[0, 0].GetString());
      Assert.Equal("2,1", space[2, 1].GetString());
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
      var space = Door(door);

      Assert.Throws<OutOfBoundsException>(() => { _ = space[-1, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = space[3, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = space[0, -1]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = space[0, 2]; });
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void ASubspaceThatDoesNotFitIsABoundsConditionToo(string door)
    {
      var space = Door(door);

      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(0, 0), new Area(4, 2)));
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(0, 0), new Area(3, 3)));
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(2, 0), new Area(2, 1)));
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void ASliceIsMeasuredFromItsOwnOrigin(string door)
    {
      // A subspace is a space in its own right: its coordinates start at zero and its edges are its
      // own. Every door composes offsets the same way, or a nested declaration would read different
      // cells depending on where its data came from.
      var slice = Door(door).GetSubspace(new Offset(1, 1), new Area(2, 1));

      Assert.Equal("1,1", slice[0, 0].GetString());
      Assert.Equal("2,1", slice[1, 0].GetString());
      Assert.Throws<OutOfBoundsException>(() => { _ = slice[2, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = slice[0, 1]; });
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
    private static ICellValues CanonicalDoor(string door)
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
          return ProjectionTestSpaces.Windowed(FakeSheet.Of("Edges", EdgeRows()));

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
    public void TheIndexerAndTheCanonicalSurfaceAnswerAsOne(string door)
    {
      // The bridge, while there is one. Every canonical member is meant to be the value's own
      // property asked of the space, so a door that computed either half separately — a windowed
      // space answering IsBlank from a row it had not adapted, say — would read as two spaces at
      // once. Swept rather than sampled, because the drift such a door would produce is per cell.
      ICellValues cells = CanonicalDoor(door);

      for (var row = 0; row < cells.Area.Height; row++)
        for (var column = 0; column < cells.Area.Width; column++)
        {
          var value = cells[column, row];

          Assert.Equal(value.IsBlank, cells.IsBlank(column, row));
          Assert.Equal(value.IsText, cells.IsText(column, row));
          Assert.Equal(value.AsText(), cells.AsText(column, row));
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
      ISpace strict = GridSpace.Create(new[,] { { "  ", "kept" } }, text => string.IsNullOrWhiteSpace(text) ? CellValue.Blank : CellValue.Of(text));

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
      }
    }

    // --- The three spellings of "give me a subspace" --------------------------------------------------
    //
    // Two of these are extension methods rather than members of ICellValues, which is why they were not
    // in this class before. They belong here now because a caller cannot tell the difference: the
    // convenience overloads are part of the contract as experienced, and the rule they have to keep
    // is the same one the interface keeps — running off the edge of a space is a bounds condition.
    //
    // The offset-only form used to break that rule by accident. An oversized offset produced a
    // negative extent, and Area's own validation reported it as ArgumentOutOfRangeException — a
    // different exception from the one the two-argument form throws for the same mistake, and the
    // wrong kind besides, because ArgumentOutOfRangeException is on the engine's fault list and can
    // never be absorbed by a tolerance boundary. It went unpinned for as long as it was an accident
    // nobody had decided about. It is decided now, so it is pinned now.

    [Theory]
    [MemberData(nameof(Doors))]
    public void AnOversizedOffsetIsABoundsConditionInEverySpelling(string door)
    {
      var space = Door(door);      // three wide, two tall

      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(4, 0)));
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(0, 3)));
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(4, 0), new Area(1, 1)));
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Area(4, 1)));
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Area(1, 3)));
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void AnOffsetAtTheFarCornerIsTheEmptyRemainderRatherThanAFailure(string door)
    {
      // The boundary the check has to fall on the right side of. An offset EQUAL to the extent has
      // not run off the edge — it has arrived at it, and what is left is a real, empty space. A
      // check written with >= instead of > would turn "there is nothing after this" into an
      // exception, which is a different and much worse answer for a declaration that is asking
      // precisely that question.
      var remainder = Door(door).GetSubspace(new Offset(3, 2));

      Assert.Equal(0, remainder.Area.Size.Width);
      Assert.Equal(0, remainder.Area.Size.Height);
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void EachSpellingTakesTheSameRectangleAsTheLongOne(string door)
    {
      // The overloads are shorthand, not different operations: an offset with no area means "the
      // rest", and an area with no offset means "from the corner". Pinned beside their failure mode
      // so the pair reads as one rule rather than as two unrelated facts.
      var space = Door(door);

      var remainder = space.GetSubspace(new Offset(1, 1));

      Assert.Equal(2, remainder.Area.Size.Width);
      Assert.Equal(1, remainder.Area.Size.Height);
      Assert.Equal("1,1", remainder[0, 0].GetString());

      var corner = space.GetSubspace(new Area(2, 1));

      Assert.Equal("0,0", corner[0, 0].GetString());
      Assert.Equal("1,0", corner[1, 0].GetString());
      Assert.Throws<OutOfBoundsException>(() => { _ = corner[2, 0]; });
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

      Assert.Throws<OutOfBoundsException>(() => { _ = space[-1, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = space[space.Area.Size.Width, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = space[0, space.Area.Size.Height]; });
      Assert.Throws<OutOfBoundsException>(
        () => space.GetSubspace(new Offset(0, 0), new Area(space.Area.Size.Width + 1, 1)));

      // ...including through the convenience overloads, which is where the three spellings used to
      // disagree.
      Assert.Throws<OutOfBoundsException>(
        () => space.GetSubspace(new Offset(space.Area.Size.Width + 1, 0)));
      Assert.Throws<OutOfBoundsException>(
        () => space.GetSubspace(new Area(space.Area.Size.Width + 1, 1)));
    }

    [Fact]
    public void AZeroColumnSheetReadsTheSameThroughAWindowAsInMemory()
    {
      // A sheet with rows and no columns is a real thing — an export whose only content was deleted,
      // a worksheet holding nothing but formatting — and the two doors have to say the same about
      // it. Not "throw the same": AGREE. Zero width is a legitimate extent, so both report it, and
      // both refuse the only cell anyone could ask for.
      var eager = new GridSpace(new CellValue[10, 0]);

      var source = new FakeRowSource(new FakeSheet("Empty", 10, 0));
      var pool = new ReaderPool(source, 1, warmReaders: false);
      ICellValues streamed = new WindowedSpace(new SheetStore(pool, 0, "Empty", 10, 0, chunkRows: 100, windowChunks: 4));

      Assert.Equal(0, eager.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Width, streamed.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Height, streamed.Area.Size.Height);
      Assert.Equal(10, streamed.Area.Size.Height);

      Assert.Throws<OutOfBoundsException>(() => { _ = eager[0, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = streamed[0, 0]; });
    }

    [Fact]
    public void AZeroWidthSpaceRefusesTheCanonicalFourAtItsOwnCorner()
    {
      // Zero rows or zero columns means there is no cell (0, 0), and the canonical members have to
      // say so as loudly as the indexer does. The trap is that all three have a plausible wrong
      // answer — "it is blank", "it is not text", "it says nothing" — and each would be a claim
      // about a cell that is not there, made by a space that could not have looked.
      ISpace eager = new GridSpace(new CellValue[10, 0]);
      ISpace streamed = ProjectionTestSpaces.Windowed(new FakeSheet("Empty", 10, 0), chunkRows: 100);

      foreach (var space in new[] { eager, streamed })
      {
        Assert.Throws<OutOfBoundsException>(() => { _ = space.IsBlank(0, 0); });
        Assert.Throws<OutOfBoundsException>(() => { _ = space.IsText(0, 0); });
        Assert.Throws<OutOfBoundsException>(() => { _ = space.AsText(0, 0); });
      }
    }

    // --- The slicing law ----------------------------------------------------------------------------
    //
    // "Slicing never changes the geometry. A capable space's subspaces are capable, coordinates
    // translated; a slice may never invent capability its parent lacked nor shed what it had.
    // Forgetting is always safe (contravariance licenses it); inventing is the sin."
    //   — the projection-model spec, §5.
    //
    // It is stated as a theory over every capable backend for the same reason the bounds rules above
    // are: a declaration cannot see which door it was handed, so a law kept by one implementation and
    // not another is not a law. Two implementations are the minimum that makes the difference between
    // a law and a habit visible, and the second one (FormulaGridSpace, beside this file) is written
    // from the interface's documented obligations alone — which is all an implementor outside this
    // repository has.
    //
    // Everything here asks through Capability<T>() rather than by type test, because that is how a
    // declaration asks: a raw `space is IFormulaSpace` answers about whichever wrapper the engine
    // happens to be holding, and is documented as the wrong question.

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
    private static IFormulaSpace CapableDoor(string door)
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
    public void EverySubspaceOfACapableSpaceIsStillCapable(string door)
    {
      // Shedding is the failure this forbids. A slice that handed back a plain space would tell a
      // declaration the file has no formulas — and it would say it in the one voice a declaration
      // cannot argue with, because absence at a projection site is a fact about the cell.
      ICellValues space = CapableDoor(door);

      Assert.True(space.Area.Width >= 4 && space.Area.Height >= 10, "the samples need a 4x10 space");

      foreach (var sample in Samples)
      {
        var slice = space.GetSubspace(sample.At, sample.Of);

        Assert.NotNull(slice.Capability<IFormulaSpace>());

        // ...and a slice of a slice, and a slice of THAT: the property has to survive depth, which
        // is what a nested declaration does to a space on the way down.
        var inner = slice.GetSubspace(new Offset(0, 0), new Area(1, 1)).GetSubspace(new Offset(0, 0));

        Assert.NotNull(inner.Capability<IFormulaSpace>());
      }
    }

    [Theory]
    [MemberData(nameof(CapableDoors))]
    public void ASliceAnswersAboutItsOwnCellsAtTranslatedCoordinates(string door)
    {
      // The half that is worse than shedding when it is wrong: a slice that forwarded without
      // translating would answer about the parent's cells and look entirely plausible doing it.
      var parent = CapableDoor(door);
      var formulasSeen = 0;

      foreach (var sample in Samples)
      {
        var slice = Assert.IsAssignableFrom<IFormulaSpace>(parent.GetSubspace(sample.At, sample.Of));

        for (var row = 0; row < sample.Of.Height; row++)
          for (var column = 0; column < sample.Of.Width; column++)
          {
            var expected = parent.FormulaAt(sample.At.Width + column, sample.At.Height + row);

            Assert.Equal(expected, slice.FormulaAt(column, row));

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
      // Two offsets composed, which is where a translating wrapper that added its origin twice — or
      // handed the inner space back through ISpaceChart, which is what that interface forbids — would
      // finally show up.
      var parent = CapableDoor(door);

      var band = parent.GetSubspace(new Offset(1, 1), new Area(3, 8));      // B2:D9
      var corner = band.GetSubspace(new Offset(1, 5), new Area(2, 3));                // C7:D9

      var formulas = Assert.IsAssignableFrom<IFormulaSpace>(corner);

      Assert.Equal(parent.FormulaAt(2, 6), formulas.FormulaAt(0, 0));
      Assert.Equal(parent.FormulaAt(3, 6), formulas.FormulaAt(1, 0));
      Assert.Equal(parent.FormulaAt(3, 8), formulas.FormulaAt(1, 2));

      // The one the coordinates are actually about: D7 carries a formula and C7 does not, so a
      // slice that had translated by the wrong amount could not produce this pair.
      Assert.Null(formulas.FormulaAt(0, 0));
      Assert.NotNull(formulas.FormulaAt(1, 0));
    }

    [Theory]
    [MemberData(nameof(CapableDoors))]
    public void ACapableSliceRefusesCoordinatesOutsideItself(string door)
    {
      // A capability answers about the cells the slice addresses, so it has the slice's edges and not
      // its parent's — the same bounds contract the indexer keeps, for the same reason. Without this
      // a slice could read a formula off a cell it does not contain.
      var slice = Assert.IsAssignableFrom<IFormulaSpace>(
        CapableDoor(door).GetSubspace(new Offset(3, 1), new Area(1, 4)));

      Assert.Throws<OutOfBoundsException>(() => slice.FormulaAt(-1, 0));
      Assert.Throws<OutOfBoundsException>(() => slice.FormulaAt(1, 0));
      Assert.Throws<OutOfBoundsException>(() => slice.FormulaAt(0, -1));
      Assert.Throws<OutOfBoundsException>(() => slice.FormulaAt(0, 4));
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void NoSliceOfAnIncapableSpaceInventsACapability(string door)
    {
      // Inventing is the sin, and it is the one a convenience would commit: a slice that answered
      // FormulaAt with null over a grid built in memory would be reporting, cell by cell, that a
      // file nobody read has no formulas in it.
      var space = Door(door);

      Assert.Null(space.Capability<IFormulaSpace>());

      var slice = space.GetSubspace(new Offset(1, 0), new Area(2, 2));

      Assert.False(slice is IFormulaSpace);
      Assert.Null(slice.Capability<IFormulaSpace>());
      Assert.Null(slice.GetSubspace(new Offset(1, 1)).Capability<IFormulaSpace>());
    }

    [Fact]
    public void ThePlainSpreadsheetDoorSlicesAsIncapablyAsItReads()
    {
      // The door that COULD have carried formulas and was not asked to. Its slices must stay as
      // silent as it is, or the opt-in would be undone one subspace down.
      var space = SpreadsheetSpace.Create(
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "formulas.xlsx"),
        "Formulas");

      Assert.Null(space.Capability<IFormulaSpace>());
      Assert.Null(space.GetSubspace(new Offset(1, 1), new Area(2, 2)).Capability<IFormulaSpace>());
    }

    [Fact]
    public void AZeroColumnSheetCanStillBeSlicedByRow()
    {
      // The width is nothing, but the rows are real, so a declaration may still bound itself against
      // them. A door that rejected the slice would turn an empty sheet into an exception rather than
      // an empty answer.
      var source = new FakeRowSource(new FakeSheet("Empty", 10, 0));
      var pool = new ReaderPool(source, 1, warmReaders: false);
      ICellValues streamed = new WindowedSpace(new SheetStore(pool, 0, "Empty", 10, 0, chunkRows: 100, windowChunks: 4));

      var slice = streamed.GetSubspace(new Offset(0, 2), new Area(0, 5));

      Assert.Equal(0, slice.Area.Size.Width);
      Assert.Equal(5, slice.Area.Size.Height);
    }
  }
}
