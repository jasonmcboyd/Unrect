using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// What a child is called in a message. Three rungs, first one that applies: the projection's own
  /// <c>.Named</c>; the bare identifier the argument was written as; the projection's description
  /// plus its 1-based position in the declaration.
  /// <para>
  /// The label belongs to the use site rather than to the projection, so the same projection used
  /// twice gets two labels and its own <c>Name</c> stays null. It names the subject as well as the
  /// path — anything less would have one message call the same child two different things.
  /// </para>
  /// <para>
  /// <b>What rung 3 falls back to changed in phase 6, and it is not this file's law that changed.</b>
  /// The description is the factory that produced the projection, and there is no longer a factory
  /// called <c>Cell</c>: the untyped leaf retired for <c>Point()</c>, and what these tests read is a
  /// KINDED leaf, so the fallback renders <c>Text</c>, <c>Integer</c>, <c>Decimal</c> — the leaf's
  /// own name. Every <c>Cell</c> and <c>Cell#2</c> below is now its leaf's name and its ordinal, and
  /// the ladder above it — a name beats an identifier beats a description-and-ordinal — is untouched.
  /// A site written as <c>Point().Select(…)</c> renders <c>Select</c>, because that is the factory
  /// that made the projection the use site was handed.
  /// </para>
  /// </summary>
  public class NameInferenceTests
  {
    private static IProjection<ISheetCells, int> Number() => IntCell();

    /// <summary>A projection that always fails, so every test reads its label off the failure.</summary>
    private static IProjection<ISheetCells, string> Text() => TextCell();

    private static ProjectionException Failure<T>(IProjection<ISheetCells, T> projection) => Assert.Throws<ProjectionException>(() => projection.Map(Ladder()));

    // --- The three rungs ---------------------------------------------------------------------------

    [Fact]
    public void Rung1_AnExplicitNameWins()
    {
      var transactions = Text();

      var failure = Failure(VerticalFlow(v =>
      {
        var number = v.Next(Number());
        var transactions2 = v.Next(transactions.Named("summary"));

        return v.Build(read => $"{read.Of(number)}{read.Of(transactions2)}");
      }));

      Assert.Equal("'summary'", failure.Subject);
      Assert.Equal("VerticalFlow -> 'summary' (Text)", failure.Path);
    }

    [Fact]
    public void Rung2_ABareIdentifierBecomesTheLabel()
    {
      // The name the user already wrote, reused. It is rendered verbatim: the point of the segment
      // is to lead a reader back to the line that produced it.
      var transactions = Text();

      var failure = Failure(VerticalFlow(v =>
      {
        var number = v.Next(Number());
        var transactions2 = v.Next(transactions);

        return v.Build(read => $"{read.Of(number)}{read.Of(transactions2)}");
      }));

      Assert.Equal("'transactions'", failure.Subject);
      Assert.Equal("VerticalFlow -> 'transactions' (Text)", failure.Path);
    }

    [Fact]
    public void Rung2_DoesNotNameTheProjection()
    {
      // The label is the use site's, not the projection's, which is what lets one projection be two
      // things.
      var transactions = Text();

      Failure(VerticalFlow(v =>
      {
        var number = v.Next(Number());
        var transactions2 = v.Next(transactions);

        return v.Build(read => $"{read.Of(number)}{read.Of(transactions2)}");
      }));

      Assert.Null(transactions.Name);
    }

    [Fact]
    public void Rung3_AnythingElseIsDescriptionAndOrdinal()
    {
      // An inline factory call has no identifier to borrow, so the child is named by what it is and
      // where it sits — 1-based, because it is a position in a declaration a human wrote.
      var failure = Failure(VerticalFlow(v =>
      {
        var number = v.Next(Number());
        var textCell = v.Next(TextCell());

        return v.Build(read => $"{read.Of(number)}{read.Of(textCell)}");
      }));

      Assert.Equal("Text#2", failure.Subject);
      Assert.Equal("VerticalFlow -> Text#2", failure.Path);
    }

    [Fact]
    public void Rung3_CoversMemberAccessAndCalls()
    {
      // Neither is a bare identifier, so neither is mistaken for a name the user chose.
      var projections = new Projections();

      Assert.Equal("Text#1", Failure(VerticalFlow(v =>
      {
        var projections2 = v.Next(projections.Total);

        return v.Build(read => read.Of(projections2));
      })).Subject);
      Assert.Equal("Text#1", Failure(VerticalFlow(v =>
      {
        var pick = v.Next(Pick());

        return v.Build(read => read.Of(pick));
      })).Subject);
    }

    [Fact]
    public void ANameBeatsAnIdentifierWhichBeatsAnOrdinal()
    {
      var labelled = Text().Named("chosen");
      var identified = Text();

      Assert.Equal("'chosen'", Failure(VerticalFlow(v =>
      {
        var labelled2 = v.Next(labelled);

        return v.Build(read => read.Of(labelled2));
      })).Subject);
      Assert.Equal("'identified'", Failure(VerticalFlow(v =>
      {
        var identified2 = v.Next(identified);

        return v.Build(read => read.Of(identified2));
      })).Subject);
      Assert.Equal("Text#1", Failure(VerticalFlow(v =>
      {
        var textCell = v.Next(TextCell());

        return v.Build(read => read.Of(textCell));
      })).Subject);
    }

    // --- Ordinals ---------------------------------------------------------------------------------------

    [Fact]
    public void OrdinalsCountEveryChildIncludingNamedOnes()
    {
      // Naming one child must never renumber the others, or a path would change meaning when an
      // unrelated line gained a name.
      var second = Number();

      var failure = Failure(VerticalFlow(v =>
      {
        var number = v.Next(Number());
        var second2 = v.Next(second);
        var textCell = v.Next(TextCell());

        return v.Build(read => $"{read.Of(number)}{read.Of(second2)}{read.Of(textCell)}");
      }));

      Assert.Equal("Text#3", failure.Subject);
    }

    // --- The use site, not the projection -----------------------------------------------------------------------

    [Fact]
    public void TheSameProjectionUsedTwiceGetsTwoLabels()
    {
      // One instance, two well-named locals, two segments — the whole reason the label lives at the
      // use site.
      var shared = Text();
      var gross = shared;
      var net = shared;

      Assert.Equal("'gross'", Failure(VerticalFlow(v =>
      {
        var gross2 = v.Next(gross);

        return v.Build(read => read.Of(gross2));
      })).Subject);
      Assert.Equal("'net'", Failure(VerticalFlow(v =>
      {
        var net2 = v.Next(net);

        return v.Build(read => read.Of(net2));
      })).Subject);
    }

    [Fact]
    public void ALabelPassesThroughTransparentWrappers()
    {
      // Select, Padded and Until add no segment of their own, so the label travels through them and
      // lands on the projection that does render one — one segment carrying the use site's name,
      // not a wrapper segment followed by an anonymous child.
      var selected = Text().Select(s => s);
      var padded = Text().Padded(0);
      var bounded = Until(RowContaining("Nothing here"), orEnd: true).Of(Text());

      Assert.Equal("VerticalFlow -> 'selected' (Text)", Failure(VerticalFlow(v =>
      {
        var selected2 = v.Next(selected);

        return v.Build(read => read.Of(selected2));
      })).Path);
      Assert.Equal("VerticalFlow -> 'padded' (Text)", Failure(VerticalFlow(v =>
      {
        var padded2 = v.Next(padded);

        return v.Build(read => read.Of(padded2));
      })).Path);
      Assert.Equal("VerticalFlow -> 'bounded' (Text)", Failure(VerticalFlow(v =>
      {
        var bounded2 = v.Next(bounded);

        return v.Build(read => read.Of(bounded2));
      })).Path);
      Assert.Equal("'selected'", Failure(VerticalFlow(v =>
      {
        var selected2 = v.Next(selected);

        return v.Build(read => read.Of(selected2));
      })).Subject);
    }

    // --- Both kinds of layout -------------------------------------------------------------------------------

    [Fact]
    public void TheLadderIsTheSameInAnOverlay()
    {
      var transactions = Text();

      var identified = Assert.Throws<ProjectionException>(() =>
        Overlay(o =>
        {
          var number = o.Next(Number());
          var transactions2 = o.Next(transactions);

          return o.Build(read => $"{read.Of(number)}{read.Of(transactions2)}");
        }).Map(Ladder()));

      var ordinal = Assert.Throws<ProjectionException>(() =>
        Overlay(o =>
        {
          var number = o.Next(Number());
          var textCell = o.Next(TextCell());

          return o.Build(read => $"{read.Of(number)}{read.Of(textCell)}");
        }).Map(Ladder()));

      Assert.Equal("Overlay -> 'transactions' (Text)", identified.Path);
      Assert.Equal("Overlay -> Text#2", ordinal.Path);
    }

    // --- What the ladder does not touch ------------------------------------------------------------------------

    [Fact]
    public void RepeatKeepsItsOwnIndexAndItsItemKeepsItsDescription()
    {
      // Inference applies to Next only; capturing factory arguments is deferred. A repeat's index
      // is a coordinate into data and stays 0-based in brackets, beside a 1-based ordinal in
      // hashes.
      var items = VerticalRepeat(Text());

      var failure = Failure(VerticalFlow(v =>
      {
        var items2 = v.Next(items);

        return v.Build(read => $"{string.Join(",", read.Of(items2))}");
      }));

      Assert.Equal("VerticalFlow -> 'items'[0] -> Text", failure.Path);
    }

    // --- Capture reaches a repeat's item as well as a Next call -------------------------------------------------
    //
    // VerticalRepeat and HorizontalRepeat capture their item argument the same way Next captures
    // a child, through the same ladder. Two things about the rendering are worth pinning because
    // both were discovered rather than designed: the index stays on the repeat's own segment, and
    // a repeat's item has no ordinal to fall back on — it is *the* item, not the nth child.

    [Fact]
    public void Rung2_ARepeatsItemIsLabelledByTheLocalItWasHoistedInto()
    {
      // The index decorates the repeat, the label lands on the item — which is how a named item has
      // always rendered. Capture only changes which rung supplies that segment's text.
      var investorDetail = Text();

      var failure = Failure(VerticalRepeat(investorDetail));

      Assert.Equal("'investorDetail'", failure.Subject);
      Assert.Equal("VerticalRepeat[0] -> 'investorDetail' (Text)", failure.Path);
    }

    [Fact]
    public void Rung1_AnExplicitNameStillBeatsTheItemsIdentifier()
    {
      var chosen = Text().Named("explicit");

      Assert.Equal("VerticalRepeat[0] -> 'explicit' (Text)", Failure(VerticalRepeat(chosen)).Path);
    }

    [Fact]
    public void Rung3_ARepeatsItemFallsStraightThroughToItsDescription()
    {
      // No ordinal: there is only ever one item, so there is nothing to count. An inline item, a
      // call, and a modifier chain are all "not a bare identifier" and all render the same way.
      var block = Text();

      // Left as an inline lambda on purpose: the three spellings this test enumerates are an
      // inline lambda, an inline factory call and a modifier chain, so funnelling this one into
      // TextCell() would leave the first of them untested.
      Assert.Equal("VerticalRepeat[0] -> Text", Failure(VerticalRepeat(Text())).Path);
      Assert.Equal("VerticalRepeat[0] -> Text", Failure(VerticalRepeat(MakeBlock())).Path);
      Assert.Equal("VerticalRepeat[0] -> Text", Failure(VerticalRepeat(Down(1).Of(block))).Path);
    }

    [Fact]
    public void HorizontalRepeat_CapturesItsItemIdentically()
    {
      var detail = Text();

      var failure = Assert.Throws<ProjectionException>(() =>
        HorizontalRepeat(detail).Map(Grid(new[,] { { 1, 2 } })));

      Assert.Equal("HorizontalRepeat[0] -> 'detail' (Text)", failure.Path);
    }

    [Fact]
    public void ARepeatAndItsItemAreLabelledIndependently()
    {
      // Two use sites, two labels: the flow's Next names the repeat, the repeat's factory names the
      // item, and the index sits between them on the repeat it belongs to.
      var space = Mixed(new object?[,] { { "a" }, { null }, { "b" }, { null }, { 9 } });

      var detail = Text();
      var details = VerticalRepeat(detail, separatedBy: BlankRows());

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          var details2 = v.Next(details);

          return v.Build(read => string.Join(",", read.Of(details2)));
        }).Map(space));

      Assert.Equal("VerticalFlow -> 'details'[2] -> 'detail' (Text)", failure.Path);
    }

    [Fact]
    public void AnItemsLabelDoesNotReachTheItemsOwnChildren()
    {
      // The label belongs to the item's segment and stops there; what is inside the item is named
      // by its own ladder, at its own use sites.
      var inner = Text();
      var detailFlow = VerticalFlow(w =>
      {
        var intCell = w.Next(IntCell());
        var inner2 = w.Next(inner);

        return w.Build(read => $"{read.Of(intCell)}{read.Of(inner2)}");
      });
      var blocks = VerticalRepeat(detailFlow);

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          var blocks2 = v.Next(blocks);

          return v.Build(read => string.Join(",", read.Of(blocks2)));
        }).Map(Ladder()));

      Assert.Equal("VerticalFlow -> 'blocks'[0] -> 'detailFlow' -> 'inner' (Text)", failure.Path);
      Assert.Equal("'inner'", failure.Subject);
    }

    [Fact]
    public void NamedArgumentsStillBindWithCapturePresent()
    {
      // The captured parameter is last and optional, so the optional arguments a caller was already
      // passing by name keep binding to what they always did.
      var space = Mixed(new object?[,] { { "a" }, { null }, { "b" } });

      var detail = Text();

      Assert.Equal(new[] { "a", "b" }, VerticalRepeat(detail, separatedBy: BlankRows(), atLeast: 1).Map(space));
      Assert.Equal(new[] { "a", "b" }, VerticalRepeat(detail, separatedBy: BlankRows()).Map(space));
      Assert.Equal(new[] { "a", "b" }, VerticalRepeat(detail, atLeast: 1, separatedBy: BlankRows()).Map(space));
    }

    // --- Capture reaches a fallback as well -----------------------------------------------------------------------
    //
    // Else(fallback) captures its argument the same way Next and Repeat do, so a stand-in that
    // fails names itself rather than hiding behind its bare kind.

    [Fact]
    public void Rung2_AFallbackIsLabelledByTheLocalItWasHoistedInto()
    {
      // Both halves failed, so the fallback owns the failure — and it is the fallback's own
      // identifier that says which stand-in was being read when the parse gave up.
      var primary = Text().Named("primary");
      var recovery = Point().Select(p => p.Date().ToString());

      var failure = Failure(primary.Else(recovery));

      Assert.Equal("'recovery'", failure.Subject);
      Assert.Equal("'recovery' (Select)", failure.Path);
      Assert.Contains("it stands in for 'primary', which failed too: ", failure.Message);
    }

    [Fact]
    public void ASucceedingFallbackLeavesTheWarningNamingThePrimary()
    {
      // The label is for the failure the fallback might raise. When it succeeds there is nothing to
      // blame it for, and the Warning still names the projection that actually went wrong.
      var primary = Text().Named("primary");
      var recovery = Point().Select(p => p.Integer().ToString());

      var result = primary.Else(recovery).MapWithDiagnostics(Ladder());

      Assert.Equal("1", result.Value);

      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal("'primary'", warning.Subject);
      Assert.Equal("'primary' (Text)", warning.Path);
    }

    [Fact]
    public void Rung3_AnInlineFallbackFallsThroughToItsDescription()
    {
      // Like a repeat's item, a fallback is *the* fallback rather than the nth child, so there is
      // no ordinal to count and rung 3 is the bare description.
      var primary = Text().Named("primary");

      var failure = Failure(primary.Else(Point().Select(p => p.Date().ToString())));

      Assert.Equal("Select", failure.Subject);
      Assert.Equal("Select", failure.Path);
    }

    // --- The helper rule §6.1 makes a rule rather than advice -----------------------------------------------------

    [Fact]
    public void AHelperThatNamesWhatItReturnsDefeatsTheLadderAtEveryUseSite()
    {
      // Rung 1 wins wherever the projection goes, so a helper that names its result calls every use
      // site the same thing — which is exactly what the use-site ladder exists to avoid.
      var captions = NamedFullRow();
      var totals = NamedFullRow();

      Assert.Equal("'full row'", Failure(VerticalFlow(v =>
      {
        var captions2 = v.Next(captions);

        return v.Build(read => read.Of(captions2));
      })).Subject);
      Assert.Equal("'full row'", Failure(VerticalFlow(v =>
      {
        var totals2 = v.Next(totals);

        return v.Build(read => read.Of(totals2));
      })).Subject);
    }

    [Fact]
    public void AHelperThatLeavesNamingToTheUseSiteGetsTwoNames()
    {
      var captions = FullRow();
      var totals = FullRow();

      Assert.Equal("'captions'", Failure(VerticalFlow(v =>
      {
        var captions2 = v.Next(captions);

        return v.Build(read => read.Of(captions2));
      })).Subject);
      Assert.Equal("'totals'", Failure(VerticalFlow(v =>
      {
        var totals2 = v.Next(totals);

        return v.Build(read => read.Of(totals2));
      })).Subject);
    }

    // --- A composition presented as one node climbs the same ladder --------------------------------------------
    //
    // Table(headerRows:, eachRow:) is built from the primitives — a ColumnLabels header over a
    // VerticalBands body — and every part of that is marked scaffolding, so the ladder reads the
    // table as ONE child. These pin the three rungs over it, which is what says the composition is
    // an implementation detail rather than a shape the user has to know about.

    [Fact]
    public void Rung2_AComposedTableIsLabelledByTheLocalItWasHoistedInto()
    {
      // The failure is the table's own placement, so the table is the deepest segment and earns the
      // kind suffix — the same rendering a leaf gets, over a projection that is really four.
      var sheet = Mixed(new object?[,] { { "Investor" }, { 10m } });
      var transactions = Down(5).Of(Table(1, Decimal()));

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalFlow(v =>
        {
          var transactions2 = v.Next(transactions);

          return v.Build(read => string.Join(",", read.Of(transactions2)));
        }).Map(sheet));

      Assert.Equal("VerticalFlow -> 'transactions' (Table)", failure.Path);
      Assert.Equal("'transactions'", failure.Subject);
    }

    [Fact]
    public void AFailureInsideAComposedTableKeepsTheTablesSegmentAndItsRecordIndex()
    {
      // The band's occurrence index rides up onto the table's own segment, because the tiler that
      // carried it is scaffolding. FullPath is where the parts are still readable.
      var sheet = Mixed(new object?[,] { { "Investor" }, { "oops" } });
      var transactions = Table(1, Decimal());

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalFlow(v =>
        {
          var transactions2 = v.Next(transactions);

          return v.Build(read => string.Join(",", read.Of(transactions2)));
        }).Map(sheet));

      Assert.Equal("VerticalFlow -> 'transactions'[0] -> Decimal", failure.Path);
      Assert.Equal("VerticalFlow -> 'transactions' -> UnderColumnLabels -> VerticalBands#2[0] -> Decimal", failure.FullPath);
    }

    [Fact]
    public void Rung3_AnUnnamedComposedTableIsItsKindAndItsPosition()
    {
      // No identifier to borrow, so the table is the second child of the flow and says so — and the
      // ordinal counts the table once, not the header and the tiler it was built from.
      var sheet = Mixed(new object?[,]
      {
        { "Title", null },
        { "Investor", "Amount" },
        { "Acme", "oops" },
      });

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalFlow(v =>
        {
          var textSlot = v.Next(Text());
          var table = v.Next(Table(1, Decimal()));

          return v.Build(read => $"{read.Of(textSlot)}|{string.Join(",", read.Of(table))}");
        }).Map(sheet));

      Assert.Equal("VerticalFlow -> Table#2[0] -> Decimal", failure.Path);
    }

    private static IProjection<ISheetCells, string> FullRow() => TextCell();

    private static IProjection<ISheetCells, string> NamedFullRow() => TextCell().Named("full row");

    private static IProjection<ISheetCells, string> Pick() => TextCell();

    private static IProjection<ISheetCells, string> MakeBlock() => TextCell();

    private sealed class Projections
    {
      public IProjection<ISheetCells, string> Total { get; } = TextCell();
    }
  }
}
