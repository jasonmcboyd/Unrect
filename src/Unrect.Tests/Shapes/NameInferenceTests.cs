using System.Collections.Generic;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// What a child is called in a message. Three rungs, first one that applies: the shape's own
  /// <c>.Named</c>; the bare identifier the argument was written as; the shape's description plus
  /// its 1-based position in the declaration.
  /// <para>
  /// The label belongs to the use site rather than to the shape, so the same shape used twice gets
  /// two labels and its own <c>Name</c> stays null. It names the subject as well as the path —
  /// anything less would have one message call the same child two different things.
  /// </para>
  /// </summary>
  public class NameInferenceTests
  {
    private static ISpace Ladder() => Grid(new[,] { { 1 }, { 2 }, { 3 } });

    private static IShape<int> Number() => Cell(c => c.GetInt());

    /// <summary>A shape that always fails, so every test reads its label off the failure.</summary>
    private static IShape<string> Text() => Cell(c => c.GetString());

    private static ShapeException Failure<T>(IShape<T> shape) => Assert.Throws<ShapeException>(() => shape.Map(Ladder()));

    // --- The three rungs ---------------------------------------------------------------------------

    [Fact]
    public void Rung1_AnExplicitNameWins()
    {
      var transactions = Text();

      var failure = Failure(VerticalFlow(v => $"{v.Next(Number())}{v.Next(transactions.Named("summary"))}"));

      Assert.Equal("'summary'", failure.Subject);
      Assert.Equal("VerticalFlow -> 'summary' (Cell)", failure.Path);
    }

    [Fact]
    public void Rung2_ABareIdentifierBecomesTheLabel()
    {
      // The name the user already wrote, reused. It is rendered verbatim: the point of the segment
      // is to lead a reader back to the line that produced it.
      var transactions = Text();

      var failure = Failure(VerticalFlow(v => $"{v.Next(Number())}{v.Next(transactions)}"));

      Assert.Equal("'transactions'", failure.Subject);
      Assert.Equal("VerticalFlow -> 'transactions' (Cell)", failure.Path);
    }

    [Fact]
    public void Rung2_DoesNotNameTheShape()
    {
      // The label is the use site's, not the shape's, which is what lets one shape be two things.
      var transactions = Text();

      Failure(VerticalFlow(v => $"{v.Next(Number())}{v.Next(transactions)}"));

      Assert.Null(transactions.Name);
    }

    [Fact]
    public void Rung3_AnythingElseIsDescriptionAndOrdinal()
    {
      // An inline factory call has no identifier to borrow, so the child is named by what it is and
      // where it sits — 1-based, because it is a position in a declaration a human wrote.
      var failure = Failure(VerticalFlow(v => $"{v.Next(Number())}{v.Next(Cell(c => c.GetString()))}"));

      Assert.Equal("Cell#2", failure.Subject);
      Assert.Equal("VerticalFlow -> Cell#2", failure.Path);
    }

    [Fact]
    public void Rung3_CoversMemberAccessAndCalls()
    {
      // Neither is a bare identifier, so neither is mistaken for a name the user chose.
      var shapes = new Shapes();

      Assert.Equal("Cell#1", Failure(VerticalFlow(v => v.Next(shapes.Total))).Subject);
      Assert.Equal("Cell#1", Failure(VerticalFlow(v => v.Next(Pick()))).Subject);
    }

    [Fact]
    public void ANameBeatsAnIdentifierWhichBeatsAnOrdinal()
    {
      var labelled = Text().Named("chosen");
      var identified = Text();

      Assert.Equal("'chosen'", Failure(VerticalFlow(v => v.Next(labelled))).Subject);
      Assert.Equal("'identified'", Failure(VerticalFlow(v => v.Next(identified))).Subject);
      Assert.Equal("Cell#1", Failure(VerticalFlow(v => v.Next(Cell(c => c.GetString())))).Subject);
    }

    // --- Ordinals ---------------------------------------------------------------------------------------

    [Fact]
    public void OrdinalsCountEveryChildIncludingNamedOnes()
    {
      // Naming one child must never renumber the others, or a path would change meaning when an
      // unrelated line gained a name.
      var second = Number();

      var failure = Failure(VerticalFlow(v =>
        $"{v.Next(Number())}{v.Next(second)}{v.Next(Cell(c => c.GetString()))}"));

      Assert.Equal("Cell#3", failure.Subject);
    }

    // --- The use site, not the shape -----------------------------------------------------------------------

    [Fact]
    public void TheSameShapeUsedTwiceGetsTwoLabels()
    {
      // One instance, two well-named locals, two segments — the whole reason the label lives at the
      // use site.
      var shared = Text();
      var gross = shared;
      var net = shared;

      Assert.Equal("'gross'", Failure(VerticalFlow(v => v.Next(gross))).Subject);
      Assert.Equal("'net'", Failure(VerticalFlow(v => v.Next(net))).Subject);
    }

    [Fact]
    public void ALabelPassesThroughTransparentWrappers()
    {
      // Select, Padded and Until add no segment of their own, so the label travels through them and
      // lands on the shape that does render one — one segment carrying the use site's name, not a
      // wrapper segment followed by an anonymous child.
      var selected = Text().Select(s => s);
      var padded = Text().Padded(0);
      var bounded = Text().Until(RowContaining("Nothing here"), orEnd: true);

      Assert.Equal("VerticalFlow -> 'selected' (Cell)", Failure(VerticalFlow(v => v.Next(selected))).Path);
      Assert.Equal("VerticalFlow -> 'padded' (Cell)", Failure(VerticalFlow(v => v.Next(padded))).Path);
      Assert.Equal("VerticalFlow -> 'bounded' (Cell)", Failure(VerticalFlow(v => v.Next(bounded))).Path);
      Assert.Equal("'selected'", Failure(VerticalFlow(v => v.Next(selected))).Subject);
    }

    // --- Both kinds of layout -------------------------------------------------------------------------------

    [Fact]
    public void TheLadderIsTheSameInAnOverlay()
    {
      var transactions = Text();

      var identified = Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(Number())}{o.Next(transactions)}").Map(Ladder()));

      var ordinal = Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(Number())}{o.Next(Cell(c => c.GetString()))}").Map(Ladder()));

      Assert.Equal("Overlay -> 'transactions' (Cell)", identified.Path);
      Assert.Equal("Overlay -> Cell#2", ordinal.Path);
    }

    // --- What the ladder does not touch ------------------------------------------------------------------------

    [Fact]
    public void RepeatKeepsItsOwnIndexAndItsItemKeepsItsDescription()
    {
      // Inference applies to Next only; capturing factory arguments is deferred. A repeat's index is
      // a coordinate into data and stays 0-based in brackets, beside a 1-based ordinal in hashes.
      var items = Repeat(Text());

      var failure = Failure(VerticalFlow(v => $"{string.Join(",", v.Next(items))}"));

      Assert.Equal("VerticalFlow -> 'items'[0] -> Cell", failure.Path);
    }

    // --- Capture reaches a repeat's item as well as a Next call -------------------------------------------------
    //
    // Repeat and RepeatHorizontal capture their item argument the same way Next captures a child,
    // through the same ladder. Two things about the rendering are worth pinning because both were
    // discovered rather than designed: the index stays on the repeat's own segment, and a repeat's
    // item has no ordinal to fall back on — it is *the* item, not the nth child.

    [Fact]
    public void Rung2_ARepeatsItemIsLabelledByTheLocalItWasHoistedInto()
    {
      // The index decorates the repeat, the label lands on the item — which is how a named item has
      // always rendered. Capture only changes which rung supplies that segment's text.
      var investorDetail = Text();

      var failure = Failure(Repeat(investorDetail));

      Assert.Equal("'investorDetail'", failure.Subject);
      Assert.Equal("Repeat[0] -> 'investorDetail' (Cell)", failure.Path);
    }

    [Fact]
    public void Rung1_AnExplicitNameStillBeatsTheItemsIdentifier()
    {
      var chosen = Text().Named("explicit");

      Assert.Equal("Repeat[0] -> 'explicit' (Cell)", Failure(Repeat(chosen)).Path);
    }

    [Fact]
    public void Rung3_ARepeatsItemFallsStraightThroughToItsDescription()
    {
      // No ordinal: there is only ever one item, so there is nothing to count. An inline item, a
      // call, and a modifier chain are all "not a bare identifier" and all render the same way.
      var block = Text();

      Assert.Equal("Repeat[0] -> Cell", Failure(Repeat(Cell(c => c.GetString()))).Path);
      Assert.Equal("Repeat[0] -> Cell", Failure(Repeat(MakeBlock())).Path);
      Assert.Equal("Repeat[0] -> Cell", Failure(Repeat(block.Down(1))).Path);
    }

    [Fact]
    public void RepeatHorizontal_CapturesItsItemIdentically()
    {
      var detail = Text();

      var failure = Assert.Throws<ShapeException>(() =>
        RepeatHorizontal(detail).Map(Grid(new[,] { { 1, 2 } })));

      Assert.Equal("RepeatHorizontal[0] -> 'detail' (Cell)", failure.Path);
    }

    [Fact]
    public void ARepeatAndItsItemAreLabelledIndependently()
    {
      // Two use sites, two labels: the flow's Next names the repeat, the repeat's factory names the
      // item, and the index sits between them on the repeat it belongs to.
      var space = Mixed(new object?[,] { { "a" }, { null }, { "b" }, { null }, { 9 } });

      var detail = Text();
      var details = Repeat(detail, separatedBy: BlankRows());

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => string.Join(",", v.Next(details))).Map(space));

      Assert.Equal("VerticalFlow -> 'details'[2] -> 'detail' (Cell)", failure.Path);
    }

    [Fact]
    public void AnItemsLabelDoesNotReachTheItemsOwnChildren()
    {
      // The label belongs to the item's segment and stops there; what is inside the item is named
      // by its own ladder, at its own use sites.
      var inner = Text();
      var detailFlow = VerticalFlow(w => $"{w.Next(Cell(c => c.GetInt()))}{w.Next(inner)}");
      var blocks = Repeat(detailFlow);

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => string.Join(",", v.Next(blocks))).Map(Ladder()));

      Assert.Equal("VerticalFlow -> 'blocks'[0] -> 'detailFlow' -> 'inner' (Cell)", failure.Path);
      Assert.Equal("'inner'", failure.Subject);
    }

    [Fact]
    public void NamedArgumentsStillBindWithCapturePresent()
    {
      // The captured parameter is last and optional, so the optional arguments a caller was already
      // passing by name keep binding to what they always did.
      var space = Mixed(new object?[,] { { "a" }, { null }, { "b" } });

      var detail = Text();

      Assert.Equal(new[] { "a", "b" }, Repeat(detail, separatedBy: BlankRows(), atLeast: 1).Map(space));
      Assert.Equal(new[] { "a", "b" }, Repeat(detail, separatedBy: BlankRows()).Map(space));
      Assert.Equal(new[] { "a", "b" }, Repeat(detail, atLeast: 1, separatedBy: BlankRows()).Map(space));
    }

    // --- The helper rule §6.1 makes a rule rather than advice -----------------------------------------------------

    [Fact]
    public void AHelperThatNamesWhatItReturnsDefeatsTheLadderAtEveryUseSite()
    {
      // Rung 1 wins wherever the shape goes, so a helper that names its result calls every use site
      // the same thing — which is exactly what the use-site ladder exists to avoid.
      var captions = NamedFullRow();
      var totals = NamedFullRow();

      Assert.Equal("'full row'", Failure(VerticalFlow(v => v.Next(captions))).Subject);
      Assert.Equal("'full row'", Failure(VerticalFlow(v => v.Next(totals))).Subject);
    }

    [Fact]
    public void AHelperThatLeavesNamingToTheUseSiteGetsTwoNames()
    {
      var captions = FullRow();
      var totals = FullRow();

      Assert.Equal("'captions'", Failure(VerticalFlow(v => v.Next(captions))).Subject);
      Assert.Equal("'totals'", Failure(VerticalFlow(v => v.Next(totals))).Subject);
    }

    private static IShape<string> FullRow() => Cell(c => c.GetString());

    private static IShape<string> NamedFullRow() => Cell(c => c.GetString()).Named("full row");

    private static IShape<string> Pick() => Cell(c => c.GetString());

    private static IShape<string> MakeBlock() => Cell(c => c.GetString());

    private sealed class Shapes
    {
      public IShape<string> Total { get; } = Cell(c => c.GetString());
    }
  }
}
