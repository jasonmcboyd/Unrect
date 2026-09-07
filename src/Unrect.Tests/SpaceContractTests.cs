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
    /// The same three-by-two grid, behind the two implementations that can be built from one.
    /// <para>
    /// The windowed one is built over a synthetic row source rather than a file: a real workbook
    /// would work too, but the point here is the contract rather than the adapter, and a fake keeps
    /// the arrangement in one screen. The third door — the eager spreadsheet reader, whose
    /// constructor is private and whose content comes from a file — keeps the same rules against a
    /// real workbook at the bottom of this class.
    /// </para>
    /// </summary>
    public static TheoryData<string> Doors => new TheoryData<string> { "grid", "windowed" };

    private static ISpace Door(string door)
    {
      var values = new[,]
      {
        { CellValue.Of("0,0"), CellValue.Of("1,0"), CellValue.Of("2,0") },
        { CellValue.Of("0,1"), CellValue.Of("1,1"), CellValue.Of("2,1") },
      };

      switch (door)
      {
        case "grid":
          return new GridSpace(values);

        default:
          var source = new FakeRowSource(new FakeSheet("Data", 2, 3));
          var pool = new ReaderPool(source, 1, warmReaders: false);

          return new WindowedSpace(new SheetStore(pool, 0, "Data", 2, 3, chunkRows: 1, windowChunks: 4));
      }
    }

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

    // --- The three spellings of "give me a subspace" --------------------------------------------------
    //
    // Two of these are extension methods rather than members of ISpace, which is why they were not
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
      ISpace streamed = new WindowedSpace(new SheetStore(pool, 0, "Empty", 10, 0, chunkRows: 100, windowChunks: 4));

      Assert.Equal(0, eager.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Width, streamed.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Height, streamed.Area.Size.Height);
      Assert.Equal(10, streamed.Area.Size.Height);

      Assert.Throws<OutOfBoundsException>(() => { _ = eager[0, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = streamed[0, 0]; });
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
      ISpace space = CapableDoor(door);

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
      ISpace streamed = new WindowedSpace(new SheetStore(pool, 0, "Empty", 10, 0, chunkRows: 100, windowChunks: 4));

      var slice = streamed.GetSubspace(new Offset(0, 2), new Area(0, 5));

      Assert.Equal(0, slice.Area.Size.Width);
      Assert.Equal(5, slice.Area.Size.Height);
    }
  }
}
