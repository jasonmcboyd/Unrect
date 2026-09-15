using System;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// How the engine cuts the region it is working in — the three cuts, and which of them carries a
  /// bottom edge still being discovered.
  /// <para>
  /// The distinction is the whole of it. <c>Tail</c> and <c>Narrow</c> say "the rest of this", which
  /// is still a question about the region whose end nobody knows; <c>Cut</c> names a rectangle, and
  /// a named rectangle is a measured one. Getting it the other way round would either settle every
  /// bound at the first child (no streaming at all) or hand a nested declaration a region that
  /// silently outgrew what it asked for.
  /// </para>
  /// <para>
  /// The other rule pinned here is the phase's own: every plane the engine makes has its origin at
  /// (0, 0) over a real subspace object, because the streaming window's locus rides on that object's
  /// extent. A cut that translated arithmetically instead would stop telling the store which band is
  /// open — moving the eviction counters and not the answers, the worst kind of change.
  /// </para>
  /// </summary>
  public class ExtentsTests
  {
    /// <summary>
    /// A bottom edge that admits a fixed number of rows and remembers how far it was asked to look,
    /// so a test can say "this settled nothing".
    /// </summary>
    private sealed class CountingBound : IBound
    {
      private readonly int _height;

      internal CountingBound(int height) => _height = height;

      /// <summary>How many times the whole extent was settled.</summary>
      internal int Forced { get; private set; }

      /// <summary>The furthest row anything asked about, or -1 where nothing did.</summary>
      internal int Reached { get; private set; } = -1;

      public bool HasRow(int row)
      {
        if (row > Reached)
          Reached = row;

        return row < _height;
      }

      public int Force()
      {
        Forced++;
        Reached = _height;

        return _height;
      }

      public IBound Shift(int rows) => new Shifted(this, rows);

      private sealed class Shifted : IBound
      {
        private readonly CountingBound _owner;
        private readonly int _rows;

        internal Shifted(CountingBound owner, int rows)
        {
          _owner = owner;
          _rows = rows;
        }

        public bool HasRow(int row) => _owner.HasRow(_rows + row);

        public int Force() => _owner.Force() - _rows;

        public IBound Shift(int rows) => new Shifted(_owner, _rows + rows);
      }
    }

    /// <summary>A four-wide, ten-tall grid under a bottom edge that will admit only six of its rows.</summary>
    private static (Plane<ICellValues> Extent, CountingBound Bound) Discovering(int height = 6)
    {
      var bound = new CountingBound(height);

      return (CoordinateGrid(4, 10).Extent().Bounded(bound, 4), bound);
    }

    // --- Tail: the rest of a region, still being discovered --------------------------------------------

    [Fact]
    public void TheTailOfARegionKeepsItsBottomEdgeUnsettled()
    {
      // What a flow hands its next child and a repeat its next occurrence. If this measured, a flow
      // of two children over a discovered extent would settle the whole scan to place the second —
      // which is exactly the forcing the bound exists to avoid.
      var (extent, bound) = Discovering();

      var rest = extent.Tail(new Offset(1, 2));

      Assert.NotNull(rest.Bound);
      Assert.Equal(3, rest.Width);
      Assert.Equal(0, bound.Forced);

      // The tail's row r is the parent's row r + 2, so nothing adds an origin to a row number.
      for (var row = 0; row < 8; row++)
        Assert.Equal(extent.HasRow(row + 2), rest.HasRow(row));

      Assert.True(rest.HasRow(3));
      Assert.False(rest.HasRow(4));
      Assert.Equal(0, bound.Forced);
    }

    [Fact]
    public void TheTailReadsThroughASpaceThatIsStillMeasured()
    {
      // The subtlety that makes the tail work at all: the bound hides the rows below the boundary,
      // so the space underneath must stay its own full height — otherwise the scan would have
      // nowhere left to look when it goes hunting for the boundary it has not found yet.
      var (extent, _) = Discovering();

      var rest = extent.Tail(new Offset(0, 2));

      // Eight rows of space to look through, four rows of region: the boundary is at the parent's
      // row 6, and the tail starts two rows into it.
      Assert.Equal(8, rest.Space.Area.Height);
      Assert.Equal(4, rest.Area.Height);
    }

    // --- Cut: a named rectangle is a measured one -------------------------------------------------------

    [Fact]
    public void ANamedRectangleCarriesNoBottomEdgeToDiscover()
    {
      // Asking for part of a region is not a question about the whole of it. The rows asked for are
      // admitted one at a time on the way in — so this still cannot reach past the boundary — and
      // what comes back answers from its own height thereafter.
      var (extent, bound) = Discovering();

      var cut = extent.Cut(new Offset(0, 1), new Area(2, 3));

      Assert.Null(cut.Bound);
      Assert.Equal(3, cut.Area.Height);
      Assert.Equal(2, cut.Width);
      Assert.Equal(0, bound.Forced);

      // The parent was advanced exactly as far as the rectangle reaches and no further: the last
      // row asked about is offset + height - 1, and the row after that is still nobody's business.
      Assert.Equal(3, bound.Reached);
    }

    [Fact]
    public void ARectangleReachingPastTheBoundaryIsRefusedWithoutSettlingIt()
    {
      var (extent, bound) = Discovering();

      Assert.Throws<OutOfBoundsException>(() => extent.Cut(new Offset(0, 4), new Area(2, 4)));
      Assert.Equal(0, bound.Forced);
    }

    // --- Narrow: the horizontal twin --------------------------------------------------------------------

    [Fact]
    public void NarrowingKeepsTheBottomEdgeUnshifted()
    {
      // Columns are not rows: taking the leading columns of a region says nothing about where it
      // ends, so the discovery is carried across unchanged rather than re-based.
      var (extent, bound) = Discovering();

      var narrow = extent.Narrow(2);

      Assert.NotNull(narrow.Bound);
      Assert.Same(extent.Bound, narrow.Bound);
      Assert.Equal(2, narrow.Width);
      Assert.Equal(0, bound.Forced);

      for (var row = 0; row < 8; row++)
        Assert.Equal(extent.HasRow(row), narrow.HasRow(row));
    }

    [Fact]
    public void AWidthPastTheEdgeIsAnOverrun_AndANegativeWidthIsAFault()
    {
      // The two halves of one rule, stated together because the division between them is the whole
      // point and is invisible when either is pinned alone.
      //
      // Four columns is one too many for a four-wide region, and running off an edge is a statement
      // about the DATA: a declaration may recover from it, a repeat stops on it, a tolerance
      // boundary may absorb it. Minus one is not a narrower region — it is not a region at all, and
      // nothing about the sheet could have produced the request. That is an argument bug, so it
      // arrives as ArgumentOutOfRangeException, which is on the engine's fault list and can never be
      // absorbed. Getting them the same way round would either make a bug look like an absent
      // section or make an ordinary overrun unrecoverable.
      var (extent, bound) = Discovering();

      Assert.Throws<OutOfBoundsException>(() => extent.Narrow(extent.Width + 1));
      Assert.Throws<ArgumentOutOfRangeException>(() => extent.Narrow(-1));

      // And the same division one level down, where the width arrives beside the bound: a plane
      // narrowed to nothing-at-all is the caller's mistake, not the sheet's.
      Assert.Throws<OutOfBoundsException>(() => extent.Bounded(bound, extent.Width + 1));
      Assert.Throws<ArgumentOutOfRangeException>(() => extent.Bounded(bound, -1));

      // Neither refusal settled anything: a region does not have to know where it ends to say that
      // minus one columns is not a width.
      Assert.Equal(0, bound.Forced);
    }

    // --- The origin rule --------------------------------------------------------------------------------

    [Fact]
    public void EveryCutIsARealSubspaceObjectAndNotAnArithmeticTranslation()
    {
      // The phase's rule, stated where it can be broken. A plane can translate arithmetically — that
      // is what an origin is for — but until the streaming store is told directly which band is
      // open, it learns that from the subspace object's own extent. So each cut must hand back a
      // DIFFERENT space, with its origin back at zero, and not the parent space with an origin added.
      var (extent, _) = Discovering();

      foreach (var cut in new[]
      {
        extent.Cut(new Offset(1, 1), new Area(2, 2)),
        extent.Cut(new Area(2, 2)),
        extent.Tail(new Offset(1, 1)),
        extent.Narrow(2),
      })
      {
        Assert.NotSame(extent.Space, cut.Space);
        Assert.Equal(0, cut.Origin.Width);
        Assert.Equal(0, cut.Origin.Height);
      }
    }

    /// <summary>
    /// The three cuts, as theory data, so the guard is stated once per door rather than once per
    /// method — a cut that forgot it would be the only one with a hole in it.
    /// </summary>
    public static TheoryData<string> Cuts => new TheoryData<string> { "cut", "tail", "narrow" };

    [Theory]
    [MemberData(nameof(Cuts))]
    public void ATranslatedRegionCannotBeCutAtAll(string cut)
    {
      // The rule enforced rather than remembered. A plane CAN translate arithmetically, and a
      // translated one cut here would read exactly the right cells — the origin composes correctly —
      // while telling the streaming store the wrong band was open. Right answers, wrong counters:
      // the failure mode nothing downstream can notice, which is why the door refuses instead of
      // trusting the rule to be kept.
      var translated = CoordinateGrid(4, 10).Extent().Slice(new Offset(1, 1));

      Assert.Equal(1, translated.Origin.Width);
      Assert.Equal(1, translated.Origin.Height);

      Assert.Throws<EngineInvariantException>(() => Cutting(cut, translated));

      // ...and the same cut over an untranslated region is ordinary business, so the refusal is
      // about the origin and not about the arguments.
      _ = Cutting(cut, CoordinateGrid(4, 10).Extent());
    }

    [Theory]
    [MemberData(nameof(Cuts))]
    public void TheGuardHoldsAtEveryDoor(string cut)
    {
      // Stated across the three ways a space can enter the library for the reason every contract law
      // here is: the engine cannot see which door it was handed, and a guard kept by one backend and
      // not another is not a guard. The windowed door is the one it exists for — the locus rides on
      // its subspace objects — and the other two are what make it a rule rather than a special case.
      foreach (var door in new[] { "grid", "windowed", "xlsx" })
      {
        var whole = Door(door).Extent();

        Assert.True(whole.Width >= 2 && whole.Area.Height >= 2, $"the '{door}' door needs a 2x2 space");

        Assert.Throws<EngineInvariantException>(() => Cutting(cut, whole.Slice(new Offset(1, 1))));
      }
    }

    /// <summary>One of the three cuts, applied to <paramref name="extent"/> at its own corner.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cut"/> names no cut.</exception>
    private static Plane<ICellValues> Cutting(string cut, Plane<ICellValues> extent) =>
      cut switch
      {
        "cut" => extent.Cut(default, new Area(1, 1)),
        "tail" => extent.Tail(default),
        "narrow" => extent.Narrow(1),
        _ => throw new ArgumentOutOfRangeException(nameof(cut), cut, "No such cut.")
      };

    [Fact]
    public void AndABrokenInvariantIsAFaultThatNoToleranceAbsorbs()
    {
      // The classification, which matters more than the message. An invariant this library owes
      // itself is never a statement about the document, so `Optional` — whose whole job is to say
      // "that section is not there" — must not be allowed to say it about a bug in the reader. That
      // would be the worst answer in the taxonomy: a wrong result with nothing anywhere reporting it.
      //
      // Provoked from inside a projection, because that is the only place a declaration can reach a
      // cut at all, and through Optional specifically because the measured-extent case it wraps
      // would otherwise absorb anything.
      var probe = Range(3, 3, block => block.Space.Slice(new Offset(1, 1)).Tail(default).Width).Named("probe");

      var failure = Assert.Throws<ProjectionException>(() => probe.Optional().Map(CoordinateGrid(4, 10)));

      Assert.True(failure.IsFault, "a broken engine invariant must be a fault");
      Assert.IsType<EngineInvariantException>(failure.GetBaseException());
      Assert.Equal("'probe'", failure.Subject);

      // Non-vacuity: the same tolerance over the same shape absorbs an ordinary absence perfectly
      // well, so "not absorbed" above is about the exception and not about Optional being inert.
      Assert.Null(On(RowContaining("no such caption")).Of(Text()).Optional().Map(CoordinateGrid(4, 10)));
    }

    [Fact]
    public void AProjectionIsHandedARegionWhoseOriginIsItsOwnCorner()
    {
      // The same rule seen from the declaration's side, through the engine rather than the helper:
      // a block placed two rows down still reports (0, 0), because what it was handed is a space of
      // its own. A plane that had been translated instead would report (0, 2) here, and every A1 the
      // engine cites would be counted twice.
      var origin = VerticalFlow(v =>
      {
        v.Next(Row(cells => cells.Count));
        v.Next(Row(cells => cells.Count));

        return v.Next(Range(WholeExtent(), block => block.Space.Origin));
      }).Map(CoordinateGrid(4, 10));

      Assert.Equal(0, origin.Width);
      Assert.Equal(0, origin.Height);
    }
  }
}
