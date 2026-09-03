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

    // --- Degenerate extents -------------------------------------------------------------------------

    [Fact]
    public void TheEagerSpreadsheetDoorKeepsTheSameBoundsContract()
    {
      // The third implementation, which has to be reached through a file because that is the only
      // way it can be built. Same rule, stated against a real workbook so the delegation the eager
      // reader is made of cannot quietly swallow it.
      var space = SpreadsheetSpace.Create(
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "simple-report.xlsx"),
        "Report");

      Assert.Throws<OutOfBoundsException>(() => { _ = space[-1, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = space[space.Area.Size.Width, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = space[0, space.Area.Size.Height]; });
      Assert.Throws<OutOfBoundsException>(
        () => space.GetSubspace(new Offset(0, 0), new Area(space.Area.Size.Width + 1, 1)));
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
