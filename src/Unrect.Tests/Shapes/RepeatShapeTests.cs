using System;
using System.Linq;
using System.Threading.Tasks;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// Repeat applies one declared item as many times as the space supports. The separator sits
  /// between items and never before the first; termination is guarded so a shape that consumes
  /// nothing stops rather than loops; and only the item's own placement is a stopping condition —
  /// anything that goes wrong deeper inside the item is a loud error.
  /// </summary>
  public class RepeatShapeTests
  {
    // --- The separator sits between items -------------------------------------------------------------

    [Fact]
    public void Repeat_AppliesTheSeparatorBetweenItemsAndNeverBeforeTheFirst()
    {
      // A one-row separator on a dense ladder: if it were applied before the first item too, this
      // would read 2 and 4 rather than 1 and 3.
      var items = Repeat(IntCell(), separatedBy: SkipRows(1)).Map(Ladder(4));

      Assert.Equal(new[] { 1, 3 }, items);
    }

    [Fact]
    public void Repeat_SkipsBlankSeparatorRowsBetweenItems()
    {
      var space = Grid(new[,] { { 1 }, { 0 }, { 2 }, { 0 }, { 3 } });

      Assert.Equal(new[] { 1, 2, 3 }, Repeat(IntCell(), separatedBy: BlankRows()).Map(space));
    }

    [Fact]
    public void Repeat_WithoutASeparator_TakesItemsBackToBack()
    {
      Assert.Equal(new[] { 1, 2, 3 }, Repeat(IntCell()).Map(Ladder(3)));
    }

    [Fact]
    public void Repeat_LeadingGapIsTheRepeatsOwnOffsetNotTheSeparator()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 0 }, { 2 } });

      var items = Repeat(IntCell(), separatedBy: BlankRows()).AfterBlankRows().Map(space);

      Assert.Equal(new[] { 1, 2 }, items);
    }

    // --- Termination -------------------------------------------------------------------------------------

    [Fact]
    public void Repeat_WithATrailingBlankBand_YieldsNoExtraItemAndDoesNotThrow()
    {
      var space = Grid(new[,] { { 1 }, { 0 }, { 2 }, { 0 }, { 0 } });

      Assert.Equal(new[] { 1, 2 }, Repeat(IntCell(), separatedBy: BlankRows()).Map(space));
    }

    [Fact]
    public void Repeat_DoesNotConsumeASeparatorThatIsFollowedByNothing()
    {
      // The cursor is tentative until an item is actually collected, so a separator that leads
      // nowhere — a trailing blank band — is not counted as consumed. Two items over five rows
      // therefore consume three, not five.
      var space = Grid(new[,] { { 1 }, { 0 }, { 2 }, { 0 }, { 0 } });

      var applied = Repeat(Range(1, 1, b => b.Width), separatedBy: BlankRows()).Apply(space);

      Assert.Equal(2, applied.Value.Count);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void Repeat_LeavesATrailingBlankBandForWhateverFollowsIt()
    {
      // The consequence of not swallowing the band: a sibling after the repeat still sees it. The
      // explicit 1x2 extent is the assertion — it only fits because both blank rows are still there.
      var space = Grid(new[,] { { 1 }, { 0 }, { 2 }, { 0 }, { 0 } });

      var items = Repeat(Range(1, 1, b => b[0, 0].GetInt()), separatedBy: BlankRows());
      var band = Range(1, 2, b => b.Height);

      var read = VerticalFlow(v => $"{string.Join(",", v.Next(items))}|{v.Next(band)}").Map(space);

      Assert.Equal("1,2|2", read);
    }

    [Fact]
    public void Repeat_TreatsAValueAfterABlankBandAsAnotherItemNotAsTheEnd()
    {
      // A blank band is a separator, never a terminator: with separatedBy: BlankRows() the repeat
      // steps over a gap of any width and keeps going. "Stop at the first big gap" is not something
      // a separator can express — bound the repeat's own extent if that is what the format means.
      var space = Grid(new[,] { { 1 }, { 0 }, { 2 }, { 0 }, { 0 }, { 9 } });

      var items = Repeat(Range(1, 1, b => b[0, 0].GetInt()), separatedBy: BlankRows()).Map(space);

      Assert.Equal(new[] { 1, 2, 9 }, items);
    }

    [Fact]
    public void Repeat_OnAnAllBlankSpace_YieldsNoItems()
    {
      var space = Grid(new[,] { { 0, 0 }, { 0, 0 } });

      Assert.Empty(Repeat(Range(b => b.Height)).Map(space));
    }

    [Fact]
    public void Repeat_OnAnEmptySpace_YieldsNoItems()
    {
      var space = Grid(new[,] { { 1, 1 } }).GetSubspace(new Offset(0, 0), new Area(0, 0));

      Assert.Empty(Repeat(IntCell()).Map(space));
    }

    [Fact(Timeout = 30000)]
    public async Task Repeat_WithAZeroAreaItem_TerminatesInsteadOfLooping()
    {
      // An item that occupies nothing would repeat forever. Run it off-thread so a regression fails
      // the test on a timeout rather than hanging the run.
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 } });
      var shape = Repeat(Range(AreaStrategies.MinArea(), b => b.Height));

      var items = await Task.Run(() => shape.Map(space));

      Assert.Empty(items);
    }

    [Fact(Timeout = 30000)]
    public async Task Repeat_WithAZeroHeightItem_TerminatesInsteadOfLooping()
    {
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 } });
      var shape = Repeat(Range(AreaStrategies.ExplicitArea(2, 0), b => b.Height));

      var items = await Task.Run(() => shape.Map(space));

      Assert.Empty(items);
    }

    [Fact(Timeout = 30000)]
    public async Task RepeatHorizontal_WithAZeroWidthItem_TerminatesInsteadOfLooping()
    {
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 } });
      var shape = RepeatHorizontal(Range(AreaStrategies.ExplicitArea(0, 2), b => b.Width));

      var items = await Task.Run(() => shape.Map(space));

      Assert.Empty(items);
    }

    [Fact]
    public void Repeat_WhenTheNextItemDoesNotFit_StopsWithoutThrowing()
    {
      // A trailing partial item is left unconsumed: "no more items" is a stopping condition, not an
      // error. This is the item's OWN placement failing to resolve.
      var items = Repeat(Range(1, 2, b => b.Height)).Map(Ladder(5));

      Assert.Equal(new[] { 2, 2 }, items);
    }

    // --- Failures inside an item are errors, not stopping conditions ------------------------------------------

    [Fact]
    public void Repeat_WhenAShapeNestedInsideTheItemDoesNotFit_Throws()
    {
      // The item's own placement resolves fine on the last row; it is the item's second child that
      // runs out of space. Intra-block format drift must be loud, not a silent truncation.
      var item = VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}");

      var failure = Assert.Throws<ShapeException>(() => Repeat(item).Map(Ladder(3)));

      Assert.Contains("Repeat[1]", failure.Path);
    }

    [Fact]
    public void Repeat_WhenAProjectionInsideTheItemThrows_Propagates()
    {
      // Cell has no notion of "value bearing", so a trailing blank is a projection failure rather
      // than a stopping condition — and the repeat surfaces it.
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 } });

      var failure = Assert.Throws<ShapeException>(() => Repeat(IntCell()).Map(space));

      Assert.IsType<InvalidOperationException>(failure.InnerException);
      Assert.Contains("Repeat[2]", failure.Path);
    }

    [Fact]
    public void Repeat_WhenAStrategyInsideTheItemThrows_DoesNotMistakeItForTheEndOfTheRun()
    {
      // Only running out of space stops a repeat. A strategy that fails any other way is a broken
      // declaration, and silently returning the items collected so far would hide it.
      var space = Grid(new[,] { { 1, 2 }, { 3, 4 } });
      var item = Range(AreaStrategies.SelectArea(_ => throw new InvalidOperationException("boom")), b => b.Width);

      var failure = Assert.Throws<ShapeException>(() => Repeat(item).Map(space));

      Assert.Contains("its area strategy threw InvalidOperationException: boom", failure.Message);
      Assert.Contains("Repeat[0]", failure.Path);
    }

    [Fact]
    public void Repeat_WithoutASeparator_TreatsATrailingBlankBandAsFormatDrift()
    {
      // A separator is what tells a repeat that a blank band ends the run. Without one the item is
      // applied to the band and fails loudly — separatedBy is load-bearing for termination, not
      // just for skipping gaps.
      var space = Mixed(new object?[,]
      {
        { "A-1", null },
        { "Name", "Amount" },
        { "Acme", 10 },
        { null, null },
        { null, null },
      });

      var item = VerticalFlow(v =>
      {
        var code = v.Next(Cell(c => c.GetString()).Named("code"));
        v.Next(TableRows(r => r["Amount"].GetInt()).Named("rows"));
        return code;
      });

      var failure = Assert.Throws<ShapeException>(() => Repeat(item).Map(space));

      Assert.Contains("the projection threw", failure.Message);
      Assert.Contains("Repeat[1]", failure.Path);

      // Declared with the separator, the same shape over the same space stops cleanly.
      Assert.Equal(new[] { "A-1" }, Repeat(item, separatedBy: BlankRows()).Map(space));
    }

    // --- Seeking as the stopping condition ---------------------------------------------------------------------
    //
    // A seek that finds nothing is a placement failure, and a repeat treats its item's own placement
    // failing as "no more items". "Repeat sections until there are no more section labels" therefore
    // needs no separator, no atLeast, and no coordinates — it falls out of the two rules meeting.

    [Fact]
    public void Repeat_OfASeekingItem_CollectsEverySectionAndThenStops()
    {
      var space = Mixed(new object?[,]
      {
        { "Section", null },
        { "a", 1 },
        { "Section", null },
        { "b", 2 },
        { "unrelated trailing junk", null },
        { "with no section label", null },
      });

      var section = VerticalFlow(v =>
      {
        v.Next(Cell(c => c.GetString()).Named("label"));
        return v.Next(Cell(c => c.GetInt()).Right(1).Named("amount"));
      }).After(To(RowContaining("Section")));

      var amounts = Repeat(section).Map(space);

      Assert.Equal(new[] { 1, 2 }, amounts);
    }

    [Fact]
    public void Repeat_OfASeekingItem_SkipsWhateverSitsBetweenTheSections()
    {
      // The point of anchoring on presence: rows inserted between sections move the next label, and
      // the seek simply finds it again. A skip-while would have stopped at the first inserted row.
      var space = Mixed(new object?[,]
      {
        { "preamble", null },
        { "Section", null },
        { "a", 1 },
        { "an inserted proof row", null },
        { "Section", null },
        { "b", 2 },
      });

      var section = VerticalFlow(v =>
      {
        v.Next(Cell(c => c.GetString()).Named("label"));
        return v.Next(Cell(c => c.GetInt()).Right(1).Named("amount"));
      }).After(To(RowContaining("Section")));

      Assert.Equal(new[] { 1, 2 }, Repeat(section).Map(space));
    }

    [Fact]
    public void Repeat_OfASeekingItem_WithNoSectionsAtAll_IsEmptyRatherThanAnError()
    {
      var space = Mixed(new object?[,] { { "nothing", null }, { "here", null } });

      var section = Cell(v => v.GetString()).After(To(RowContaining("Section")));

      Assert.Empty(Repeat(section).Map(space));
    }

    [Fact]
    public void ASeekingShape_OutsideARepeat_IsStillAHardError()
    {
      // The same missing anchor: a stopping condition inside a repeat, a reported failure anywhere
      // else. Only the item's own placement is allowed to be optional.
      var space = Mixed(new object?[,] { { "nothing", null }, { "here", null } });

      var failure = Assert.Throws<ShapeException>(() =>
        Cell(v => v.GetString()).After(To(RowContaining("Section"))).Map(space));

      Assert.Contains("no row containing 'Section' exists in the available space", failure.Message);
    }

    // --- The Under trap, and its recipe (§3.4) -------------------------------------------------------------------
    //
    // A repeat stops only when the ITEM'S OWN placement fails. .Under puts the caption's anchor
    // inside the flow, and the flow's own placement always fits — so a missing caption on the last
    // iteration is a failure one level down, which is loud. Both halves are documented, so both are
    // pinned: the trap, and the one-modifier recipe that fixes it.

    /// <summary>Two captioned sections, a blank line between them, and a totals row that is neither.</summary>
    private static ISpace CaptionedSections() => Mixed(new object?[,]
    {
      { "Detail" },
      { "a" },
      { null },
      { "Detail" },
      { "b" },
      { null },
      { "Totals" },
    });

    [Fact]
    public void ARepeatOfAnUnderSection_FailsLoudlyWhenTheCaptionsRunOut()
    {
      // The trap. The iteration past the last section finds no caption, and because the anchor is
      // inside the item rather than on it, that is drift rather than exhaustion.
      var section = Range(b => b.Height).Under(Caption("Detail"));

      var hoisted = Assert.Throws<ShapeException>(() =>
        Repeat(section, separatedBy: BlankRows()).Map(CaptionedSections()));

      // The repeat's own index, then the item — labelled by the local it was hoisted into — then
      // the caption that was not found, at its ordinal inside the desugared flow.
      Assert.Equal("Repeat[2] -> 'section' -> Caption(\"Detail\")#1", hoisted.Path);
      Assert.Contains("no row containing 'Detail' exists in the available space", hoisted.Message);

      // Written inline there is no identifier to capture, and the flow renders by its description.
      var inline = Assert.Throws<ShapeException>(() =>
        Repeat(Range(b => b.Height).Under(Caption("Detail")), separatedBy: BlankRows()).Map(CaptionedSections()));

      Assert.Equal("Repeat[2] -> Under -> Caption(\"Detail\")#1", inline.Path);
    }

    [Fact]
    public void ARepeatOfAnUnderSection_StopsGracefullyWhenTheAnchorIsAlsoOnTheItem()
    {
      // The recipe: hoist the matcher and put it on the item's placement as well. The seek is
      // idempotent — the flow lands ON the caption row and the caption inside finds it at distance
      // zero — so the item's own placement is what runs out, which is a stop.
      var detail = RowContaining("Detail");
      var section = Range(b => b.Height).Under(Caption("Detail")).After(To(detail));

      var result = Repeat(section, separatedBy: BlankRows()).MapWithDiagnostics(CaptionedSections());

      Assert.Equal(new[] { 1, 1 }, result.Value);

      // ...and the totals row is left undescribed rather than swallowed or blamed.
      var info = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Info);
      Assert.Equal("the shape consumed 5 of 7 rows; rows 6+ were not described", info.Message);
    }

    [Fact]
    public void AnItemAnchoredWithPast_AlsoStopsRatherThanThrowing()
    {
      // Past is a lift like To, so it fails the same way, so a repeat treats it the same way.
      var item = Cell(c => c.GetString()).After(Past(RowContaining("Detail")));

      var items = Repeat(item).Map(Mixed(new object?[,] { { "Detail" }, { "a" }, { "Detail" }, { "b" } }));

      Assert.Equal(new[] { "a", "b" }, items);
    }

    // --- atLeast ---------------------------------------------------------------------------------------------

    [Fact]
    public void Repeat_WithAnUnmetMinimum_Throws()
    {
      var failure = Assert.Throws<ShapeException>(() => Repeat(IntCell(), atLeast: 5).Map(Ladder(2)));

      Assert.Contains("expected at least 5 occurrences but found 2", failure.Message);
    }

    [Fact]
    public void Repeat_WithAMetMinimum_Succeeds()
    {
      Assert.Equal(new[] { 1, 2, 3 }, Repeat(IntCell(), atLeast: 3).Map(Ladder(3)));
    }

    [Fact]
    public void Repeat_WithAMinimumOfZero_AllowsAnEmptyResultThatAMinimumOfOneRejects()
    {
      // atLeast is the whole difference between "there were none" and "there should have been one":
      // the same shape over the same space either returns empty or fails, by declaration alone.
      var space = Grid(new[,] { { 0, 0 } });

      Assert.Empty(Repeat(Range(b => b.Height), atLeast: 0).Map(space));
      Assert.Throws<ShapeException>(() => Repeat(Range(b => b.Height), atLeast: 1).Map(space));
    }

    [Fact]
    public void Repeat_WithANegativeMinimum_IsRejectedAtConstruction()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => Repeat(IntCell(), atLeast: -1));
      Assert.Throws<ArgumentOutOfRangeException>(() => RepeatHorizontal(IntCell(), atLeast: -1));
    }

    [Fact]
    public void Repeat_RejectsANullItem()
    {
      Assert.Throws<ArgumentNullException>(() => Repeat((IShape<int>)null!));
    }

    // --- Horizontal repetition --------------------------------------------------------------------------------

    [Fact]
    public void RepeatHorizontal_YieldsItemsLeftToRight()
    {
      var space = Grid(new[,] { { 1, 2, 3 } });

      Assert.Equal(new[] { 1, 2, 3 }, RepeatHorizontal(IntCell()).Map(space));
    }

    [Fact]
    public void RepeatHorizontal_SkipsBlankSeparatorColumns()
    {
      var space = Grid(new[,] { { 1, 0, 2, 0, 3 } });

      Assert.Equal(new[] { 1, 2, 3 }, RepeatHorizontal(IntCell(), separatedBy: BlankColumns()).Map(space));
    }

    [Fact]
    public void RepeatHorizontal_WhenTheNextItemDoesNotFit_Stops()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4, 5 } });

      Assert.Equal(new[] { 2, 2 }, RepeatHorizontal(Range(2, 1, b => b.Width)).Map(space));
    }

    // --- Repeat as a shape ------------------------------------------------------------------------------------

    [Fact]
    public void Repeat_SizesItselfFromWhatItConsumed()
    {
      var space = Grid(new[,] { { 1 }, { 0 }, { 2 } });

      var applied = Repeat(IntCell(), separatedBy: BlankRows()).Apply(space);

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void Repeat_ComposesInsideAStack()
    {
      var space = Grid(new[,] { { 9 }, { 1 }, { 2 }, { 3 } });

      var shape = VerticalFlow(v =>
        $"{v.Next(IntCell().Named("total"))}|{string.Join(",", v.Next(Repeat(IntCell()).Named("items")))}");

      Assert.Equal("9|1,2,3", shape.Map(space));
    }

    [Fact]
    public void Repeat_OfAComposedItem_YieldsOneValuePerBlock()
    {
      // The canonical repeating-block shape in miniature: a code cell over a table, blank separated.
      var space = Mixed(new object?[,]
      {
        { "A-1", null },
        { "Name", "Amount" },
        { "Acme", 10 },
        { "Beta", 20 },
        { null, null },
        { "A-2", null },
        { "Name", "Amount" },
        { "Gamma", 30 },
      });

      var block = VerticalFlow(v => (
        Code: v.Next(Cell(c => c.GetString()).Named("code")),
        Amounts: v.Next(TableRows(r => r["Amount"].GetInt()).Named("amounts"))))
        .Named("block");

      var blocks = Repeat(block, separatedBy: BlankRows()).Map(space);

      Assert.Equal(2, blocks.Count);
      Assert.Equal("A-1", blocks[0].Code);
      Assert.Equal(new[] { 10, 20 }, blocks[0].Amounts);
      Assert.Equal("A-2", blocks[1].Code);
      Assert.Equal(new[] { 30 }, blocks[1].Amounts.ToArray());
    }
  }
}
