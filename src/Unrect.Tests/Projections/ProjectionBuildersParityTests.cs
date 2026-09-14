using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;
using Xunit.Sdk;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

// The closed vocabulary, held at arm's length so this ONE file can name both spellings at once.
// Everywhere else it is written `using static Unrect.Projections.ProjectionBuilders<...>;` and the
// declarations below it carry no prefix — which is the whole point of the class and exactly why a
// parity suite cannot be written that way: a file that imported it statically could not also import
// `Projection`, every shared name being ambiguous, and there would be nothing to compare against.
// So an ALIAS, and `B.` at every use site, is this file's deliberate departure from the shipping
// shape. The coexistence pin in Spreadsheets/SpreadsheetProjectionBuildersParityTests.cs writes the
// real thing.
//
// The type argument is spelled in FULL, and must be: a using alias is resolved as if the other
// usings were not there, so `ProjectionBuilders<ISpace>` would not bind even with
// `using Unrect.Core;` two lines above. That is the same rule the shipping `using static` lives
// under, and the reason the import names the space in full there too.
using B = Unrect.Projections.ProjectionBuilders<Unrect.Core.ISpace>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <see cref="ProjectionBuilders{TSpace}"/> — the file-scoped vocabulary — against the
  /// <see cref="Projection"/> factories it re-exports. The class ships with no semantics of its own:
  /// every member is one line to the plain factory or to the scope, so the only things that can go
  /// wrong are a forwarder that drops an argument, a forwarder that captures its OWN parameter name
  /// instead of the caller's, and a factory that is added to the vocabulary and never re-exported.
  /// There is a section for each.
  /// <para>
  /// <b>Section 1 — value parity, mechanically.</b> Three theories cover all 84 members: one for the
  /// 39 that return a projection, read through the <see cref="Observations"/> harness at L3 over
  /// three grids (a well-formed ledger, one whose kinds are all wrong, and a sparse one); one
  /// for the 25 that hand back a strategy, a landmark or a <see cref="Unrect.Projections.Field"/>, compared at the
  /// strategy level — the selection, offset or area each computes over the same grids; and one for
  /// the 20 placement-pipeline entries, which hand back a <em>stage</em>. Failure
  /// parity is not a separate list: the hostile and sparse grids make most of these declarations
  /// fail, and a failure compared at L3 is compared down to its path, its subject and its
  /// diagnostics.
  /// </para>
  /// <para>
  /// <b>What a stage entry is compared against.</b> Geometry is now spelled only through the pipeline,
  /// so there is no postfix twin left to compare against: a stage is closed with a terminal and read
  /// against the SAME entry on <see cref="Projection"/> — <c>B.On(mark).Text()</c> against
  /// <c>On(mark).Text()</c>, <c>B.Heading(t).Range(…)</c> against <c>Heading(t).Range(…)</c>. This is a
  /// re-export parity pin: the builders entry forwards through the scope, the plain entry enters the
  /// pipeline directly, and the two paths must resolve the same placement. The pipeline's own laws —
  /// chained headings, the canonical order, the refusals — live in
  /// <see cref="PlacementPipelineLawTests"/>.
  /// </para>
  /// <para>
  /// <b>Section 2 — name capture, per site.</b> The four members that forward a
  /// <c>CallerArgumentExpression</c> get their own pins, because this is the one defect class a
  /// one-line forwarder can hide completely: drop the <c>declared</c> argument and the inner factory
  /// captures the text at the FORWARDING site — the forwarder's own parameter name — which reads
  /// like a correct capture wherever the caller's identifier happens to match it. As in
  /// <see cref="ProjectionScopeTests"/>, no identifier here is called <c>item</c> or <c>eachRow</c>.
  /// </para>
  /// <para>
  /// <b>Section 3 — the completeness covenant, made self-enforcing.</b> The class documentation
  /// promises that everything a declaration says is reachable from it, and that an operator which
  /// moves in the vocabulary moves here too. That promise is a reflection pin: every public static
  /// member of <c>Projection</c> is either matched here or named on an explicit exclusion list with
  /// its reason. It is the test that fails when someone adds a factory and forgets the re-export.
  /// </para>
  /// </summary>
  public class ProjectionBuildersParityTests
  {
    // --- The grids ---------------------------------------------------------------------------------
    //
    // Three, all 3x3, so no declaration below can run off an edge and every one of them is read over
    // all three. The ledger is what the declarations were written for; the hostile grid has the same
    // shape with every kind wrong, which is what turns the value theory into a failure theory
    // without a second list of cases; the sparse grid is where a discovered extent stops early.

    private static ISpace Ledger() => Mixed(new object?[,]
    {
      { "Fund", "Amount", "Units" },
      { "Alpha", 100m, 2 },
      { "Beta", 250m, 4 },
    });

    private static ISpace Hostile() => Mixed(new object?[,]
    {
      { 1m, 2m, 3m },
      { "x", "y", "z" },
      { true, "Beta", 9m },
    });

    private static ISpace Sparse() => Mixed(new object?[,]
    {
      { "Fund", null, null },
      { null, null, null },
      { "Beta", 250m, null },
    });

    private static IEnumerable<(string Name, ISpace Space)> Grids()
    {
      yield return ("ledger", Ledger());
      yield return ("hostile", Hostile());
      yield return ("sparse", Sparse());
    }

    /// <summary>What <c>Table&lt;T&gt;()</c> and the bind rung read the ledger into.</summary>
    public record Pair(string Fund, decimal Amount);

    /// <summary>A bind as a method group — the spelling the table rung recommends.</summary>
    private static IProjection<decimal> AmountByCaption(LabelMap captions) => Right(captions["Amount"]).Of(Decimal());

    /// <summary>The same bind pointed at the column of fund names, so every record fails.</summary>
    private static IProjection<decimal> FundColumnAsANumber(LabelMap captions) => Right(captions["Fund"]).Of(Decimal());

    // A matcher demanding nothing beyond ISpace. The published demanding matchers all demand a
    // capability (`RowWithFormula` demands IFormulaSpace) and IRowLandmark<in TSpace> is
    // contravariant, so none of them can stand where an IRowLandmark<ISpace> is wanted — and this
    // file's builders are closed over ISpace. The demand is a static-type fact with no run-time
    // shadow, so the least demanding witness is exactly the right one: it exercises the forwarder's
    // unwrap (`Required(landmark).Landmark`) and nothing else.

    private sealed class DemandingRow : IRowLandmark<ISpace>
    {
      internal DemandingRow(IRowLandmark landmark) => Landmark = landmark;

      public IRowLandmark Landmark { get; }
    }

    private sealed class DemandingColumn : IColumnLandmark<ISpace>
    {
      internal DemandingColumn(IColumnLandmark landmark) => Landmark = landmark;

      public IColumnLandmark Landmark { get; }
    }

    private static IRowLandmark<ISpace> Demanding(IRowLandmark landmark) => new DemandingRow(landmark);

    private static IColumnLandmark<ISpace> Demanding(IColumnLandmark landmark) => new DemandingColumn(landmark);

    // --- 1a. The 33 projection-returning members ---------------------------------------------------
    //
    // Each entry declares the same thing twice, once through the builders and once through the
    // vocabulary, with the argument text identical on both sides so a capture cannot differ for a
    // reason other than the forwarding. Both are read over every grid and compared at L3.

    // A twin returns whether the reading it just compared ENDED IN A FAILURE, which is what the
    // census below counts: an L3 comparison of two successful readings never looks at a path or a
    // subject, so a suite that only ever succeeded would be pinning half of what it claims to.
    private static readonly IReadOnlyDictionary<string, Func<ISpace, bool>> ProjectionTwins =
      new Dictionary<string, Func<ISpace, bool>>(StringComparer.Ordinal)
      {
        ["Boolean()"] = Reading(() => B.Boolean(), () => Boolean()),
        ["Caption(string)"] = Reading(() => B.Caption("Fund"), () => Caption("Fund")),
        ["Cell<T>(Func<CellValue, T>)"] = Reading(() => B.Cell(v => v.Kind.ToString()), () => Cell(v => v.Kind.ToString())),
        ["Choice<T>(IProjection<TSpace, T>[])"] = Reading(
          () => B.Choice(Caption("Total"), Text()),
          () => Choice(Caption("Total"), Text())),
        // The three labelled-axis primitives. ColumnLabels reads the header row (its value is a
        // LabelMap, rendered by the harness through its Labels); Record reads a body row by index, so
        // it needs no ambient labels; WithColumnLabels wraps a body that reads by index over a literal
        // map. All three re-export at their plain type, so the twin is the same declaration twice.
        ["ColumnLabels(int)"] = Reading(() => B.ColumnLabels(1), () => ColumnLabels(1)),
        ["Column<T>(Func<CellStrip, T>)"] = Reading(() => B.Column(s => s.Count), () => Column(s => s.Count)),
        ["Column<T>(int, Func<CellStrip, T>)"] = Reading(() => B.Column(2, s => s.Count), () => Column(2, s => s.Count)),
        ["Column<T>(IRowStrategy, Func<CellStrip, T>)"] = Reading(
          () => B.Column(TakeRows(2), s => s.Count),
          () => Column(TakeRows(2), s => s.Count)),
        ["Date()"] = Reading(() => B.Date(), () => Date()),
        ["Decimal()"] = Reading(() => B.Decimal(), () => Decimal()),
        ["Double()"] = Reading(() => B.Double(), () => Double()),
        ["Fields(Field[])"] = Reading(() => B.Fields(Field("Fund")), () => Fields(Field("Fund"))),
        ["HorizontalBands<T>(int, IProjection<T>, Nullable<BlankRowStrategy>, string)"] = Reading(
          () => { var quarter = Text(); return B.HorizontalBands(2, quarter); },
          () => { var quarter = Text(); return HorizontalBands(2, quarter); }),
        ["HorizontalFlow<T>(Layout<TSpace, T>)"] = Reading(
          () => B.HorizontalFlow(h => $"{h.Next(Text())}/{h.Next(Text())}"),
          () => HorizontalFlow(h => $"{h.Next(Text())}/{h.Next(Text())}")),
        ["HorizontalRepeat<T>(IProjection<TSpace, T>, IOffsetStrategy, int, string)"] = Reading(
          () => { var cell = Text(); return B.HorizontalRepeat(cell, separatedBy: BlankColumns(), atLeast: 1); },
          () => { var cell = Text(); return HorizontalRepeat(cell, separatedBy: BlankColumns(), atLeast: 1); }),
        ["Integer()"] = Reading(() => B.Integer(), () => Integer()),
        ["Overlay<T>(Layout<TSpace, T>)"] = Reading(
          () => B.Overlay(o => $"{o.Next(Text())}/{o.Next(Right(1).Of(Text()))}"),
          () => Overlay(o => $"{o.Next(Text())}/{o.Next(Right(1).Of(Text()))}")),
        ["Range<T>(Func<CellBlock, T>)"] = Reading(
          () => B.Range(b => b.Width * 100 + b.Height),
          () => Range(b => b.Width * 100 + b.Height)),
        ["Range<T>(IAreaStrategy, Func<CellBlock, T>)"] = Reading(
          () => B.Range(WholeExtent(), b => b.Width * 100 + b.Height),
          () => Range(WholeExtent(), b => b.Width * 100 + b.Height)),
        ["Range<T>(int, int, Func<CellBlock, T>)"] = Reading(
          () => B.Range(2, 2, b => b.Width * 100 + b.Height),
          () => Range(2, 2, b => b.Width * 100 + b.Height)),
        ["Record<T>(Func<TableRow, T>)"] = Reading(
          () => B.Record((TableRow r) => r.Count),
          () => Record((TableRow r) => r.Count)),
        ["Row<T>(Func<CellStrip, T>)"] = Reading(() => B.Row(s => s.Count), () => Row(s => s.Count)),
        ["Row<T>(int, Func<CellStrip, T>)"] = Reading(() => B.Row(2, s => s.Count), () => Row(2, s => s.Count)),
        ["Row<T>(IColumnStrategy, Func<CellStrip, T>)"] = Reading(
          () => B.Row(TakeColumns(2), s => s.Count),
          () => Row(TakeColumns(2), s => s.Count)),
        ["Table<T>()"] = Reading(() => B.Table<Pair>(), () => Table<Pair>()),
        ["Table()"] = Reading(() => B.Table(), () => Table()),
        ["Table<T>(Func<TableBinding<T>, TableBinding<T>>)"] = Reading(
          () => B.Table<Pair>(bind => bind.Column(p => p.Amount, "Amount")),
          () => Table<Pair>(bind => bind.Column(p => p.Amount, "Amount"))),
        ["Table<T>(Func<TableRow, T>)"] = Reading(() => B.Table((TableRow r) => r.Count), () => Table((TableRow r) => r.Count)),
        ["Table<T>(Func<TableView, T>)"] = Reading(
          () => B.Table((TableView t) => t.RowCount),
          () => Table((TableView t) => t.RowCount)),
        ["Table<T>(int, Func<TableRow, T>)"] = Reading(
          () => B.Table(1, (TableRow r) => r.Count),
          () => Table(1, (TableRow r) => r.Count)),
        ["Table<T>(int, Func<TableView, T>)"] = Reading(
          () => B.Table(1, (TableView t) => t.RowCount),
          () => Table(1, (TableView t) => t.RowCount)),
        ["Table<T>(int, Func<LabelMap, IProjection<TSpace, T>>, string)"] = Reading(
          () => B.Table(headerRows: 1, eachRow: AmountByCaption),
          () => Table(headerRows: 1, eachRow: AmountByCaption)),
        ["Table<T>(int, IProjection<TSpace, T>, string)"] = Reading(
          () => { var amountRow = Right(1).Of(Decimal()); return B.Table(headerRows: 1, eachRow: amountRow); },
          () => { var amountRow = Right(1).Of(Decimal()); return Table(headerRows: 1, eachRow: amountRow); }),
        // The six onBlank/blankRecord Table rungs. They forward one-to-one exactly as the Stop-path
        // rungs above do, so a Skip strategy (run-to-edge, non-self-bounding) reads identically
        // through both spellings over every grid; the blankRecord rungs pass a lambda where the
        // onBlank ones pass a strategy value, which is what tells the two Table/2 (and Table/3) pairs
        // apart at the call site.
        ["Table<T>(BlankRowStrategy)"] = Reading(() => B.Table<Pair>(BlankRowStrategy.Skip), () => Table<Pair>(BlankRowStrategy.Skip)),
        ["Table<T>(Func<TableBinding<T>, TableBinding<T>>, BlankRowStrategy)"] = Reading(
          () => B.Table<Pair>(bind => bind.Column(p => p.Amount, "Amount"), BlankRowStrategy.Skip),
          () => Table<Pair>(bind => bind.Column(p => p.Amount, "Amount"), BlankRowStrategy.Skip)),
        ["Table<T>(Func<TableRow, T>, BlankRowStrategy)"] = Reading(
          () => B.Table((TableRow r) => r.Count, BlankRowStrategy.Skip),
          () => Table((TableRow r) => r.Count, BlankRowStrategy.Skip)),
        ["Table<T>(Func<TableRow, T>, Func<TableRow, T>)"] = Reading(
          () => B.Table((TableRow r) => r.Count, blankRecord: _ => -1),
          () => Table((TableRow r) => r.Count, blankRecord: _ => -1)),
        ["Table<T>(int, Func<TableRow, T>, BlankRowStrategy)"] = Reading(
          () => B.Table(1, (TableRow r) => r.Count, BlankRowStrategy.Skip),
          () => Table(1, (TableRow r) => r.Count, BlankRowStrategy.Skip)),
        ["Table<T>(int, Func<TableRow, T>, Func<TableRow, T>)"] = Reading(
          () => B.Table(1, (TableRow r) => r.Count, blankRecord: _ => -1),
          () => Table(1, (TableRow r) => r.Count, blankRecord: _ => -1)),
        ["Text()"] = Reading(() => B.Text(), () => Text()),
        ["VerticalBands<T>(int, IProjection<T>, Nullable<BlankRowStrategy>, string)"] = Reading(
          () => { var line = Text(); return B.VerticalBands(1, line, onBlank: BlankRowStrategy.Tolerate); },
          () => { var line = Text(); return VerticalBands(1, line, onBlank: BlankRowStrategy.Tolerate); }),
        ["VerticalFlow<T>(Layout<TSpace, T>)"] = Reading(
          () => B.VerticalFlow(v => $"{v.Next(Text())}/{v.Next(Text())}"),
          () => VerticalFlow(v => $"{v.Next(Text())}/{v.Next(Text())}")),
        ["VerticalRepeat<T>(IProjection<TSpace, T>, IOffsetStrategy, int, string)"] = Reading(
          () => { var line = Text(); return B.VerticalRepeat(line, separatedBy: BlankRows(), atLeast: 1); },
          () => { var line = Text(); return VerticalRepeat(line, separatedBy: BlankRows(), atLeast: 1); }),
        ["WithColumnLabels<T>(LabelMap, IProjection<T>)"] = Reading(
          () => B.WithColumnLabels(LabelMap.Of(("Fund", 0)), Record((TableRow r) => r.Count)),
          () => WithColumnLabels(LabelMap.Of(("Fund", 0)), Record((TableRow r) => r.Count))),
      };

    // --- 1b. The 25 that hand back something other than a projection -------------------------------
    //
    // A strategy, a landmark or a Field has no reading to observe, so parity is stated where the
    // value is used: the rows a row strategy selects, the offset an offset strategy computes, the
    // area an area strategy measures, the index a landmark finds. Same grids, same arguments, and a
    // throw is compared as a value so a guard cannot move on one side only.

    private static readonly IReadOnlyDictionary<string, Action<ISpace>> StrategyTwins =
      new Dictionary<string, Action<ISpace>>(StringComparer.Ordinal)
      {
        ["AllColumns()"] = Strategy(s => B.AllColumns().SelectColumns(s), s => AllColumns().SelectColumns(s)),
        ["AllRows()"] = Strategy(s => B.AllRows().SelectRows(s), s => AllRows().SelectRows(s)),
        ["BlankColumns()"] = Strategy(s => B.BlankColumns().GetOffset(s), s => BlankColumns().GetOffset(s)),
        ["BlankRows()"] = Strategy(s => B.BlankRows().GetOffset(s), s => BlankRows().GetOffset(s)),
        ["ColumnContaining(string)"] = Strategy(
          s => B.ColumnContaining("Amount").FindColumn(s),
          s => ColumnContaining("Amount").FindColumn(s)),
        ["ColumnsWhileAny(Func<CellValue, bool>)"] = Strategy(
          s => B.ColumnsWhileAny(v => v.HasValue).GetArea(s),
          s => ColumnsWhileAny(v => v.HasValue).GetArea(s)),
        ["ColumnsWhileAnyValue()"] = Strategy(s => B.ColumnsWhileAnyValue().GetArea(s), s => ColumnsWhileAnyValue().GetArea(s)),
        ["ColumnWhere(Func<ISpace, int, bool>)"] = Strategy(
          s => B.ColumnWhere((space, index) => index == space.Area.Width - 1).FindColumn(s),
          s => ColumnWhere((space, index) => index == space.Area.Width - 1).FindColumn(s)),
        ["ColumnWithCell(Func<CellValue, bool>)"] = Strategy(
          s => B.ColumnWithCell(v => v.Kind == CellKind.Number).FindColumn(s),
          s => ColumnWithCell(v => v.Kind == CellKind.Number).FindColumn(s)),
        ["Extent(int, int)"] = Strategy(s => B.Extent(2, 2).GetArea(s), s => Extent(2, 2).GetArea(s)),
        // Space-indifferent, and the only member of the class that hands back neither a projection
        // nor a strategy: what a Field carries is its label, which is what keys a Fields result.
        ["Field(string)"] = Strategy(_ => B.Field("EIN").Label, _ => Field("EIN").Label),
        ["FromBottom(int)"] = Strategy(s => B.FromBottom(1).GetOffset(s), s => FromBottom(1).GetOffset(s)),
        ["FromRight(int)"] = Strategy(s => B.FromRight(1).GetOffset(s), s => FromRight(1).GetOffset(s)),
        ["NoExtent()"] = Strategy(s => B.NoExtent().GetArea(s), s => NoExtent().GetArea(s)),
        ["RowContaining(string)"] = Strategy(s => B.RowContaining("Beta").FindRow(s), s => RowContaining("Beta").FindRow(s)),
        ["RowsWhileAny(Func<CellValue, bool>)"] = Strategy(
          s => B.RowsWhileAny(v => v.HasValue).GetArea(s),
          s => RowsWhileAny(v => v.HasValue).GetArea(s)),
        ["RowsWhileAnyValue()"] = Strategy(s => B.RowsWhileAnyValue().GetArea(s), s => RowsWhileAnyValue().GetArea(s)),
        ["RowWhere(Func<ISpace, int, bool>)"] = Strategy(
          s => B.RowWhere((space, index) => index == space.Area.Height - 1).FindRow(s),
          s => RowWhere((space, index) => index == space.Area.Height - 1).FindRow(s)),
        ["RowWithCell(Func<CellValue, bool>)"] = Strategy(
          s => B.RowWithCell(v => v.Kind == CellKind.Number).FindRow(s),
          s => RowWithCell(v => v.Kind == CellKind.Number).FindRow(s)),
        ["SkipColumns(int)"] = Strategy(s => B.SkipColumns(1).GetOffset(s), s => SkipColumns(1).GetOffset(s)),
        ["SkipRows(int)"] = Strategy(s => B.SkipRows(1).GetOffset(s), s => SkipRows(1).GetOffset(s)),
        ["TakeColumns(int)"] = Strategy(s => B.TakeColumns(2).SelectColumns(s), s => TakeColumns(2).SelectColumns(s)),
        ["TakeRows(int)"] = Strategy(s => B.TakeRows(2).SelectRows(s), s => TakeRows(2).SelectRows(s)),
        ["Then(IOffsetStrategy[])"] = Strategy(
          s => B.Then(SkipRows(1), SkipColumns(1)).GetOffset(s),
          s => Then(SkipRows(1), SkipColumns(1)).GetOffset(s)),
        ["WholeExtent()"] = Strategy(s => B.WholeExtent().GetArea(s), s => WholeExtent().GetArea(s)),
      };

    // --- 1c. The 20 placement-pipeline entries -----------------------------------------------------
    //
    // A stage is not a value anyone reads, so each entry is closed with a terminal and compared
    // against the SAME entry on Projection — geometry is spelled only through the pipeline now, so the
    // postfix twin is gone. What this pins is the re-export: the builders entry forwards through the
    // scope, the plain entry enters the pipeline directly, and the two must resolve the same
    // placement over every grid.
    //
    // The subject is Text() wherever the entry moves the section and a region read as its own
    // measurements wherever it changes the extent, because a bound written on a leaf that reads one
    // cell has nothing to show for itself.

    private static readonly IReadOnlyDictionary<string, Func<ISpace, bool>> StageTwins =
      new Dictionary<string, Func<ISpace, bool>>(StringComparer.Ordinal)
      {
        ["AfterBlankColumns()"] = Reading(() => B.AfterBlankColumns().Text(), () => AfterBlankColumns().Text()),
        ["AfterBlankRows()"] = Reading(() => B.AfterBlankRows().Text(), () => AfterBlankRows().Text()),

        // The lazy corner heuristic — the offset entry added with the strategy. Like the AfterBlank*
        // twins it moves the section, so Text() is the terminal that shows the move.
        ["SkipToFirstNonBlankCell()"] = Reading(() => B.SkipToFirstNonBlankCell().Text(), () => SkipToFirstNonBlankCell().Text()),
        ["Below(IRowLandmark)"] = Reading(
          () => B.Below(RowContaining("Fund")).Text(),
          () => Below(RowContaining("Fund")).Text()),
        ["Below(IRowLandmark<TSpace>)"] = Raised(
          () => B.Below(Demanding(RowContaining("Fund"))).Text(),
          () => Below(Demanding(RowContaining("Fund"))).Text()),
        ["Down(int)"] = Reading(() => B.Down(1).Text(), () => Down(1).Text()),

        // The one entry spelled with a different WORD from the leaf it announces: a heading is a
        // caption the library mints and a vertical flow it builds, which is the whole of the L3-by-
        // construction claim in PlacementPipelineLawTests, said here as a value.
        ["Heading(string)"] = Reading(
          () => B.Heading("Fund").Range(b => $"{b.Width}x{b.Height}"),
          () => Heading("Fund").Range(b => $"{b.Width}x{b.Height}")),

        ["OffsetBy(IOffsetStrategy)"] = Reading(
          () => B.OffsetBy(SkipRows(1)).Text(),
          () => OffsetBy(SkipRows(1)).Text()),
        ["On(IColumnLandmark)"] = Reading(
          () => B.On(ColumnContaining("Amount")).Text(),
          () => On(ColumnContaining("Amount")).Text()),
        ["On(IColumnLandmark<TSpace>)"] = Raised(
          () => B.On(Demanding(ColumnContaining("Amount"))).Text(),
          () => On(Demanding(ColumnContaining("Amount"))).Text()),
        ["On(IRowLandmark)"] = Reading(() => B.On(RowContaining("Beta")).Text(), () => On(RowContaining("Beta")).Text()),
        ["On(IRowLandmark<TSpace>)"] = Raised(
          () => B.On(Demanding(RowContaining("Beta"))).Text(),
          () => On(Demanding(RowContaining("Beta"))).Text()),
        ["Right(int)"] = Reading(() => B.Right(1).Text(), () => Right(1).Text()),
        ["RightOf(IColumnLandmark)"] = Reading(
          () => B.RightOf(ColumnContaining("Fund")).Text(),
          () => RightOf(ColumnContaining("Fund")).Text()),
        ["RightOf(IColumnLandmark<TSpace>)"] = Raised(
          () => B.RightOf(Demanding(ColumnContaining("Fund"))).Text(),
          () => RightOf(Demanding(ColumnContaining("Fund"))).Text()),

        // The size-only entry, added when postfix geometry was retired — a region sized to an extent
        // and placed at adjacency. No postfix twin exists to compare against; this is a direct pin of
        // the re-export against the plain Projection factory, read as geometry over a region.
        ["Sized(IAreaStrategy)"] = Reading(
          () => B.Sized(Extent(2, 2)).Range(b => $"{b.Width}x{b.Height}"),
          () => Sized(Extent(2, 2)).Range(b => $"{b.Width}x{b.Height}")),

        // The one-word entry for the instinct the default refuses, against the two words it stands
        // for. Its twin is therefore a COMPOSITION rather than a single entry — which is exactly the
        // claim the sugar makes.
        ["SkipEmptyRowsAndColumns()"] = Reading(
          () => B.SkipEmptyRowsAndColumns().Text(),
          () => AfterBlankRows().AfterBlankColumns().Text()),

        ["Until(IRowLandmark, bool)"] = Reading(
          () => B.Until(RowContaining("Beta")).Range(b => $"{b.Width}x{b.Height}"),
          () => Until(RowContaining("Beta")).Range(b => $"{b.Width}x{b.Height}")),
        ["Until(IRowLandmark<TSpace>, bool)"] = Raised(
          () => B.Until(Demanding(RowContaining("Beta"))).Range(b => $"{b.Width}x{b.Height}"),
          () => Until(Demanding(RowContaining("Beta"))).Range(b => $"{b.Width}x{b.Height}")),
        ["UntilColumn(IColumnLandmark, bool)"] = Reading(
          () => B.UntilColumn(ColumnContaining("Amount")).Range(b => $"{b.Width}x{b.Height}"),
          () => UntilColumn(ColumnContaining("Amount")).Range(b => $"{b.Width}x{b.Height}")),
        ["UntilColumn(IColumnLandmark<TSpace>, bool)"] = Raised(
          () => B.UntilColumn(Demanding(ColumnContaining("Amount"))).Range(b => $"{b.Width}x{b.Height}"),
          () => UntilColumn(Demanding(ColumnContaining("Amount"))).Range(b => $"{b.Width}x{b.Height}")),
      };

    public static TheoryData<string> TheProjectionReturningMembers => Keys(ProjectionTwins);

    public static TheoryData<string> TheStrategyReturningMembers => Keys(StrategyTwins);

    public static TheoryData<string> ThePipelineEntries => Keys(StageTwins);

    [Theory]
    [MemberData(nameof(TheProjectionReturningMembers))]
    public void AProjectionReturningMemberReadsAsItsPlainTwin(string member)
      => OverEveryGrid(ProjectionTwins[member]);

    [Theory]
    [MemberData(nameof(TheStrategyReturningMembers))]
    public void AStrategyReturningMemberComputesWhatItsPlainTwinComputes(string member)
      => OverEveryGrid(StrategyTwins[member]);

    [Theory]
    [MemberData(nameof(ThePipelineEntries))]
    public void APipelineEntryClosedByATerminalReadsAsItsPostfixTwin(string member)
      => OverEveryGrid(StageTwins[member]);

    [Fact]
    public void AndTheTheoryAboveComparesFailuresAndNotOnlySuccesses()
    {
      // The census, in the house style: an L3 comparison of two SUCCESSFUL readings never reaches the
      // failure's path or its subject, so the theory's claim to compare "at the strongest observable
      // level" rests on the hostile and sparse grids actually breaking things. The floors are set
      // under the counts observed when this was written — 19 of the 33 members fail on at least one
      // grid, and 33 of the 99 member-grid readings fail — so this catches a rewrite that quietly
      // made every declaration succeed rather than recording today's numbers.
      var failing = ProjectionTwins.Values.Select(OverEveryGrid).ToList();

      Assert.True(failing.Count(count => count > 0) >= 15, $"only {failing.Count(count => count > 0)} members ever fail");
      Assert.True(failing.Sum() >= 27, $"only {failing.Sum()} of the {failing.Count * 3} readings fail");
    }

    [Fact]
    public void AndTheStageTheoryComparesFailuresAndNotOnlySuccesses()
    {
      // The same census for the pipeline entries, and it matters more here than above: a stage twin
      // that succeeded everywhere would compare two readings whose paths and subjects are never
      // looked at, which is most of what the pipeline could get wrong. The hostile grid has no
      // "Fund" and no "Amount", so every matcher-taking entry misses on it; the sparse grid has the
      // rows but blanks where the leaf expects text. Floors under the counts observed when this was
      // written (of the 20 entries, most fail on at least one grid).
      var failing = StageTwins.Values.Select(OverEveryGrid).ToList();

      Assert.True(failing.Count(count => count > 0) >= 15, $"only {failing.Count(count => count > 0)} entries ever fail");
      Assert.True(failing.Sum() >= 22, $"only {failing.Sum()} of the {failing.Count * 3} readings fail");
    }

    [Fact]
    public void AndTheThreeTablesTogetherCoverEveryMemberOfTheClass()
    {
      // The pin that stops the three theories above from quietly under-covering: a member added to
      // the builders and left untwinned fails HERE rather than going unread. Together with the
      // covenant in section 3 this closes the chain — a new Projection factory forces a re-export,
      // and a new re-export forces a twin.
      var covered = ProjectionTwins.Keys
        .Concat(StrategyTwins.Keys)
        .Concat(StageTwins.Keys)
        .OrderBy(k => k, StringComparer.Ordinal);

      Assert.Equal(Signatures(typeof(ProjectionBuilders<>)).OrderBy(k => k, StringComparer.Ordinal), covered);
    }

    [Fact]
    public void AndTheGuardsRefuseInThePlainFamilysWords()
    {
      // The failures that happen where the declaration is WRITTEN, which no reading can reach: the
      // theories above only ever construct valid declarations. Representative rather than exhaustive
      // — one per kind of guard the class forwards through.
      SameRefusal(() => B.SkipRows(-1), () => SkipRows(-1));
      SameRefusal(() => B.SkipColumns(-1), () => SkipColumns(-1));
      SameRefusal(() => B.TakeRows(-1), () => TakeRows(-1));
      SameRefusal(() => B.Extent(-1, 1), () => Extent(-1, 1));
      SameRefusal(() => B.Field(""), () => Field(""));
      SameRefusal(() => B.Fields(null!), () => Fields(null!));
      SameRefusal(() => B.Then(null!), () => Then(null!));
      SameRefusal(() => B.Caption(null!), () => Caption(null!));
      SameRefusal(() => B.Cell<int>(null!), () => Cell<int>(null!));
      SameRefusal(() => B.VerticalFlow<int>(null!), () => VerticalFlow<int>(null!));
      SameRefusal(() => B.Overlay<int>(null!), () => Overlay<int>(null!));
      SameRefusal(() => B.VerticalRepeat((IProjection<ISpace, int>)null!), () => VerticalRepeat((IProjection<int>)null!));
      SameRefusal(() => B.HorizontalRepeat(Text(), atLeast: -1), () => HorizontalRepeat(Text(), atLeast: -1));
      SameRefusal(() => B.Choice(Text()), () => Choice(Text()));
      SameRefusal(() => B.Table(headerRows: 2, eachRow: Text()), () => Table(headerRows: 2, eachRow: Text()));
      SameRefusal(
        () => B.Table(headerRows: 0, eachRow: AmountByCaption),
        () => Table(headerRows: 0, eachRow: AmountByCaption));
    }

    [Fact]
    public void AndClosingTheClassOverACapableSpaceIsTheWitnessFormSaidOnce()
    {
      // The other half of what the class is for, and the one place a `TSpace` other than ISpace is
      // exercised: closed over a capability, the layouts are the witness spelling with the witness
      // moved into the using directive. Written through the full type name rather than the alias,
      // because this is the only test that needs a second closing.
      var sheet = new FormulaGridSpace(
        new[,] { { CellValue.Of("Fund") }, { CellValue.Of(100m) } },
        new string?[2, 1]);

      var throughBuilders = ProjectionBuilders<IFormulaSpace>.VerticalFlow(v => $"{v.Next(Text())}/{v.Next(Decimal())}");
      var throughWitness = VerticalFlow(SpreadsheetProjections.Formulas, v => $"{v.Next(Text())}/{v.Next(Decimal())}");

      Assert.Equal("Fund/100", throughBuilders.Map(sheet));
      Assert.Equal(throughWitness.Map(sheet), throughBuilders.Map(sheet));
      Assert.Equal(throughWitness.Description, throughBuilders.Description);
    }

    // --- 2. Name capture, one pin per forwarding site ----------------------------------------------
    //
    // Four members forward a [CallerArgumentExpression]. None of the identifiers below is called
    // `item` or `eachRow` — a test written with those would pass straight through the bug it exists
    // to catch, because a forwarder that dropped the argument would capture its own parameter name
    // and be indistinguishable.

    [Fact]
    public void AVerticalRepeatsItemKeepsTheIdentifierItWasWrittenAs()
    {
      var investorDetail = Decimal();

      var throughBuilders = Assert.Throws<ProjectionException>(() => B.VerticalRepeat(investorDetail).Map(Ledger()));
      var plain = Assert.Throws<ProjectionException>(() => VerticalRepeat(investorDetail).Map(Ledger()));

      Assert.Equal("'investorDetail'", throughBuilders.Subject);
      Assert.Equal("VerticalRepeat[0] -> 'investorDetail' (Decimal)", throughBuilders.Path);
      Assert.Equal(plain.Path, throughBuilders.Path);
      Assert.Equal(plain.Message, throughBuilders.Message);
    }

    [Fact]
    public void AndSoDoesAHorizontalRepeatsItem()
    {
      var quarterlyColumn = Decimal();

      var throughBuilders = Assert.Throws<ProjectionException>(() => B.HorizontalRepeat(quarterlyColumn).Map(Ledger()));
      var plain = Assert.Throws<ProjectionException>(() => HorizontalRepeat(quarterlyColumn).Map(Ledger()));

      Assert.Equal("HorizontalRepeat[0] -> 'quarterlyColumn' (Decimal)", throughBuilders.Path);
      Assert.Equal(plain.Path, throughBuilders.Path);
      Assert.Equal(plain.Message, throughBuilders.Message);
    }

    [Fact]
    public void AndATablesRowSlot()
    {
      var allocationRow = Decimal();

      var throughBuilders = Assert.Throws<ProjectionException>(
        () => B.Table(headerRows: 1, eachRow: allocationRow).Map(Ledger()));

      var plain = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 1, eachRow: allocationRow).Map(Ledger()));

      Assert.Equal("Table[0] -> 'allocationRow' (Decimal)", throughBuilders.Path);
      Assert.Equal(plain.Path, throughBuilders.Path);
      Assert.Equal(plain.Message, throughBuilders.Message);
    }

    [Fact]
    public void AndATablesBind_LabelledByTheMethodGroupItWasPassedAs()
    {
      var throughBuilders = Assert.Throws<ProjectionException>(
        () => B.Table(headerRows: 1, eachRow: FundColumnAsANumber).Map(Ledger()));

      var plain = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 1, eachRow: FundColumnAsANumber).Map(Ledger()));

      Assert.Equal("Table[0] -> 'FundColumnAsANumber' (Decimal)", throughBuilders.Path);
      Assert.Equal(plain.Path, throughBuilders.Path);
      Assert.Equal(plain.Message, throughBuilders.Message);
    }

    [Fact]
    public void AndAnExplicitNameStillOutranksTheIdentifier()
    {
      var investorDetail = Decimal().Named("detail");

      var failure = Assert.Throws<ProjectionException>(() => B.VerticalRepeat(investorDetail).Map(Ledger()));

      Assert.Equal("VerticalRepeat[0] -> 'detail' (Decimal)", failure.Path);
    }

    // --- 3. The completeness covenant ---------------------------------------------------------------

    /// <summary>
    /// The nine public static members of <see cref="Projection"/> that are deliberately NOT
    /// re-exported, each with the reason it cannot be. Rendered exactly as <see cref="Signature"/>
    /// writes them, so a member that changes shape drops off the list and the covenant pin reports
    /// it rather than silently keeping it excluded.
    /// </summary>
    private static readonly IReadOnlyList<string> TheDeliberateExclusions = new[]
    {
      // The door onto the scope. A file that has imported the closed class has already answered the
      // question `Over` asks, and opening a second scope inside it is the mixed-space file the
      // dichotomy says wants splitting.
      "Over<TSpace>()",

      // The three witness layouts. The witness exists to carry TSpace in an ARGUMENT where inference
      // can read it; the closed class carries it in the class, so the argument has nothing to add.
      "VerticalFlow<TSpace, T>(Demand<TSpace>, Layout<TSpace, T>)",
      "HorizontalFlow<TSpace, T>(Demand<TSpace>, Layout<TSpace, T>)",
      "Overlay<TSpace, T>(Demand<TSpace>, Layout<TSpace, T>)",

      // The five <TSpace, T> typed forms, for the same reason said the other way: their first type
      // argument is exactly what the using directive already fixed, so re-exporting them would ask
      // the file to name its space twice.
      "VerticalRepeat<TSpace, T>(IProjection<TSpace, T>, IOffsetStrategy, int, string)",
      "HorizontalRepeat<TSpace, T>(IProjection<TSpace, T>, IOffsetStrategy, int, string)",
      "Choice<TSpace, T>(IProjection<TSpace, T>[])",
      "Table<TSpace, T>(int, IProjection<TSpace, T>, string)",
      "Table<TSpace, T>(int, Func<LabelMap, IProjection<TSpace, T>>, string)",
    };

    [Fact]
    public void EveryExclusionIsARealMemberOfTheVocabulary()
    {
      // The list above is only as good as its accuracy: an exclusion that no longer names anything
      // would silently widen into a licence to omit whatever comes to share its shape.
      var vocabulary = Signatures(typeof(Projection));

      Assert.All(TheDeliberateExclusions, excluded => Assert.Contains(excluded, vocabulary));
    }

    [Fact]
    public void AndEveryOtherProjectionFactoryIsReachableFromTheBuilders()
    {
      // THE COVENANT. Every public static member of Projection, minus the nine, is matched by a
      // member of the closed class with the same name and the same number of parameters — matched as
      // a MULTISET, so an overload cannot go missing behind a sibling that shares its name. The
      // signatures themselves cannot be compared directly, since the whole point of the class is
      // that TSpace replaces ISpace in the members that take a projection.
      //
      // This is the test that fails when a factory is added to the vocabulary and not re-exported.
      var expected = Signatures(typeof(Projection))
        .Where(signature => !TheDeliberateExclusions.Contains(signature))
        .Select(Shape)
        .OrderBy(shape => shape, StringComparer.Ordinal);

      var actual = Signatures(typeof(ProjectionBuilders<>))
        .Select(Shape)
        .OrderBy(shape => shape, StringComparer.Ordinal);

      Assert.Equal(expected, actual);
    }

    [Fact]
    public void AndNoNameIsMissingAltogether()
    {
      // The same claim at the coarsest grain, kept because it is the one a reader can check by eye
      // and the one whose failure message names the operator rather than a shape key. `Over` is the
      // only NAME with nothing behind it on the closed class; every other exclusion is an overload
      // of a name that is re-exported.
      var reachable = Signatures(typeof(ProjectionBuilders<>)).Select(Name).ToList();

      var missing = Signatures(typeof(Projection))
        .Select(Name)
        .Distinct(StringComparer.Ordinal)
        .Where(name => !reachable.Contains(name, StringComparer.Ordinal))
        .OrderBy(name => name, StringComparer.Ordinal);

      Assert.Equal(new[] { "Over" }, missing);
    }

    // --- 4. The boundary the class deliberately does not cross --------------------------------------

    [Fact]
    public void ALeafComesBackPlain_SoThePostfixHalfStillReachesIt()
    {
      // Only the members that TAKE a projection are closed over TSpace; a leaf is re-exported at its
      // plain type on purpose. This is that decision's consequence, pinned positively: OrBlank is
      // written against IProjection<T>, so a leaf raised to IProjection<TSpace, T> would be out of
      // its reach and `B.Decimal().OrBlank()` would be CS1929 — "IProjection<ISpace, decimal> does
      // not contain a definition for OrBlank". It compiles, and it reads a blank as null.
      IProjection<decimal> leaf = B.Decimal();
      IProjection<decimal?> tolerant = leaf.OrBlank();

      Assert.Equal(100m, leaf.Map(Ledger().GetSubspace(new Offset(1, 1), new Area(1, 1))));
      Assert.Null(tolerant.Map(Sparse().GetSubspace(new Offset(1, 1), new Area(1, 1))));

      // ...and the same for the other five typed leaves and the untyped one, which share the rule.
      Assert.IsAssignableFrom<IProjection<string>>(B.Text());
      Assert.IsAssignableFrom<IProjection<int>>(B.Integer());
      Assert.IsAssignableFrom<IProjection<double>>(B.Double());
      Assert.IsAssignableFrom<IProjection<DateTime>>(B.Date());
      Assert.IsAssignableFrom<IProjection<bool>>(B.Boolean());
      Assert.IsAssignableFrom<IProjection<string>>(B.Caption("Fund"));
    }

    // --- The machinery -------------------------------------------------------------------------------

    /// <summary>
    /// Runs one twin over all three grids and reports how many of the three readings ended in a
    /// failure — see the census. A comparison that blows up is re-thrown naming the grid, since
    /// otherwise a difference reads as an anonymous mismatch in one of three identical-looking runs.
    /// </summary>
    private static int OverEveryGrid(Func<ISpace, bool> twin)
    {
      var failed = 0;

      foreach (var (name, space) in Grids())
      {
        try
        {
          if (twin(space))
            failed++;
        }
        catch (Exception failure) when (!(failure is XunitException named && named.Message.StartsWith("over the ", StringComparison.Ordinal)))
        {
          throw new XunitException($"over the {name} grid: {failure.Message}");
        }
      }

      return failed;
    }

    private static void OverEveryGrid(Action<ISpace> twin)
      => OverEveryGrid(space =>
      {
        twin(space);

        return false;
      });

    /// <summary>
    /// One member's twin: the same declaration written twice, read over a grid, and compared at the
    /// strongest level there is. Both sides are built inside the comparison rather than passed in
    /// already built, so a factory that refuses at CONSTRUCTION is compared too.
    /// </summary>
    private static Func<ISpace, bool> Reading<T>(
      Func<IProjection<ISpace, T>> throughBuilders,
      Func<IProjection<T>> throughProjection)
      => space =>
      {
        var (plain, plainFault) = Read(throughProjection, space);
        var (builders, buildersFault) = Read(() => (IProjection<T>)throughBuilders(), space);

        Assert.Equal(Describe(plainFault), Describe(buildersFault));

        if (plain is not null && builders is not null)
          Observations.AssertL3(plain, builders);

        return plainFault is not null || plain?.Failure is not null;
      };

    /// <summary>
    /// The same comparison where the postfix twin comes back DEMANDING — which the demand-raising
    /// lifts all do, since they hand back <c>IProjection&lt;TSpace, T&gt;</c> rather than the
    /// receiver's own type. The demand lives only in the static type and every projection this
    /// library makes implements <c>IProjection&lt;T&gt;</c>, so forgetting it here to read the
    /// declaration is the same cast the typed layer itself makes.
    /// </summary>
    private static Func<ISpace, bool> Raised<T>(
      Func<IProjection<ISpace, T>> throughBuilders,
      Func<IProjection<ISpace, T>> throughProjection)
      => Reading(throughBuilders, () => (IProjection<T>)throughProjection());

    private static (Observation? Reading, Exception? Fault) Read<T>(Func<IProjection<T>> declare, ISpace space)
    {
      try
      {
        return (Observations.Observe(declare(), space), null);
      }
      catch (Exception fault)
      {
        return (null, fault);
      }
    }

    /// <summary>
    /// One non-projection member's twin: what it computes over a grid, with a throw rendered as a
    /// value so a guard that moved on one side only reads as a difference rather than as an error.
    /// </summary>
    private static Action<ISpace> Strategy<T>(Func<ISpace, T> throughBuilders, Func<ISpace, T> throughProjection)
      => space => Assert.Equal(Attempt(() => throughProjection(space)), Attempt(() => throughBuilders(space)));

    private static void SameRefusal(Action throughBuilders, Action throughProjection)
    {
      var expected = Attempt(throughProjection);

      Assert.NotEqual("<no failure>", expected);
      Assert.Equal(expected, Attempt(throughBuilders));
    }

    private static string Attempt<T>(Func<T> read)
    {
      try
      {
        return Render(read());
      }
      catch (Exception failure)
      {
        return $"{failure.GetType().Name}: {failure.Message}";
      }
    }

    private static string Attempt(Action call)
    {
      try
      {
        call();

        return "<no failure>";
      }
      catch (Exception failure)
      {
        return $"{failure.GetType().Name}: {failure.Message}";
      }
    }

    private static string Describe(Exception? failure)
      => failure is null ? "<none>" : $"{failure.GetType().Name}: {failure.Message}";

    private static string Render(object? value) => value switch
    {
      null => "<none>",
      Area area => $"{area.Width}x{area.Height}",
      Offset offset => $"{offset.Width}x{offset.Height}",
      _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private static TheoryData<string> Keys<TTwin>(IReadOnlyDictionary<string, TTwin> twins)
    {
      var data = new TheoryData<string>();

      foreach (var key in twins.Keys.OrderBy(key => key, StringComparer.Ordinal))
        data.Add(key);

      return data;
    }

    // --- Reflection: how a vocabulary is enumerated and how two of them are compared ------------------

    /// <summary>
    /// Every public static member a declaration could write, rendered as a signature. Property
    /// accessors are dropped and the property itself kept under its own name, because a witness such
    /// as <c>Formulas</c> is written without parentheses and is as much of the vocabulary as a
    /// factory is.
    /// </summary>
    internal static IReadOnlyCollection<string> Signatures(Type vocabulary)
    {
      const BindingFlags Declared = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

      var methods = vocabulary
        .GetMethods(Declared)
        .Where(method => !method.IsSpecialName)
        .Select(Signature);

      var properties = vocabulary.GetProperties(Declared).Select(property => property.Name);

      return methods.Concat(properties).ToList();
    }

    private static string Signature(MethodInfo method)
    {
      var arguments = method.IsGenericMethodDefinition
        ? "<" + string.Join(", ", method.GetGenericArguments().Select(argument => argument.Name)) + ">"
        : string.Empty;

      return method.Name
        + arguments
        + "(" + string.Join(", ", method.GetParameters().Select(parameter => Render(parameter.ParameterType))) + ")";
    }

    /// <summary>
    /// A signature reduced to what the two classes can be expected to agree on: the name and the
    /// parameter count. The types themselves cannot be compared — <c>TSpace</c> stands where
    /// <c>ISpace</c> stood in every member that takes a projection, which is the class's entire
    /// reason to exist.
    /// </summary>
    private static string Shape(string signature)
    {
      var open = signature.IndexOf('(');

      if (open < 0)
        return signature + "/property";

      var parameters = signature.Substring(open + 1, signature.Length - open - 2);
      var depth = 0;
      var count = parameters.Length == 0 ? 0 : 1;

      foreach (var character in parameters)
      {
        if (character == '<')
          depth++;
        else if (character == '>')
          depth--;
        else if (character == ',' && depth == 0)
          count++;
      }

      return Name(signature) + "/" + count;
    }

    private static string Name(string signature)
    {
      var end = signature.IndexOfAny(new[] { '<', '(' });

      return end < 0 ? signature : signature.Substring(0, end);
    }

    private static string Render(Type type)
    {
      if (type.IsGenericParameter)
        return type.Name;

      if (type.IsArray)
        return Render(type.GetElementType()!) + "[]";

      if (type.IsGenericType)
      {
        var name = type.Name;
        var tick = name.IndexOf('`');

        return (tick < 0 ? name : name.Substring(0, tick))
          + "<" + string.Join(", ", type.GetGenericArguments().Select(Render)) + ">";
      }

      // The keyword spellings, so the exclusion list above reads the way the source does.
      return type == typeof(int) ? "int"
        : type == typeof(string) ? "string"
        : type == typeof(bool) ? "bool"
        : type == typeof(double) ? "double"
        : type == typeof(decimal) ? "decimal"
        : type == typeof(object) ? "object"
        : type.Name;
    }
  }
}
