using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Step 2 of labelled axes: the built-in <c>Table</c> is reimplementable from the three public
  /// primitives — <see cref="Projection.ColumnLabels(int)"/> manufactures a <see cref="LabelMap"/>,
  /// <see cref="Projection.WithColumnLabels{T}(LabelMap, IProjection{T})"/> pushes it as the ambient
  /// column labels, and <see cref="Projection.Record{T}(Func{TableRow, T})"/> reads a row by name
  /// through the pushed scope. The acceptance claim is byte-identity of the reading's <em>value</em>,
  /// its failure <em>message</em> and <em>A1 location</em>, and — since GAP C closed in step 2 — its
  /// <see cref="TableRow.Index"/>. GAP A is closed: the repeat re-hosts the discovered block, so the
  /// composition streams in step with the leaf. GAP B remains the one documented divergence, captured
  /// not asserted equal: path/subject reflect the primitive tree, not a flat <c>Table</c>.
  /// </summary>
  public class LabeledAxisPrimitivesTests
  {
    // --- The reimplementation, built from the PUBLIC primitives ------------------------------------
    //
    // The composition: a VerticalFlow of
    // ColumnLabels then WithColumnLabels(columns, VerticalRepeat(Record(record))). It is dressed with
    // the built-in table's own placement — skip-to-first-non-blank-cell over a discovered block — and its "Table"
    // description, through the internal FlowProjection, which is precisely what step 3 will do when
    // the bespoke path is deleted. Constructed in the test project because the primitives it composes
    // are public and the assembling FlowProjection/Placement are reachable through InternalsVisibleTo.

    internal static IProjection<IReadOnlyList<T>> TableFromPrimitives<T>(int headerRows, Func<TableRow, T> record)
      => new FlowProjection<IReadOnlyList<T>>(
        Orientation.Vertical,
        flow =>
        {
          var columns = flow.Next(ColumnLabels(headerRows));

          return flow.Next(WithColumnLabels(columns, VerticalRepeat(Record(record), onBlank: BlankRowStrategy.Stop)));
        },
        TablePlacementReplica(),
        "Table");

    /// <summary>
    /// A hand copy of the offset half of the private <c>Projection.TablePlacement()</c> — skip to the
    /// first non-blank cell (down to the first content row, then across to its first non-blank column).
    /// The block is not declared here: it is re-hosted inside the <c>VerticalRepeat</c> via
    /// <c>onBlank: Stop</c>, so the flow carries no area and the body streams.
    /// </summary>
    private static Placement TablePlacementReplica()
      => new Placement(OffsetStrategies.SkipToFirstNonBlankCell(), null);

    // --- The record read, shared by both spellings -------------------------------------------------
    //
    // Reading Index into the value bakes GAP C's parity into the value facet: an occurrence number
    // that disagreed between the two Tables would show up as a different Line. Investor and Amount are
    // resolved by name through the ambient scope, which is the whole of what the primitives provide.

    private sealed record Line(int Index, string Investor, decimal Amount);

    private static Line ReadLine(TableRow row) => new Line(row.Index, row.Text("Investor"), row.Decimal("Amount"));

    // --- The sheet set (spec §5.4) -----------------------------------------------------------------

    /// <summary>(1) A flat, well-formed table — the case where laziness is preserved.</summary>
    private static ISpace Flat() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10m },
      { "Beta", 20m },
    });

    /// <summary>(2) A table missing the "Investor" column the record reads — an absent-column failure.</summary>
    private static ISpace AbsentColumn() => Mixed(new object?[,]
    {
      { "Client", "Amount" },
      { "Acme", 10m },
    });

    /// <summary>(3) A table carrying "Amount" twice — an ambiguous-column failure.</summary>
    private static ISpace AmbiguousColumn() => Mixed(new object?[,]
    {
      { "Investor", "Amount", "Amount" },
      { "Acme", 10m, 11m },
    });

    /// <summary>(4) The table shifted one column right — column origin &gt; 0, the translation case.</summary>
    private static ISpace OffsetColumn() => Mixed(new object?[,]
    {
      { null, "Investor", "Amount" },
      { null, "Acme", 10m },
      { null, "Beta", 20m },
    });

    /// <summary>(5) Two blank-separated blocks, each with its own header — per-occurrence scope.</summary>
    private static ISpace Repeated() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10m },
      { "Beta", 20m },
      { null, null },
      { "Investor", "Amount" },
      { "Gamma", 30m },
    });

    /// <summary>(6) A table with trailing content past a blank row — forces the discovered block (GAP A).</summary>
    private static ISpace Trailing() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10m },
      { "Beta", 20m },
      { null, null },
      { "Total", 30m },
    });

    /// <summary>(7) A body cell of the wrong kind — text where the record reads a decimal.</summary>
    private static ISpace KindMismatch() => Mixed(new object?[,]
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
      => SameReading(Table(1, ReadLine), TableFromPrimitives(1, ReadLine), KindMismatch());

    // --- 2. GAP B — path/subject reflect the primitive tree, captured not asserted equal -----------
    //
    // The spec is explicit: message and A1 are identical (asserted), path and subject are not (GAP B).
    // The negative-pin rule forbids a blanket "these differ" — so the actual strings are documented as
    // specific asserts. Bespoke reports one flat `Table` segment; the reimplementation's failure sits
    // inside the VerticalRepeat the primitives compose (the transparent WithColumnLabels is skipped,
    // and the FlowProjection's "Table" description keeps the outer segment reading `Table`).

    [Fact]
    public void GapB_TheMessageAndA1AgreeButThePathAndSubjectReflectTheDifferentTrees()
    {
      var sheet = KindMismatch();

      var bespoke = Assert.Throws<ProjectionException>(() => Table(1, ReadLine).Map(sheet));
      var primitives = Assert.Throws<ProjectionException>(() => TableFromPrimitives(1, ReadLine).Map(sheet));

      // Asserted equal — the acceptance claim. Both blame the band's own origin (A2, the failing
      // record's first cell), and both speak the compute-legal binder's sentence.
      Assert.Equal(bespoke.Problem, primitives.Problem);
      Assert.Equal("A2", bespoke.Location.A1);
      Assert.Equal(bespoke.Location.A1, primitives.Location.A1);

      // Documented divergence (GAP B). The exact strings, so a change to either tree is caught here
      // rather than sliding under a "they differ" that would pass forever after the first drift. The
      // built-in Table's Func rung calls the record inline, so its failure blames the flat Table; the
      // reimplementation's failure sits inside the VerticalRepeat the primitives compose — the "Table"
      // description on the FlowProjection keeps the outer segment reading Table, and the transparent
      // WithColumnLabels is skipped, so the tree between them is VerticalRepeat -> Record.
      Assert.Equal("Table", bespoke.Path);
      Assert.Equal("Table", bespoke.Subject);
      Assert.Equal("Table -> VerticalRepeat#2[0] -> Record", primitives.Path);
      Assert.Equal("Record", primitives.Subject);
    }

    // --- 2b. .AsUnit — the composition folds to one named unit in the collapsed path --------------
    //
    // The same reimplementation marked .AsUnit("Table"). The boundary folds its internal scaffolding
    // — the header repeat and the record — into one "Table" segment, carrying the failing occurrence's
    // index up onto it, while FullPath keeps the uncollapsed tree GAP B pinned above. The value and
    // the reading are untouched: the marker is presentation-only.

    private static IProjection<IReadOnlyList<T>> UnitTableFromPrimitives<T>(int headerRows, Func<TableRow, T> record)
      => TableFromPrimitives(headerRows, record).AsUnit("Table");

    [Fact]
    public void AsUnitFoldsTheCompositionToOneNamedSegment()
    {
      var failure = Assert.Throws<ProjectionException>(
        () => UnitTableFromPrimitives(1, (TableRow row) => row.Decimal("Amount")).Map(KindMismatch()));

      Assert.Equal("Table[0]", failure.Path);
      Assert.Equal("Table", failure.Subject);
      Assert.Equal("Table -> VerticalRepeat#2[0] -> Record", failure.FullPath);
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
      Assert.Equal("Table -> VerticalRepeat#2[0] -> Record", failure.Path);
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

    // A single value leaf hand-named "allocation" under a repeat marked .AsUnit("Table"). The named
    // leaf survives the fold as a quoted segment; the repeat's internal occurrence index hoists onto
    // the unit, and the failing leaf is the subject of the deepest surviving segment.
    [Fact]
    public void AsUnitKeepsANamedSurvivorAndHoistsTheIndexOntoTheUnit()
    {
      var sheet = Mixed(new object?[,] { { "oops" } });

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalRepeat(Decimal().Named("allocation")).AsUnit("Table").Map(sheet));

      Assert.Equal("Table[0] -> 'allocation'", failure.Path);
      Assert.Equal("'allocation'", failure.Subject);

      // The value leaf's kind is still what the failure is about — the marker touches only the path.
      Assert.Contains("Number", failure.Problem);
      Assert.Contains("Text", failure.Problem);

      // The repeat itself is the boundary, so FullPath renders it as "Table[0]" too — the only
      // difference from the collapsed Path is the trailing kind suffix RenderFull always applies.
      Assert.Equal("Table[0] -> 'allocation' (Decimal)", failure.FullPath);
    }

    // A unit inside a unit: an .AsUnit("Inner") composition used as the item of an .AsUnit("Outer") one.
    // Both boundaries survive the fold, the scaffolding between them (the flows and the repeat) does
    // not, and the failing named leaf survives as the deepest segment.
    private static IProjection<decimal> InnerUnit()
      => new FlowProjection<decimal>(
        Orientation.Vertical,
        flow => flow.Next(Decimal().Named("allocation")),
        Placement.Default,
        "Row").AsUnit("Inner");

    private static IProjection<IReadOnlyList<decimal>> OuterUnit()
      => new FlowProjection<IReadOnlyList<decimal>>(
        Orientation.Vertical,
        flow => flow.Next(VerticalRepeat(InnerUnit())),
        Placement.Default,
        "Body").AsUnit("Outer");

    [Fact]
    public void NestedUnitsBothFoldAndNeitherLeaksScaffolding()
    {
      var sheet = Mixed(new object?[,] { { "oops" } });

      var failure = Assert.Throws<ProjectionException>(() => OuterUnit().Map(sheet));

      // Both boundaries kept; the repeat's occurrence index hoists onto the outer unit, and Inner
      // carries none of its own. The collapsed path names no scaffolding.
      Assert.Equal("Outer[0] -> Inner -> 'allocation'", failure.Path);
      Assert.Equal("'allocation'", failure.Subject);
      Assert.DoesNotContain("VerticalRepeat", failure.Path);
      Assert.DoesNotContain("Row", failure.Path);
      Assert.DoesNotContain("Body", failure.Path);

      // FullPath keeps the uncollapsed tree, scaffolding and all — the repeat renders by description
      // with its use-site ordinal, and the failing leaf earns its kind suffix.
      Assert.Equal("Outer -> VerticalRepeat#1[0] -> Inner -> 'allocation' (Decimal)", failure.FullPath);
    }

    // A diagnostic (not an exception) under a boundary. A tolerant repeat skips a fully-blank body row
    // with an Info; the boundary folds the Info's Path exactly as it folds a failure's, while FullPath
    // keeps the uncollapsed chain.
    private static IProjection<IReadOnlyList<int>> TolerantUnit()
      => new FlowProjection<IReadOnlyList<int>>(
        Orientation.Vertical,
        flow => flow.Next(VerticalRepeat(Record((TableRow row) => row.Index), onBlank: BlankRowStrategy.Tolerate)),
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
      Assert.Equal("Table -> VerticalRepeat#1", info.FullPath);
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

      Assert.Equal("Table:fruit", unitThenNamed.Path);
      Assert.Equal("Table:fruit", namedThenUnit.Path);
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
        () => VerticalFlow(v => v.Next(block)).Map(sheet));

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
    // The reimplementation re-hosts the discovered block inside the VerticalRepeat, so the flow carries
    // no area and the body walks one row past the cursor via HasRow, exactly as the built-in leaf's
    // StreamBands does. Over the trailing-content sheet the two now touch the same rows by the time the
    // first record projects and the same total at completion, and read the same value.

    [Fact]
    public void GapA_TheCompositeStreamsInStepWithTheLeaf()
    {
      var (bespokeAtFirst, bespokeTotal) = RowsTouchedAtFirstRecord(Table(1, Instrumented));
      var (primitivesAtFirst, primitivesTotal) = RowsTouchedAtFirstRecord(TableFromPrimitives(1, Instrumented));

      Assert.Equal(bespokeAtFirst, primitivesAtFirst);
      Assert.Equal(bespokeTotal, primitivesTotal);

      SameReading(Table(1, ReadLine), TableFromPrimitives(1, ReadLine), Trailing());
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
      var body = VerticalRepeat(Record((TableRow row) => row.Count));

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
      var body = VerticalRepeat(Record((TableRow row) => row.Decimal("Amount")));

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
      var body = VerticalRepeat(Record((TableRow row) => row.Decimal("Amount")));

      var failure = Assert.Throws<ProjectionException>(
        () => WithColumnLabels(map, body).AsUnit("Table").Map(sheet));

      Assert.StartsWith("Table", failure.Path);
      Assert.DoesNotContain("VerticalRepeat", failure.Path);
      Assert.Contains("VerticalRepeat", failure.FullPath);
    }

    // ColumnLabels discovers its width via TakeColumnsWhileAnyValue, so a header row with trailing
    // blank columns mints a map only as wide as its leading valued-column run — not the full sheet
    // width, which would carry empty-string labels for the blank columns.

    [Fact]
    public void ColumnLabelsWidthIsTheLeadingValuedColumnRunNotTheFullSheetWidth()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Investor", "Amount", null, null },
        { "Acme", 10m, null, null },
      });

      LabelMap map = ColumnLabels(1).Map(sheet);

      Assert.Equal(new[] { "Investor", "Amount" }, map.Labels);
      Assert.Equal(2, map.Labels.Count);
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

      IReadOnlyList<int> widths = WithColumnLabels(map, VerticalRepeat(Record((TableRow row) => row.Count))).Map(sheet);

      Assert.All(widths, width => Assert.Equal(2, width));
    }

    [Fact]
    public void WithColumnLabelsLeavesAnExactWidthSheetUntouched()
    {
      // Label width equals sheet width, so the wider-than branch is not taken and the body is handed
      // the extent unchanged — the identity the extent-transparency pin depends on.
      var sheet = Flat();
      var map = LabelMap.Of(("Investor", 0), ("Amount", 1));

      IReadOnlyList<int> widths = WithColumnLabels(map, VerticalRepeat(Record((TableRow row) => row.Count))).Map(sheet);

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
      var fromTable = Table((TableView view) => view).Map(sheet);

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

    // Record standalone reuse: a decoupled Record reading a wrong-kind cell under a pushed scope
    // reports the compute-legal binder's own sentence — `column 'Amount': …` at the cell's A1 —
    // identical to what the built-in Table produces, because resolution flows through the one shared
    // Resolvable/Convert path with nothing copied.

    [Fact]
    public void AStandaloneRecordReportsAWrongKindCellInTheBinderSentence()
    {
      // A headerless single-row sheet read by a literal map: the record resolves "Amount" to column 1,
      // finds text where it reads a decimal, and reports the compute-legal binder's own sentence —
      // `column 'Amount': …` with the A1 of the cell it actually read (B1 here). No copying: resolution
      // ran through the one shared Resolvable/Convert path a built-in Table's row uses, so the sentence
      // is the binder's. (Byte-identical A1 in the full table composition is pinned by the differential
      // over the kind-mismatch sheet above.)
      var sheet = Mixed(new object?[,]
      {
        { "Acme", "oops" },
      });

      var map = LabelMap.Of(("Investor", 0), ("Amount", 1));

      var standalone = Assert.Throws<ProjectionException>(
        () => WithColumnLabels(map, VerticalRepeat(Record((TableRow row) => row.Decimal("Amount")))).Map(sheet));

      Assert.Equal("column 'Amount': expected Number at B1, found Text", Problem(standalone));
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

      IReadOnlyList<int> indices = VerticalRepeat(Record((TableRow row) => row.Index)).Map(sheet);

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

      var block = TableFromPrimitives(1, (TableRow row) => row.Index);
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

      var record = Record((TableRow row) => row.Decimal(0));

      var failure = Assert.Throws<ProjectionException>(
        () => VerticalRepeat(record).Map(sheet));

      Assert.Contains("VerticalRepeat[2]", failure.Path);
    }

    // --- Machinery ----------------------------------------------------------------------------------

    private static void SameReading<T>(IProjection<T> bespoke, IProjection<T> primitives, ISpace sheet)
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

    private static int Instrumented(TableRow row)
    {
      if (_gapAAtFirstRecord < 0)
        _gapAAtFirstRecord = _gapACounter.RowsTouched;

      return row.Index;
    }

    private static (int AtFirstRecord, int Total) RowsTouchedAtFirstRecord(IProjection<IReadOnlyList<int>> table)
    {
      _gapACounter = new CountingSpace(Trailing());
      _gapAAtFirstRecord = -1;

      table.Map(_gapACounter);

      return (_gapAAtFirstRecord, _gapACounter.RowsTouched);
    }
  }
}
