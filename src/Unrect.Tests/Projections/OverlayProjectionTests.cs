using System;
using System.Linq;
using System.Threading.Tasks;

using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The third layout combinator. Where a flow flows — each child consuming space and moving a
  /// cursor on — an overlay places: every child is applied to the same extent and finds its own
  /// spot inside it. Children are independent, may overlap, and may read the same cells, because
  /// they read rather than paint.
  /// <para>
  /// It is declared with the same cursor and the same lambda as a flow; all that differs is what
  /// the composite does between <c>Next</c> calls, which is the composite's business and not the
  /// cursor's.
  /// </para>
  /// </summary>
  public class OverlayProjectionTests
  {
    // Values are (row * 10 + column + 1) over 4 columns by 3 rows, so an assertion reads as a
    // coordinate: 1 2 3 4 / 11 12 13 14 / 21 22 23 24.
    // --- No flow ------------------------------------------------------------------------------------

    [Fact]
    public void Overlay_AppliesEveryChildToTheSameExtent()
    {
      // Two children, each placing itself independently inside the one extent.
      Assert.Equal("1|13", Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var down = o.Next(Down(1).Right(2).Of(IntCell()));

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(down)}");
      }).Map(CoordinateGrid()));
    }

    [Fact]
    public void Overlay_DoesNotAdvanceACursorBetweenChildren()
    {
      // The distinguishing test, now cursor against cursor: the same two calls read one cell twice
      // in an overlay and two rows in a flow. Nothing but the composite differs.
      Assert.Equal("1|1", Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var intCell2 = o.Next(IntCell());

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      }).Map(CoordinateGrid()));
      Assert.Equal("1|11", VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      }).Map(CoordinateGrid()));
    }

    [Fact]
    public void Overlay_LetsChildrenOverlapAndReadTheSameCells()
    {
      // Deliberately no z-order and no occlusion: reading a cell twice is not a conflict.
      var read = Overlay(o =>
      {
        var rangeSlot = o.Next(Range(2, 2, b => b[1, 0].Integer()));
        var right = o.Next(Right(1).Of(IntCell()));
        var right2 = o.Next(Right(1).Of(IntCell()));

        return o.Build(read2 => $"{read2.Of(rangeSlot)}|{read2.Of(right)}|{read2.Of(right2)}");
      });

      Assert.Equal("2|2|2", read.Map(CoordinateGrid()));
    }

    [Fact]
    public void Overlay_ProjectsChildrenInDeclarationOrder()
    {
      var result = Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var right = o.Next(Right(1).Of(IntCell()));
        var right2 = o.Next(Right(2).Of(IntCell()));

        return o.Build(read => new[] { read.Of(intCell), read.Of(right), read.Of(right2) });
      })
        .Map(CoordinateGrid());

      Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    // --- Bounding-box extent -------------------------------------------------------------------------

    [Fact]
    public void Overlay_SizesItselfToTheUnionOfItsChildrensFootprints()
    {
      // Per axis, the furthest any child reached: three columns across, two rows down.
      var applied = Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var down = o.Next(Down(1).Right(2).Of(IntCell()));

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(down)}");
      }).Apply(CoordinateGrid());

      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void Overlay_SizesItselfToTheWidestChildNotTheLast()
    {
      // The last child reaches least far; the bounding box is still the widest reach of any of
      // them.
      var applied = Overlay(o =>
      {
        var right = o.Next(Right(3).Of(IntCell()));
        var intCell = o.Next(IntCell());

        return o.Build(read => $"{read.Of(right)}|{read.Of(intCell)}");
      }).Apply(CoordinateGrid());

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void AFollowingSiblingStartsAfterTheOverlaysBoundingBox()
    {
      // What the derived extent is for: the overlay occupies one row here, so the next child of the
      // enclosing flow begins on the second.
      var band = Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var right = o.Next(Right(2).Of(IntCell()));

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(right)}");
      });

      Assert.Equal("1|3/11", VerticalFlow(v =>
      {
        var band2 = v.Next(band);
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(band2)}/{read.Of(intCell)}");
      }).Map(CoordinateGrid()));
    }

    [Fact]
    public void Sized_OverridesTheBoundingBox()
    {
      // Common for a header region, whose footprint on the sheet exceeds its sparse content.
      var band = Sized(AreaStrategies.ExplicitArea(4, 2)).Of(Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var right = o.Next(Right(2).Of(IntCell()));

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(right)}");
      }));

      var applied = band.Apply(CoordinateGrid());

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);

      Assert.Equal("1|3/21", VerticalFlow(v =>
      {
        var band2 = v.Next(band);
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(band2)}/{read.Of(intCell)}");
      }).Map(CoordinateGrid()));
    }

    // --- Misfit is a hard error -----------------------------------------------------------------------

    [Fact]
    public void Overlay_WhenAChildDoesNotFit_Throws()
    {
      // Consistent with a flow: an overlay places its children, it does not decide which of them
      // are optional.
      var block = Range(9, 9, b => b.Width);

      var failure = Assert.Throws<ProjectionException>(() =>
        Overlay(o =>
        {
          var intCell = o.Next(IntCell());
          var block2 = o.Next(block);

          return o.Build(read => $"{read.Of(intCell)}|{read.Of(block2)}");
        }).Map(CoordinateGrid()));

      Assert.Contains("an extent of 9x9 does not fit here", failure.Message);
      Assert.Equal("Overlay -> 'block' (Range)", failure.Path);
    }

    [Fact]
    public void Overlay_WhenAChildsOffsetRunsOff_Throws()
    {
      Assert.Throws<ProjectionException>(() =>
        Overlay(o =>
        {
          var intCell = o.Next(IntCell());
          var right = o.Next(Right(9).Of(IntCell()));

          return o.Build(read => $"{read.Of(intCell)}|{read.Of(right)}");
        }).Map(CoordinateGrid()));
    }

    // --- Context and diagnostics ----------------------------------------------------------------------

    [Fact]
    public void AChildFailure_ReportsItsAbsolutePosition()
    {
      // Each child descends from the overlay's scope carrying its own offset, so a failure names
      // where the child actually landed rather than where the overlay starts.
      var title = Down(1).Right(2).Of(TextCell());

      var failure = Assert.Throws<ProjectionException>(() =>
        Overlay(o =>
        {
          var intCell = o.Next(IntCell());
          var title2 = o.Next(title);

          return o.Build(read => $"{read.Of(intCell)}|{read.Of(title2)}");
        }).Map(CoordinateGrid()));

      Assert.Equal("Overlay -> 'title' (Text)", failure.Path);
      Assert.Equal("C2", failure.Location.A1);
      Assert.Equal(2, failure.Location.Row);
      Assert.Equal(3, failure.Location.Column);
    }

    [Fact]
    public void AChildFailure_IsReportedRelativeToAPlacedOverlayToo()
    {
      var title = Right(1).Of(TextCell());

      var failure = Assert.Throws<ProjectionException>(() =>
        Down(1).Of(Overlay(o =>
        {
          var intCell = o.Next(IntCell());
          var title2 = o.Next(title);

          return o.Build(read => $"{read.Of(intCell)}|{read.Of(title2)}");
        })).Map(CoordinateGrid()));

      Assert.Equal("B2", failure.Location.A1);
    }

    // --- Inspection ------------------------------------------------------------------------------------

    [Fact]
    public void AnOverlayDescribesItselfAndExposesItsChildren()
    {
      // The lambda ran once, at declaration, so an overlay's children are complete and it hides
      // nothing from a renderer.
      var overlay = Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var right = o.Next(Right(1).Of(IntCell()));

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(right)}");
      });

      Assert.Equal("Overlay", overlay.Description);
      Assert.Equal(2, overlay.Children.Count);
      Assert.False(overlay.IsWrapper);
      Assert.Null(overlay.Opacity);
    }

    [Fact]
    public void AnOverlayDerivesItsExtent()
    {
      Assert.Null(Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var intCell2 = o.Next(IntCell());

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      }).Placement.Area);
    }

    [Fact]
    public void AnOverlayIsAProjectionAndCanBeNamedAndPlaced()
    {
      var space = Grid(new[,] { { 0, 0 }, { 1, 2 } });

      var projection = AfterBlankRows().Of(Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var right = o.Next(Right(1).Of(IntCell()));

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(right)}");
      })).Named("header");

      Assert.Equal("1|2", projection.Map(space));
      Assert.Equal("header", projection.Name);
    }

    // --- Arity -------------------------------------------------------------------------------------------

    [Fact]
    public void AnOverlayHasNoArityLimit()
    {
      // Children are Next calls, so there is no tuple to run out of and no nesting to reach for.
      var projection = Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var right = o.Next(Right(1).Of(IntCell()));
        var right2 = o.Next(Right(2).Of(IntCell()));
        var right3 = o.Next(Right(3).Of(IntCell()));
        var down = o.Next(Down(1).Of(IntCell()));
        var down2 = o.Next(Down(1).Right(1).Of(IntCell()));
        var down3 = o.Next(Down(1).Right(2).Of(IntCell()));
        var down4 = o.Next(Down(2).Of(IntCell()));
        var down5 = o.Next(Down(2).Right(1).Of(IntCell()));
        var down6 = o.Next(Down(2).Right(2).Of(IntCell()));

        return o.Build(read => string.Join(",", new[]
        {
          read.Of(intCell), read.Of(right), read.Of(right2), read.Of(right3),
          read.Of(down), read.Of(down2), read.Of(down3),
          read.Of(down4), read.Of(down5), read.Of(down6),
        }));
      });

      Assert.Equal("1,2,3,4,11,12,13,21,22,23", projection.Map(CoordinateGrid()));
    }

    [Fact]
    public void OverlaysAndFlowsNest()
    {
      // The header band of a real report: two independent blocks sharing rows, above a table.
      var space = Mixed(new object?[,]
      {
        { "Acme Fund", null, null, "2026" },
        { null, null, null, null },
        { "Item", "Amount", null, null },
        { "Fees", 10, null, null },
      });

      var entity = TextCell();
      var year = Right(3).Of(TextCell());
      var items = Table(r => r["Amount"].Integer());

      var projection = VerticalFlow(v =>
      {
        var overlay = v.Next(Overlay(o =>
          {
            var entity2 = o.Next(entity);
            var year2 = o.Next(year);

            return o.Build(read => $"{read.Of(entity2)}/{read.Of(year2)}");
          }));
        var items2 = v.Next(items);

        return v.Build(read => $"{read.Of(overlay)}|{string.Join(",", read.Of(items2))}");
      });

      Assert.Equal("Acme Fund/2026|10", projection.Map(space));
    }

    // --- Guards -------------------------------------------------------------------------------------------

    [Fact]
    public void AnOverlayThatDeclaresNothing_IsRefusedWhereItIsWritten()
    {
      // Same rule as a flow, told with the right noun: an overlay that declared nothing would match
      // anything and describe nothing, so Build refuses it at declaration.
      var failure = Assert.Throws<InvalidOperationException>(() => Overlay<int>(o => o.Build(_ => 42)));

      Assert.Equal("an overlay must declare at least one projection; this one called Next zero times", failure.Message);
    }

    [Fact]
    public void ANullChildIsRefusedWhereItIsWritten()
    {
      IProjection<ISheetCells, int>? missing = null;

      var failure = Assert.Throws<ArgumentNullException>(() =>
        Overlay(o =>
        {
          var down = o.Next(Down(1).Of(IntCell()));
          var missing2 = o.Next(missing!);

          return o.Build(read => $"{read.Of(down)}|{read.Of(missing2)}");
        }));

      Assert.Contains("a null projection was declared as child 2", failure.Message);
    }

    [Fact]
    public void AnOverlayChildCarriesNoSiblingNote()
    {
      // The note explains a child failing on cells its predecessor declined to consume. An overlay
      // has no such relation — every child starts from the same origin whatever its neighbours did
      // — so the identical declaration is noted in a flow and silent here.
      var space = Mixed(new object?[,] { { "x" }, { 5 } });

      var absorbed = IntCell().Optional();
      var second = IntCell();

      var inOverlay = Assert.Throws<ProjectionException>(() =>
        Overlay(o =>
        {
          var absorbed2 = o.Next(absorbed);
          var second2 = o.Next(second);

          return o.Build(read => $"{read.Of(absorbed2)}|{read.Of(second2)}");
        }).Map(space));

      var inFlow = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          var absorbed2 = v.Next(absorbed);
          var second2 = v.Next(second);

          return v.Build(read => $"{read.Of(absorbed2)}|{read.Of(second2)}");
        }).Map(space));

      Assert.DoesNotContain("note:", inOverlay.Message);
      Assert.Contains("note: the preceding sibling consumed nothing at this position", inFlow.Message);
    }

    [Fact]
    public void ACaptureNothingOverlayIsSafeToApplyToManySpacesAtOnce()
    {
      var projection = Overlay(o =>
      {
        var intCell = o.Next(IntCell());
        var right = o.Next(Right(1).Of(IntCell()));

        return o.Build(read => $"{read.Of(intCell)}|{read.Of(right)}");
      });

      var spaces = Enumerable.Range(0, 64)
        .Select(seed => Grid(new[,] { { seed + 1, seed + 2 } }))
        .ToArray();

      var results = new string[spaces.Length];

      Parallel.For(0, spaces.Length, index => results[index] = projection.Map(spaces[index]));

      for (var index = 0; index < spaces.Length; index++)
        Assert.Equal($"{index + 1}|{index + 2}", results[index]);
    }
  }
}
