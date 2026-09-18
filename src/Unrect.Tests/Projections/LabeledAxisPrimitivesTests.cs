using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
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
  /// Step 2 of labelled axes: the built-in <c>Table</c> is reimplementable from the three public
  /// primitives — <c>ColumnLabels(int)</c> manufactures a <see cref="LabelMap"/>,
  /// <c>WithColumnLabels&lt;T&gt;(LabelMap, IProjectionDefinition&lt;TSpace, T&gt;)</c> pushes it as the ambient
  /// column labels, and <c>Record&lt;T&gt;(Func&lt;TableRow&lt;TSpace&gt;, T&gt;)</c> reads a row by name
  /// through the pushed scope. The acceptance claim is byte-identity of the reading's <em>value</em>,
  /// its failure <em>message</em> and <em>A1 location</em>, and — since GAP C closed in step 2 — its
  /// <see cref="TableRow{TSpace}.Index"/>. GAP A is closed: the body tiler walks the declared block a band
  /// at a time, so the composition streams in step with the leaf. GAP B remains the one documented
  /// divergence, captured not asserted equal: path/subject reflect the primitive tree, not a flat
  /// <c>Table</c>.
  /// </summary>
  public class LabeledAxisPrimitivesTests
  {
    // --- The reimplementation, built from the PUBLIC primitives ------------------------------------
    //
    // The composition: a VerticalFlow of
    // ColumnLabels then WithColumnLabels(columns, VerticalBands(1, Record(record))). One row per
    // record is the tiler's business, not a pattern repeat's — a record declares its own one-row band
    // and so has no shape to discover. It is dressed with the built-in table's own placement —
    // skip-to-first-non-blank-cell over a discovered block — and its "Table" description, through the
    // internal FlowDefinition, which is precisely what step 3 will do when the bespoke path is
    // deleted. Constructed in the test project because the primitives it composes
    // are public and the assembling FlowDefinition/Placement are reachable through InternalsVisibleTo.

    internal static IProjectionDefinition<ISheetCells, IReadOnlyList<T>> TableFromPrimitives<T>(int headerRows, Func<TableRow<ISheetCells>, T> record)
      => PrimitiveTable(headerRows, record, marked: false);

    /// <summary>
    /// The composition. <paramref name="marked"/> marks every part it assembles scaffolding, which
    /// is what a factory does to pieces the caller never wrote — the caller hands a
    /// <c>Func&lt;TableRow, T&gt;</c>, so the header, the tiler and the record are all this
    /// method's. Unmarked, nothing folds and a rendered path is the whole tree.
    /// </summary>
    private static IProjectionDefinition<ISheetCells, IReadOnlyList<T>> PrimitiveTable<T>(int headerRows, Func<TableRow<ISheetCells>, T> record, bool marked)
      => new LabelledDefinition<ISheetCells, IReadOnlyList<T>>(
        LabelAxis.Column,
        Mark(ColumnLabels(headerRows), marked),
        Mark(VerticalBands(1, Mark(Record(record), marked)), marked),
        TablePlacementReplica(),
        "Table");

    private static IProjectionDefinition<ISheetCells, T> Mark<T>(IProjectionDefinition<ISheetCells, T> part, bool marked) => marked ? part.AsScaffolding() : part;

    /// <summary>
    /// A hand copy of the private <c>Projection.TablePlacement()</c> — skip to the first non-blank
    /// cell (down to the first content row, then across to its first non-blank column) over a
    /// discovered block. The block is what bounds the body: the tiler declares no extent of its own
    /// and runs exactly as far as what places it, so the block is the composition's own terminator.
    /// </summary>
    private static Placement TablePlacementReplica()
      => new Placement(
        OffsetStrategies.SkipToFirstNonBlankCell(),
        RowStrategies.TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue());

    // --- The record read, shared by both spellings -------------------------------------------------
    //
    // Reading Index into the value bakes GAP C's parity into the value facet: an occurrence number
    // that disagreed between the two Tables would show up as a different Line. Investor and Amount are
    // resolved by name through the ambient scope, which is the whole of what the primitives provide.

    private sealed record Line(int Index, string Investor, decimal Amount);

    private static Line ReadLine(TableRow<ISheetCells> row) => new Line(row.Index, row["Investor"].Text(), row["Amount"].Decimal());

    // --- The sheet set (spec §5.4) -----------------------------------------------------------------

    /// <summary>(1) A flat, well-formed table — the case where laziness is preserved.</summary>
    private static ISheetCells Flat() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10m },
      { "Beta", 20m },
    });

    /// <summary>(2) A table missing the "Investor" column the record reads — an absent-column failure.</summary>
    private static ISheetCells AbsentColumn() => Mixed(new object?[,]
    {
      { "Client", "Amount" },
      { "Acme", 10m },
    });

    /// <summary>(3) A table carrying "Amount" twice — an ambiguous-column failure.</summary>
    private static ISheetCells AmbiguousColumn() => Mixed(new object?[,]
    {
      { "Investor", "Amount", "Amount" },
      { "Acme", 10m, 11m },
    });

    /// <summary>(4) The table shifted one column right — column origin &gt; 0, the translation case.</summary>
    private static ISheetCells OffsetColumn() => Mixed(new object?[,]
    {
      { null, "Investor", "Amount" },
      { null, "Acme", 10m },
      { null, "Beta", 20m },
    });

    /// <summary>(5) Two blank-separated blocks, each with its own header — per-occurrence scope.</summary>
    private static ISheetCells Repeated() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10m },
      { "Beta", 20m },
      { null, null },
      { "Investor", "Amount" },
      { "Gamma", 30m },
    });

    /// <summary>(6) A table with trailing content past a blank row — forces the discovered block (GAP A).</summary>
    private static ISheetCells Trailing() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10m },
      { "Beta", 20m },
      { null, null },
      { "Total", 30m },
    });

    /// <summary>(7) A body cell of the wrong kind — text where the record reads a decimal.</summary>
    private static ISheetCells KindMismatch() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", "oops" },
    });

    // --- 1. The differential: value + message + A1 byte-identical over the sheet set ---------------
    //
    // AssertL1 is exactly the level the spec names: it compares the value (rendered deeply, so a
    // nested Line's Index is in it) and the failure's L1 identity (Problem + A1 location + fault +
    // inner), and NOT the path, the subject, or the diagnostics — the facets GAP B moves. Each sheet
    // wraps the two Tables identically, so the only thing that can differ is the reimplementation.

    [Fact]
    public void AFlatTableReadsIdentically()
      => SameReading(Table(1, ReadLine), TableFromPrimitives(1, ReadLine), Flat());

    [Fact]
    public void AnAbsentColumnFailsIdentically()
      => SameReading(Table(1, ReadLine), TableFromPrimitives(1, ReadLine), AbsentColumn());

    [Fact]
    public void AnAmbiguousColumnFailsIdentically()
      => SameReading(Table(1, ReadLine), TableFromPrimitives(1, ReadLine), AmbiguousColumn());

    [Fact]
    public void AColumnOffsetTableReadsIdentically()
      => SameReading(
        Right(1).Of(Table(1, ReadLine)),
        Right(1).Of(TableFromPrimitives(1, ReadLine)),
        OffsetColumn());

    [Fact]
    public void ADefaultTableNowSelfLocatesOntoAColumnIndentedRegion()
    {
      // Table's
      // default offset is SkipToFirstNonBlankCell, so a table whose content starts past column 0 reads
      // with Table(...) ALONE — no explicit Right(1)/offset. Before the change the default
      // SkipBlankRows landed the origin at column 0, DiscoveredBlock's TakeColumnsWhileAnyValue took a
      // 0-wide leading block (column A is entirely blank), and the by-name read of "Investor"/"Amount"
      // failed for want of columns. Contrast AColumnOffsetTableReadsIdentically above, which still
      // spells the Right(1) override: on this sheet that spelling is now redundant, not required.
      IReadOnlyList<Line> lines = Table(1, ReadLine).Map(OffsetColumn());

      Assert.Equal(new[] { new Line(0, "Acme", 10m), new Line(1, "Beta", 20m) }, lines);
    }

    [Fact]
    public void PerOccurrenceHeadersUnderAnOuterRepeatReadIdentically()
    {
      var bespoke = VerticalRepeat(Table(1, ReadLine), separatedBy: BlankRows());
      var primitives = VerticalRepeat(TableFromPrimitives(1, ReadLine), separatedBy: BlankRows());

      SameReading(bespoke, primitives, Repeated());
    }

    [Fact]
    public void ATableWithTrailingContentReadsTheSameValue()
      => SameReading(Table(1, ReadLine), TableFromPrimitives(1, ReadLine), Trailing());

    [Fact]
    public void AKindMismatchInABodyCellFailsIdentically()
    {
      // The SENTENCE is the claim, and it is byte-identical: both trees route the read through the
      // one shared binder, so both describe the offending cell in the same words with the same A1
      // inside them.
      //
      // Not the extent citation, which is where the two trees legitimately part. The bespoke rung
      // calls the record lambda inline, so the failure is caught by the TABLE and located at the
      // table's own corner (A1); the primitive composition catches it per BAND, so it is located at
      // the failing band's corner (A2). Each is right about the declaration it belongs to — this is
      // the same divergence GapB_… documents, seen from the other side, and the reason SameReading
      // (whose L1 facet compares the whole rendered failure, extent citation included) is not the
      // harness for this one case.
      var sheet = KindMismatch();

      var bespoke = Assert.Throws<ProjectionException>(() => Table(1, ReadLine).Map(sheet));
      var primitives = Assert.Throws<ProjectionException>(() => TableFromPrimitives(1, ReadLine).Map(sheet));

      Assert.Equal("expected Number at B2, found Text", bespoke.Problem);
      Assert.Equal(bespoke.Problem, primitives.Problem);
    }

    // --- 2. GAP B — path/subject reflect the primitive tree, captured not asserted equal -----------
    //
    // The spec is explicit: message and A1 are identical (asserted), path and subject are not (GAP B).
    // The negative-pin rule forbids a blanket "these differ" — so the actual strings are documented as
    // specific asserts. Bespoke reports one flat `Table` segment; the reimplementation's failure sits
    // inside the VerticalBands the primitives compose (the transparent WithColumnLabels is skipped,
    // and the FlowDefinition's "Table" description keeps the outer segment reading `Table`).

    [Fact]
    public void GapB_TheMessageAndA1AgreeButThePathAndSubjectReflectTheDifferentTrees()
    {
      var sheet = KindMismatch();

      var bespoke = Assert.Throws<ProjectionException>(() => Table(1, ReadLine).Map(sheet));
      var primitives = Assert.Throws<ProjectionException>(() => TableFromPrimitives(1, ReadLine).Map(sheet));

      // Asserted equal — the acceptance claim, and since phase 6 it is the whole of it: both speak
      // the compute-legal binder's sentence, with the offending cell's own A1 inside the sentence
      // rather than left to the citation beside it.
      Assert.Equal(bespoke.Problem, primitives.Problem);
      Assert.Contains("at B2", bespoke.Problem);

      // The extent citation is NOT equal, and that moved here from the acceptance claim above. The
      // bespoke rung calls the record inline, so the table catches the read and cites its own corner
      // (A1); the primitive composition catches it per band and cites the band's (A2). Both are
      // right about their own tree, which is exactly what GAP B says, so the two are pinned
      // separately rather than one of them being bent to match the other.
      Assert.Equal("A1", bespoke.Location.A1);
      Assert.Equal("A2", primitives.Location.A1);

      // Documented divergence (GAP B). The exact strings, so a change to either tree is caught here
      // rather than sliding under a "they differ" that would pass forever after the first drift. The
      // built-in Table's Func rung calls the record inline, so its failure blames the flat Table; the
      // reimplementation's failure sits inside the VerticalBands the primitives compose — the "Table"
      // description on the FlowDefinition keeps the outer segment reading Table, and the transparent
      // WithColumnLabels is skipped, so the tree between them is VerticalBands -> Record.
      Assert.Equal("Table", bespoke.Path);
      Assert.Equal("Table", bespoke.Subject);
      Assert.Equal("Table -> VerticalBands#2[0] -> Record", primitives.Path);
      Assert.Equal("Record", primitives.Subject);
    }

    // --- 2b. .AsUnit — the composition folds to one named unit in the collapsed path --------------
    //
    // The same reimplementation with its parts marked scaffolding and the whole marked .AsUnit("Table").
    // The marked parts — the header, the repeat and the record — fold into the one "Table" segment,
    // carrying the failing occurrence's index up onto it, while FullPath keeps the uncollapsed tree
    // GAP B pinned above. The value and the reading are untouched: the markers are presentation-only.

    private static IProjectionDefinition<ISheetCells, IReadOnlyList<T>> UnitTableFromPrimitives<T>(int headerRows, Func<TableRow<ISheetCells>, T> record)
      => PrimitiveTable(headerRows, record, marked: true).AsUnit("Table");

    [Fact]
    public void AsUnitFoldsTheCompositionToOneNamedSegment()
    {
      var failure = Assert.Throws<ProjectionException>(
        () => UnitTableFromPrimitives(1, (TableRow<ISheetCells> row) => row["Amount"].Decimal()).Map(KindMismatch()));

      Assert.Equal("Table[0]", failure.Path);
      Assert.Equal("Table", failure.Subject);
      Assert.Equal("Table -> VerticalBands#2[0] -> Record", failure.FullPath);
      Assert.Equal("A2", failure.Location.A1);
    }

    [Fact]
    public void AsUnitIsAFirewallUnderAnOuterRepeat()
    {
      // Two blank-separated blocks; the second block's body cell is the wrong kind. The outer repeat
      // stays outside the fold (it is not part of the unit), so its own segment survives with the
      // occurrence index, and the inner scaffolding collapses onto the unit.
      var sheet = Mixed(new object?[,]
      {
        { "Investor", "Amount" },
        { "Acme", 10m },
        { null, null },
        { "Investor", "Amount" },
        { "Beta", "oops" },
      });

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalRepeat(UnitTableFromPrimitives(1, ReadLine), separatedBy: BlankRows()).Map(sheet));

      Assert.Equal("VerticalRepeat[1] -> Table[0]", failure.Path);
      Assert.Equal("Table", failure.Subject);
      Assert.Contains("VerticalRepeat[1] -> Table -> ", failure.FullPath);
    }

    [Fact]
    public void WithoutAsUnitTheCollapsedPathEqualsTheFullPath()
    {
      // The no-boundary reference: collapse is inert, so Path and FullPath are the same uncollapsed
      // string GAP B pins above.
      var failure = Assert.Throws<ProjectionException>(
        () => TableFromPrimitives(1, ReadLine).Map(KindMismatch()));

      Assert.Equal(failure.FullPath, failure.Path);
      Assert.Equal("Table -> VerticalBands#2[0] -> Record", failure.Path);
    }

    [Fact]
    public void AsUnitIsPresentationOnly()
      => SameReading(Table(1, ReadLine), UnitTableFromPrimitives(1, ReadLine), Flat());

    // --- 2c. .AsUnit — broad behavioural coverage of the fold ------------------------------------
    //
    // The compositions below are built directly rather than through TableFromPrimitives, so each pins
    // one facet of the collapse in isolation: a quoted-name survivor, nested boundaries, a diagnostic
    // (not just an exception), the AsUnit/Named precedence, the degenerate single-leaf unit, and the
    // kind-suffix rule. The strings asserted are the ones the code actually produces.

    // A single value leaf hand-named "allocation" under a repeat marked .AsUnit("Table"). The unit
    // label replaces the repeat's own segment and carries its occurrence index; the leaf is written
    // by the declaration, so it keeps its quoted segment and is the subject.
    [Fact]
    public void AsUnitKeepsANamedSurvivorAndHoistsTheIndexOntoTheUnit()
    {
      var sheet = Mixed(new object?[,] { { "oops" } });

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalRepeat(Decimal().Named("allocation")).AsUnit("Table").Map(sheet));

      Assert.Equal("Table[0] -> 'allocation' (Decimal)", failure.Path);
      Assert.Equal("'allocation'", failure.Subject);

      // The value leaf's kind is still what the failure is about — the marker touches only the path.
      Assert.Contains("Number", failure.Problem);
      Assert.Contains("Text", failure.Problem);

      // The repeat itself is the boundary and nothing in the chain is marked scaffolding, so there
      // is nothing to fold: the collapsed Path and FullPath are the same string.
      Assert.Equal("Table[0] -> 'allocation' (Decimal)", failure.FullPath);
    }

    // A unit inside a unit: an .AsUnit("Inner") composition used as the item of an .AsUnit("Outer") one.
    // Both boundaries keep their segments, the marked repeat between them does not, and the failing
    // named leaf keeps its own as the deepest segment. Each flow IS its boundary, so neither "Body"
    // nor "Row" appears: a boundary renders its unit label in place of its description.
    private static IProjectionDefinition<ISheetCells, decimal> InnerUnit()
      => new FlowDefinition<ISheetCells, decimal>(
        Orientation.Vertical,
        SingleChild(Decimal().Named("allocation")),
        Placement.Default,
        "Row").AsUnit("Inner");

    private static IProjectionDefinition<ISheetCells, IReadOnlyList<decimal>> OuterUnit()
      => new FlowDefinition<ISheetCells, IReadOnlyList<decimal>>(
        Orientation.Vertical,
        SingleChild(VerticalRepeat(InnerUnit()).AsScaffolding()),
        Placement.Default,
        "Body").AsUnit("Outer");

    /// <summary>A layout of exactly one child, declared with no use-site text — as a factory declares the parts it assembles.</summary>
    private static Layout<ISheetCells, T> SingleChild<T>(IProjectionDefinition<ISheetCells, T> child)
      => LayoutBuilder<ISheetCells>.Declare<T>(
        flow =>
        {
          var only = flow.Next(child, declared: null);
          return flow.Build(read => read.Of(only));
        },
        "a flow",
        nameof(child));

    [Fact]
    public void NestedUnitsBothFoldAndNeitherLeaksScaffolding()
    {
      var sheet = Mixed(new object?[,] { { "oops" } });

      var failure = Assert.Throws<ProjectionException>(() => OuterUnit().Map(sheet));

      // Both boundaries kept; the repeat's occurrence index hoists onto the outer unit, and Inner
      // carries none of its own. The collapsed path names no scaffolding.
      Assert.Equal("Outer[0] -> Inner -> 'allocation' (Decimal)", failure.Path);
      Assert.Equal("'allocation'", failure.Subject);
      Assert.DoesNotContain("VerticalRepeat", failure.Path);
      Assert.DoesNotContain("Row", failure.Path);
      Assert.DoesNotContain("Body", failure.Path);

      // FullPath keeps the uncollapsed tree, scaffolding and all — the repeat renders by description
      // with its use-site ordinal, and the failing leaf earns its kind suffix.
      Assert.Equal("Outer -> VerticalRepeat#1[0] -> Inner -> 'allocation' (Decimal)", failure.FullPath);
    }

    // A diagnostic (not an exception) under a boundary. A tolerant band tiler skips a fully-blank body
    // row with an Info; the boundary folds the Info's Path exactly as it folds a failure's, while
    // FullPath keeps the uncollapsed chain.
    private static IProjectionDefinition<ISheetCells, IReadOnlyList<int>> TolerantUnit()
      => new FlowDefinition<ISheetCells, IReadOnlyList<int>>(
        Orientation.Vertical,
        SingleChild(VerticalBands(1, Record((TableRow<ISheetCells> row) => row.Index), onBlank: BlankRowStrategy.Tolerate).AsScaffolding()),
        Placement.Default,
        "Body").AsUnit("Table");

    [Fact]
    public void AsUnitCollapsesADiagnosticPathButKeepsTheFullPath()
    {
      var sheet = Mixed(new object?[,]
      {
        { "a" },
        { null },
        { "b" },
      });

      var result = TolerantUnit().MapWithDiagnostics(sheet);

      var info = result.Diagnostics.Single(diagnostic => diagnostic.Message.Contains("blank"));

      Assert.Equal(DiagnosticSeverity.Info, info.Severity);
      Assert.Equal("Table", info.Path);
      Assert.Equal("Table -> VerticalBands#1", info.FullPath);
      Assert.NotEqual(info.Path, info.FullPath);
    }

    // AsUnit sets the kind label and Named sets the instance name — two independent slots that
    // concatenate as "kind:instance", so both orders yield the same segment. A plain Named with no
    // AsUnit is unchanged: a quoted name with its kind suffix.
    [Fact]
    public void AsUnitAndNamedConcatenateOrderIndependently()
    {
      var sheet = Mixed(new object?[,] { { "oops" } });

      var unitThenNamed = Assert.Throws<ProjectionException>(
        () => Decimal().AsUnit("Table").Named("fruit").Map(sheet));
      var namedThenUnit = Assert.Throws<ProjectionException>(
        () => Decimal().Named("fruit").AsUnit("Table").Map(sheet));

      // The kind suffix is there because a name is: the segment says "fruit", and the leaf is still
      // a Decimal.
      Assert.Equal("Table:fruit (Decimal)", unitThenNamed.Path);
      Assert.Equal("Table:fruit (Decimal)", namedThenUnit.Path);
      Assert.Equal("Table:fruit", unitThenNamed.Subject);
      Assert.Equal("Table:fruit", namedThenUnit.Subject);

      var plainNamed = Assert.Throws<ProjectionException>(
        () => Decimal().Named("fruit").Map(sheet));

      Assert.Equal("'fruit' (Decimal)", plainNamed.Path);
    }

    // A .AsUnit'd item used at a use site with a bare-identifier name — the unit name wins over the
    // use-site label 'block'.
    [Fact]
    public void AUnitNameBeatsTheUseSiteLabel()
    {
      var sheet = Mixed(new object?[,] { { "oops" } });
      var block = VerticalRepeat(Decimal().Named("x")).AsUnit("Table");

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalFlow(v =>
        {
          var block2 = v.Next(block);

          return v.Build(read => read.Of(block2));
        }).Map(sheet));

      Assert.Contains("Table[0]", failure.Path);
      Assert.DoesNotContain("block", failure.Path);
    }

    // A degenerate unit: .AsUnit on a plain leaf. With no instance name the kind suffix is gone from
    // every rendering — the boundary carries the label alone, and there is no name to hide the kind.
    [Fact]
    public void AsUnitOnAPlainLeafIsADegenerateUnit()
    {
      var sheet = Mixed(new object?[,] { { "oops" } });

      var failure = Assert.Throws<ProjectionException>(
        () => Decimal().AsUnit("Cell").Map(sheet));

      Assert.Equal("Cell", failure.Path);
      Assert.Equal("Cell", failure.Subject);
      Assert.Equal("Cell", failure.FullPath);
      Assert.DoesNotContain("Decimal", failure.Path);
    }

    // .AsScaffolding standing on its own, away from the table composition: the marker is the whole of
    // what the collapse acts on, so a unit a user assembles themselves folds exactly the parts they
    // marked and nothing else. Two readings of one declaration, differing only in the marker.

    private static IProjectionDefinition<ISheetCells, IReadOnlyList<string>> Card(bool marked)
      => new FlowDefinition<ISheetCells, IReadOnlyList<string>>(
        Orientation.Vertical,
        SingleChild(Mark(VerticalRepeat(Text().Named("investor")), marked)),
        Placement.Default,
        "Body").AsUnit("Card");

    [Fact]
    public void AsScaffoldingFoldsOnlyWhatCarriesIt()
    {
      var sheet = Mixed(new object?[,] { { 1 } });

      var marked = Assert.Throws<ProjectionException>(() => Card(marked: true).Map(sheet));
      var unmarked = Assert.Throws<ProjectionException>(() => Card(marked: false).Map(sheet));

      // Marked: the repeat is chrome the caller never wrote, so it contributes no segment and hands
      // its occurrence index up to the unit. The leaf the caller named keeps its own segment and the
      // kind suffix that says what a quoted name hides.
      Assert.Equal("Card[0] -> 'investor' (Text)", marked.Path);
      Assert.Equal("Card -> VerticalRepeat#1[0] -> 'investor' (Text)", marked.FullPath);

      // Unmarked: an inner node without the marker is not folded, even inside a unit — so the
      // collapsed path is the whole tree, and equals the uncollapsed one.
      Assert.Equal("Card -> VerticalRepeat#1[0] -> 'investor' (Text)", unmarked.Path);
      Assert.Equal(unmarked.FullPath, unmarked.Path);
    }

    [Fact]
    public void AScaffoldingMarkedRootStillRendersAPath()
    {
      // Nothing survives the fold, so the path is the degenerate one rather than empty; the subject
      // still names the node that failed.
      var failure = Assert.Throws<ProjectionException>(
        () => Decimal().AsScaffolding().Map(Mixed(new object?[,] { { "oops" } })));

      Assert.Equal("(root)", failure.Path);
      Assert.NotEmpty(failure.Subject);
      Assert.Equal("Decimal", failure.FullPath);
    }

    // The marker is presentation-only, over a second fixture as well as Flat above.
    [Fact]
    public void AsUnitIsPresentationOnlyOverTrailingContent()
      => SameReading(Table(1, ReadLine), UnitTableFromPrimitives(1, ReadLine), Trailing());

    // The kind-suffix rule, unchanged: a no-boundary failing leaf keeps its ` (Kind)` suffix, and with
    // no boundary in the chain the collapsed Path equals FullPath.
    [Fact]
    public void ANoBoundaryFailingLeafStillCarriesItsKindSuffix()
    {
      var sheet = Mixed(new object?[,] { { "oops" } });

      var failure = Assert.Throws<ProjectionException>(
        () => Decimal().Named("amount").Map(sheet));

      Assert.Equal("'amount' (Decimal)", failure.Path);
      Assert.Equal(failure.Path, failure.FullPath);
    }

    // --- 3. GAP A closed — the composition streams in step with the leaf --------------------------
    //
    // The reimplementation declares the discovered block on the flow, and the band tiler inside it
    // walks that bound one row past the cursor, exactly as the built-in leaf's StreamBands does. Over
    // the trailing-content sheet the two now touch the same rows by the time the first record
    // projects and the same total at completion, and read the same value.

    [Fact]
    public void AnEachRowInsideAHeaderedTableResolvesThatTablesOwnCaptionsByName()
    {
      // The header a Table(headerRows:, eachRow:) consumes is not merely skipped
      // — it is pushed as the ambient column labels for the band projection, so a Record inside the
      // slot resolves "Investor" against the table's OWN captions and lands on the body values. The
      // parity partner is the leaf rung over the same sheet: composed and leaf read the same records,
      // which is what says the slot rung is a composition of the primitives and not a second reading
      // of the header.
      var sheet = Flat();

      IReadOnlyList<string> composed = Table(1, Record((TableRow<ISheetCells> row) => row["Investor"].Text())).Map(sheet);
      IReadOnlyList<string> leaf = Table((TableRow<ISheetCells> row) => row["Investor"].Text()).Map(sheet);

      Assert.Equal(new[] { "Acme", "Beta" }, composed);
      Assert.Equal(leaf, composed);
    }

    // --- 4. §5.5 pins ------------------------------------------------------------------------------

    // WithColumnLabels is extent-transparent: it forces nothing of its own and adds no path segment,
    // so the wrapped body reads exactly as it would unwrapped. Compared at L2 (value + consumed +
    // offset + advance) against the body alone, both read under the same literal map so resolution is
    // identical on both sides and only the wrapper is under test.

    [Fact]
    public void WithColumnLabelsIsExtentTransparent()
    {
      var sheet = Flat();
      var map = LabelMap.Of(("Investor", 0), ("Amount", 1));

      // The body reads one row by index, so it needs no labels to succeed and the wrapper is the only
      // variable. VerticalRepeat over the sheet gives the wrapper something with a real extent to be
      // transparent about.
      var body = VerticalRepeat(Record((TableRow<ISheetCells> row) => row.Count));

      var wrapped = Observations.Observe(WithColumnLabels(map, body), sheet);
      var bare = Observations.Observe(body, sheet);

      Observations.AssertL2(bare, wrapped);
    }

    [Fact]
    public void WithColumnLabelsAddsNoPathSegment()
    {
      var sheet = KindMismatch();
      var map = LabelMap.Of(("Amount", 1));

      // A body that fails, so its path is observable. Wrapped and bare must reach the same path — the
      // transparent wrapper contributes nothing to it.
      var body = VerticalRepeat(Record((TableRow<ISheetCells> row) => row["Amount"].Decimal()));

      var wrapped = Assert.Throws<ProjectionException>(() => WithColumnLabels(map, body).Map(sheet));
      var bare = Assert.Throws<ProjectionException>(() => body.Map(sheet));

      Assert.Equal(bare.Path, wrapped.Path);
      Assert.DoesNotContain("WithColumnLabels", wrapped.Path);
    }

    // .AsUnit on a transparent wrapper: WithColumnLabels adds no segment of its own, but marking it a
    // unit boundary makes it opaque, so it claims a boundary segment the internals fold into instead of
    // being skipped like the bare wrapper above.
    [Fact]
    public void AsUnitOnATransparentWrapperClaimsABoundarySegment()
    {
      var sheet = KindMismatch();
      var map = LabelMap.Of(("Amount", 1));
      var body = VerticalRepeat(Record((TableRow<ISheetCells> row) => row["Amount"].Decimal()).AsScaffolding()).AsScaffolding();

      var failure = Assert.Throws<ProjectionException>(
        () => WithColumnLabels(map, body).AsUnit("Table").Map(sheet));

      Assert.StartsWith("Table", failure.Path);
      Assert.DoesNotContain("VerticalRepeat", failure.Path);
      Assert.Contains("VerticalRepeat", failure.FullPath);
    }

    // ColumnLabels takes the width of the band it is handed rather than discovering one of its own,
    // so a header row read straight off a four-column sheet mints four labels — two named and two
    // empty. Narrowing to the columns that carry content is the placement's job, not the map's.

    [Fact]
    public void ColumnLabelsTakesTheWidthOfTheBandItIsHanded()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Investor", "Amount", null, null },
        { "Acme", 10m, null, null },
      });

      LabelMap map = ColumnLabels(1).Map(sheet);

      Assert.Equal(new[] { "Investor", "Amount", "", "" }, map.Labels);
      Assert.Equal(4, map.Labels.Count);
    }

    // WithColumnLabels bounds the body to the label-map width when the extent is wider, so a record
    // cannot reach a trailing column outside the labels; on an exact-width extent it is the identity.

    [Fact]
    public void WithColumnLabelsBoundsTheBodyToTheLabelWidthWhenTheSheetIsWider()
    {
      // Two labels over a three-column sheet whose body carries a trailing third column. The body is
      // narrowed to the labelled width, so each record sees two columns, not three.
      var sheet = Mixed(new object?[,]
      {
        { "Investor", "Amount", null },
        { "Acme", 10m, 999m },
        { "Beta", 20m, 888m },
      });

      var map = LabelMap.Of(("Investor", 0), ("Amount", 1));

      IReadOnlyList<int> widths = WithColumnLabels(map, VerticalRepeat(Record((TableRow<ISheetCells> row) => row.Count))).Map(sheet);

      Assert.All(widths, width => Assert.Equal(2, width));
    }

    [Fact]
    public void WithColumnLabelsLeavesAnExactWidthSheetUntouched()
    {
      // Label width equals sheet width, so the wider-than branch is not taken and the body is handed
      // the extent unchanged — the identity the extent-transparency pin depends on.
      var sheet = Flat();
      var map = LabelMap.Of(("Investor", 0), ("Amount", 1));

      IReadOnlyList<int> widths = WithColumnLabels(map, VerticalRepeat(Record((TableRow<ISheetCells> row) => row.Count))).Map(sheet);

      Assert.All(widths, width => Assert.Equal(2, width));
    }

    // ColumnLabels reads the header through the same TableView parser a built-in Table uses, so its
    // LabelMap is byte-identical to the table's own header: same labels, same ordinals under the
    // content rule.

    [Fact]
    public void ColumnLabelsMintsTheSameMapAsATablesOwnHeader()
    {
      var sheet = Flat();

      LabelMap fromPrimitive = ColumnLabels(1).Map(sheet);
      var fromTable = Table((TableView<ISheetCells> view) => view).Map(sheet);

      Assert.Equal(fromTable.ColumnNames, fromPrimitive.Labels);

      // Same ordinals through the ILabelSource face the primitive path resolves by.
      Assert.Equal(new[] { 0 }, ((ILabelSource)fromPrimitive).IndicesOf("Investor"));
      Assert.Equal(new[] { 1 }, ((ILabelSource)fromPrimitive).IndicesOf("Amount"));
      Assert.Equal(fromTable.IndicesOf("Amount"), ((ILabelSource)fromPrimitive).IndicesOf("Amount"));
    }

    // The comparer split: the bind-rung indexer matches by CaptionComparer (whitespace ignored
    // everywhere), while the ILabelSource face the primitive path resolves through matches by the
    // content rule (trimmed and case-insensitive, interior whitespace kept). A header of "Net Income"
    // is the wedge: "NetIncome" (no space) binds through the indexer and misses through IndicesOf.

    [Fact]
    public void TheLabelMapCarriesTwoMatchingRulesOnePerFace()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Investor", "Net Income" },
        { "Acme", 10m },
      });

      LabelMap map = ColumnLabels(1).Map(sheet);

      // Bind-rung indexer — CaptionComparer, whitespace ignored everywhere.
      Assert.Equal(1, map["NetIncome"]);
      Assert.Equal(1, map["net income"]);

      // ILabelSource — the content rule, interior whitespace kept, so "NetIncome" misses.
      Assert.Empty(((ILabelSource)map).IndicesOf("NetIncome"));
      Assert.Equal(new[] { 1 }, ((ILabelSource)map).IndicesOf("net income"));
    }

    // Citation parity: the bind-rung indexer's failure — missing or ambiguous — is byte-identical
    // between the primitive-minted map and a built-in Table's own map, because both are the one
    // HeaderLabels parse over the same header. Only the declaration-path subject differs (ColumnLabels
    // vs Table), which the Problem/A1 facets exclude — the same facets §5.4's differential asserts on.

    [Fact]
    public void ColumnLabelsCitesAMissingOrAmbiguousCaptionIdenticallyToATablesOwnMap()
    {
      // A caption the header does not carry — the missing citation, which lists the file's captions.
      SameCitation(AbsentColumn(), "Investor");

      // A caption two columns carry — the ambiguous citation, which names both header cells.
      SameCitation(AmbiguousColumn(), "Amount");
    }

    // The built-in Table's own map, captured through the bind rung: eachRow is handed the table's
    // LabelMap, and a record that reads only by index lets the Map complete so the capture survives.
    private static LabelMap ATablesOwnMap(ISheetCells sheet)
    {
      LabelMap captured = null!;

      Table(1, captions =>
      {
        captured = captions;
        return Record((TableRow<ISheetCells> row) => row.Index);
      }).Map(sheet);

      return captured;
    }

    private static void SameCitation(ISheetCells sheet, string caption)
    {
      LabelMap fromPrimitive = ColumnLabels(1).Map(sheet);
      LabelMap fromTable = ATablesOwnMap(sheet);

      var primitive = Assert.Throws<ProjectionException>(() => fromPrimitive[caption]);
      var table = Assert.Throws<ProjectionException>(() => fromTable[caption]);

      // The citation itself — Problem and A1 — is identical; only the path subject differs.
      Assert.Equal(table.Problem, primitive.Problem);
      Assert.Equal(table.Location.A1, primitive.Location.A1);
    }

    // The extent a bind-rung caption failure cites (spec facet, owner-approved). The ColumnLabels
    // decoupling refactor deliberately narrowed this: a missing/ambiguous caption failure now cites
    // the HEADER BAND — ColumnCount x headerRows — not the full table (ColumnCount x (headerRows +
    // bodyRows)). A caption fault is about the header, and citing the header avoids forcing a
    // still-discovering region to yield its full height. The facet slipped the gate for want of
    // a guard, so it is pinned here — for BOTH faces, the primitive-minted map and a built-in Table's
    // own map, the same parity the SameCitation sibling asserts on Problem/A1.

    [Fact]
    public void ABindRungCaptionFailureCitesTheHeaderBandExtentNotTheFullTable()
    {
      // Several body rows so the header band and the full extent genuinely differ: a failure citing
      // the full table would report 2x6 here, its height tracking the body. The header band is 2x1.
      var absent = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", 10m },
        { "Beta", 20m },
        { "Gamma", 30m },
        { "Delta", 40m },
        { "Epsilon", 50m },
      });

      HeaderBandCitation(absent, caption: "Investor", expected: new Size(2, 1), fullHeight: 6);

      // Ambiguous, three columns: the header band is 3x1, the full extent 3x5.
      var ambiguous = Mixed(new object?[,]
      {
        { "Investor", "Amount", "Amount" },
        { "Acme", 10m, 11m },
        { "Beta", 20m, 21m },
        { "Gamma", 30m, 31m },
        { "Delta", 40m, 41m },
      });

      HeaderBandCitation(ambiguous, caption: "Amount", expected: new Size(3, 1), fullHeight: 5);
    }

    private static void HeaderBandCitation(ISheetCells sheet, string caption, Size expected, int fullHeight)
    {
      LabelMap fromPrimitive = ColumnLabels(1).Map(sheet);
      LabelMap fromTable = ATablesOwnMap(sheet);

      var primitive = Assert.Throws<ProjectionException>(() => fromPrimitive[caption]);
      var table = Assert.Throws<ProjectionException>(() => fromTable[caption]);

      // The header band — ColumnCount x headerRows (headerRows == 1) — for both faces. Asserted as
      // the concrete Size the code produces, explicitly NOT the full-table height.
      foreach (var available in new[] { primitive.Location.Available, table.Location.Available })
      {
        Assert.Equal(expected.Width, available.Width);
        Assert.Equal(1, available.Height);
        Assert.Equal(expected.Height, available.Height);
        Assert.NotEqual(fullHeight, available.Height);
      }

      // Parity on this facet, matching how SameCitation asserts Problem/A1 parity: the two faces cite
      // the same header-band extent, down to the message's "NxM available" tail.
      Assert.Equal(table.Location.Available.Width, primitive.Location.Available.Width);
      Assert.Equal(table.Location.Available.Height, primitive.Location.Available.Height);
      Assert.Contains($"{expected.Width}x{expected.Height} available", primitive.Message);
      Assert.Contains($"{expected.Width}x{expected.Height} available", table.Message);
    }

    // Record standalone reuse: a decoupled Record reading a wrong-kind cell under a pushed scope
    // reports the compute-legal binder's own sentence — `column 'Amount': …` at the cell's A1 —
    // identical to what the built-in Table produces, because resolution flows through the one shared
    // Resolvable/Convert path with nothing copied.

    [Fact]
    public void AStandaloneRecordReportsAWrongKindCellInTheBinderSentence()
    {
      // A headerless single-row sheet read by a literal map: the record resolves "Amount" to column 1,
      // finds text where it reads a decimal, and reports the compute-legal binder's own sentence,
      // with the A1 of the cell it actually read (B1 here). Since phase 6 the caption is a path
      // segment rather than a prefix on the sentence, so what is pinned here is the sentence itself
      // — see CellReadingIdentityTests for where the caption went. No copying: resolution
      // ran through the one shared Resolvable/Convert path a built-in Table's row uses, so the sentence
      // is the binder's. (Byte-identical A1 in the full table composition is pinned by the differential
      // over the kind-mismatch sheet above.)
      var sheet = Mixed(new object?[,]
      {
        { "Acme", "oops" },
      });

      var map = LabelMap.Of(("Investor", 0), ("Amount", 1));

      var standalone = Assert.Throws<ProjectionException>(
        () => WithColumnLabels(map, VerticalRepeat(Record((TableRow<ISheetCells> row) => row["Amount"].Decimal()))).Map(sheet));

      Assert.Equal("expected Number at B1, found Text", Problem(standalone));
    }

    // Frame-agreement / translation, through the primitives at a column offset. WithColumnLabels
    // captures the map at the reading frame and Record reads at the same frame, so translation is the
    // identity — the label lands on the ABSOLUTE column even when the whole table is shifted right,
    // which is the silent-wrong-read case the step-1 mutation guard pins directly.

    [Fact]
    public void ALabelLandsOnItsAbsoluteColumnWhenTheWholeTableIsOffset()
    {
      var sheet = OffsetColumn();

      IReadOnlyList<Line> offset = Right(1).Of(TableFromPrimitives(1, ReadLine)).Map(sheet);
      IReadOnlyList<Line> flush = TableFromPrimitives(1, ReadLine).Map(Flat());

      // The same records regardless of the column origin — the translation term absorbed the shift.
      Assert.Equal(flush, offset);
      Assert.Equal(new[] { new Line(0, "Acme", 10m), new Line(1, "Beta", 20m) }, offset);
    }

    // LabelMap.Of: a literal map answers the primitive ILabelSource face (so a Record resolves through
    // it) but has no header cells, so the bind-rung indexer refuses with the documented message rather
    // than a NullReferenceException.

    [Fact]
    public void LabelMapOfAnswersThePrimitiveFaceAndRefusesTheBindRung()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Acme", 10m },
        { "Beta", 20m },
      });

      var map = LabelMap.Of(("Investor", 0), ("Amount", 1));

      // The primitive face resolves — a headerless sheet read by a literal map.
      IReadOnlyList<Line> lines = WithColumnLabels(map, VerticalRepeat(Record(ReadLine))).Map(sheet);

      Assert.Equal(new[] { new Line(0, "Acme", 10m), new Line(1, "Beta", 20m) }, lines);

      // The bind-rung indexer cites header cells only on a miss or an ambiguity; a literal map has
      // none, so a missing caption reaches for the citation and refuses with the documented message
      // rather than a NullReferenceException. (A present caption still resolves — matches.Count == 1
      // returns before the citation is needed.)
      Assert.Equal(1, map["Amount"]);

      var refusal = Assert.Throws<InvalidOperationException>(() => map["Missing"]);

      Assert.Contains("created from literal labels", refusal.Message);
    }

    // row.Index (GAP C): a decoupled Record recovers the enclosing repeat's occurrence number.

    [Fact]
    public void RecordIndexIsTheOccurrenceNumberOfTheEnclosingRepeat()
    {
      var sheet = Mixed(new object?[,]
      {
        { "a" },
        { "b" },
        { "c" },
      });

      IReadOnlyList<int> indices = VerticalRepeat(Record((TableRow<ISheetCells> row) => row.Index)).Map(sheet);

      Assert.Equal(new[] { 0, 1, 2 }, indices);
    }

    [Fact]
    public void ANestedRepeatsInnerRecordIndexIsTheInnerOccurrence()
    {
      // Two blank-separated blocks, each a table whose header is consumed and whose two body rows are
      // read by Record. Record reads the NEAREST enclosing repeat's occurrence — the table's inner
      // VerticalRepeat — so the index resets per block. (A bare VerticalRepeat(Record) would not stop
      // at the blank row, since a Record's one-row band has no content rule; a table's discovered
      // block does, which is why the nesting is through the table composition.)
      var sheet = Mixed(new object?[,]
      {
        { "Header" },
        { "a" },
        { "b" },
        { null },
        { "Header" },
        { "c" },
        { "d" },
      });

      var block = TableFromPrimitives(1, (TableRow<ISheetCells> row) => row.Index);
      IReadOnlyList<IReadOnlyList<int>> blocks = VerticalRepeat(block, separatedBy: BlankRows()).Map(sheet);

      Assert.Equal(new[] { new[] { 0, 1 }, new[] { 0, 1 } }, blocks.Select(b => b.ToArray()));
    }

    [Fact]
    public void TheOrdinalFieldLeavesPathRenderingUnchanged()
    {
      // The third occurrence's record fails; the path must still index the repeat by its
      // path-rendering Index (VerticalRepeat[2]), which the copied Ordinal field does not touch —
      // Ordinal persists into the item's subtree for row.Index, the path Index is nulled on Descend,
      // and they are distinct so the segment renders exactly as before step 2.
      var sheet = Mixed(new object?[,]
      {
        { 10m },
        { 20m },
        { "oops" },
      });

      var record = Record((TableRow<ISheetCells> row) => row[0].Decimal());

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalRepeat(record).Map(sheet));

      Assert.Contains("VerticalRepeat[2]", failure.Path);
    }

    // --- 5. An empty extent under a declared header — the composed rung says what the leaves say ---
    //
    // Regression pin. The composed Table(1, eachRow) reaches its header through ColumnLabels rather
    // than through TableView's own parse, so the "there is no row to cut a header band from" guard
    // has to live in the primitive as well. Without it the composed rung cut the band anyway and
    // surfaced a raw OutOfBoundsException: not the sentence the leaf rungs speak, and not a
    // declaration-level failure, so no tolerance boundary could absorb it either. The leaf rungs'
    // side of this sentence is pinned in TableProjectionTests and DictionaryTableTests; here it is
    // asserted IDENTICAL across the composed rung and the primitive underneath it.

    private const string EmptyExtentProblem = "a header row was declared but the table's extent is empty";

    /// <summary>An all-blank sheet: a real extent to place onto, with no content row to head it.</summary>
    private static ISheetCells AllBlank() => Mixed(new object?[,] { { null, null }, { null, null } });

    [Fact]
    public void TheComposedRungCitesAnEmptyExtentInTheLeafRungsOwnSentence()
    {
      var composed = Assert.Throws<ProjectionException>(
        () => Table(1, Record((TableRow<ISheetCells> row) => row.Index)).Map(AllBlank()));

      // The TableView leaf rung over the same sheet — the parity partner, not a copied string.
      var leaf = Assert.Throws<ProjectionException>(() => Table((TableView<ISheetCells> view) => view.RowCount).Map(AllBlank()));

      Assert.Equal(EmptyExtentProblem, Problem(composed));
      Assert.Equal(Problem(leaf), Problem(composed));

      // A declaration-level failure, not a fault — which is what makes the sibling pin below possible.
      Assert.False(composed.IsFault);

      // The unit boundary keeps the composed rung's collapsed path as flat as the leaf's.
      Assert.Equal("Table", composed.Path);
      Assert.Equal("Table", composed.Subject);
      Assert.Equal(leaf.Path, composed.Path);
      Assert.Equal(leaf.Subject, composed.Subject);

      // FullPath keeps the primitive tree the rung composes, each part under its own description.
      Assert.Equal("Table -> UnderColumnLabels -> ColumnLabels#1", composed.FullPath);
    }

    [Fact]
    public void TheComposedRungsEmptyExtentFailureIsAbsorbable()
    {
      // The other half of the regression: a raw OutOfBoundsException escaped .Optional(), so the
      // absence of a table read as a broken file. The guarded failure is absorbed and reads as null.
      IReadOnlyList<int>? absent = Table(1, Record((TableRow<ISheetCells> row) => row.Index)).Optional().Map(AllBlank());

      Assert.Null(absent);
    }

    [Fact]
    public void ColumnLabelsCitesAnEmptyExtentInTheSameSentence()
    {
      // The primitive alone, over a genuinely zero-by-zero extent: the guard's own home, so the
      // sentence is pinned at the site that produces it as well as through the composition.
      var failure = Assert.Throws<ProjectionException>(() => ColumnLabels(1).Map(Mixed(new object?[0, 0])));

      Assert.Equal(EmptyExtentProblem, Problem(failure));
      Assert.Equal("ColumnLabels", failure.Path);
    }

    // --- Machinery ----------------------------------------------------------------------------------

    private static void SameReading<T>(IProjectionDefinition<ISheetCells, T> bespoke, IProjectionDefinition<ISheetCells, T> primitives, ISheetCells sheet)
    {
      var expected = Observations.Observe(bespoke, sheet);
      var actual = Observations.Observe(primitives, sheet);

      // L1 is precisely the acceptance level: value + failure (Problem + A1 location + fault + inner),
      // and deliberately NOT path/subject/diagnostics, which GAP B moves.
      Observations.AssertL1(expected, actual);
    }

    // A field shared by the two GAP-A readings so the record lambda can note the counting space's
    // ledger at the moment the first record projects. Reset before each reading.
    private static CountingSpace _gapACounter = null!;
    private static int _gapAAtFirstRecord = -1;

    private static int Instrumented(TableRow<ISheetCells> row)
    {
      if (_gapAAtFirstRecord < 0)
        _gapAAtFirstRecord = _gapACounter.RowsTouched;

      return row.Index;
    }

    /// <summary>
    /// <see cref="Instrumented"/> as a row PROJECTION rather than a record lambda, so the slot rung
    /// can be measured by the same harness. It reads the band it was handed, which is what a record
    /// that measured itself could not do.
    /// </summary>
    private static IProjectionDefinition<ISheetCells, int> InstrumentedBand() => Range(WholeExtent(), block =>
    {
      if (_gapAAtFirstRecord < 0)
        _gapAAtFirstRecord = _gapACounter.RowsTouched;

      return block.Height;
    });

    private static (int AtFirstRecord, int Total) RowsTouchedAtFirstRecord(IProjectionDefinition<ISheetCells, IReadOnlyList<int>> table)
    {
      _gapACounter = new CountingSpace(Trailing());
      _gapAAtFirstRecord = -1;

      table.Map(_gapACounter);

      return (_gapAAtFirstRecord, _gapACounter.RowsTouched);
    }
  }
}
