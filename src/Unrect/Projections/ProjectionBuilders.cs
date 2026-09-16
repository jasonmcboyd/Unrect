using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The projection vocabulary, with <typeparamref name="TSpace"/> answered once at the top of a
  /// file — and every declaration below written with no prefix and no type argument naming the
  /// space. <b>The file is the scope</b>, which is where C# already puts file-level bindings.
  /// <code>
  /// using Unrect.Projections;                                                      // the postfix half
  /// using Unrect.Spreadsheets;
  ///
  /// using static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  /// using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  ///
  /// var report = VerticalFlow(v =&gt; new Report(
  ///     Title: v.Next(Text()),
  ///     Rows:  v.Next(Table(headerRows: 1, eachRow: row))));
  /// </code>
  /// <para>
  /// The space is spelled in full in the import because a <c>using</c> directive is resolved without
  /// the other <c>using</c>s around it — the one place in the file where a namespace does not help,
  /// and the price of naming the space exactly once.
  /// </para>
  /// <para>
  /// Two closed imports in one file collide on every shared name, and that is the model rather than
  /// a limitation: for any two space types, either their difference matters to a declaration — and
  /// then no declaration serves both, so the file wants splitting — or it does not, and both
  /// declarations target the shared base one scope already covers. <b>Scope a file to what its
  /// declarations READ, not to what the file parses</b>: a workbook opened for formulas whose
  /// projections never ask for one is an <c>ISpace</c> file.
  /// </para>
  /// <para>
  /// <b>What cannot live here is the postfix half.</b> C# forbids extension methods in a generic
  /// static class, so <c>.Named</c>, <c>.Optional</c>, <c>.OrBlank</c>, <c>.Select</c>,
  /// <c>.Padded</c> and <c>.Map</c> arrive through the ordinary
  /// <c>using Unrect.Projections;</c>. A namespace import publishes no simple names, so the two
  /// cannot collide, and the seam falls where a reader would draw it anyway: what spells before the
  /// subject is imported here, what spells after it is an extension.
  /// </para>
  /// <para>
  /// This part holds the placement pipeline's entries — where a section sits, said before the
  /// section itself. Every anchor in the vocabulary is here and nowhere else, and that is the whole
  /// of the compile-time discipline: an anchor is a root, movements never lead back to one, and
  /// nothing chains past a terminal, so the contradictions a placement can state are unspellable
  /// rather than refused when they run. What follows an entry is
  /// <see cref="PlacementStage{TSpace}"/>.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space every declaration in the importing file is written over.</typeparam>
  public static partial class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
  {
    // --- Anchors: a position stated as a relation to something in the grid -------------------------

    /// <summary>
    /// Opens a pipeline whose section sits <em>on</em> the row <paramref name="landmark"/> matches: it
    /// starts at that row and owns it, so a caption is content the section reads rather than a gap it
    /// steps over. A position is a relation to a thing, never a distance arrived at. Occupancy has no
    /// direction, so the word names none — the argument's type carries the axis, and the column form is
    /// this same word. A landmark that matches nothing is loud, absorbable by <c>Optional</c> and
    /// <c>Else</c>, and read by a <c>VerticalRepeat</c> as having run out of sections.
    /// </summary>
    /// <param name="landmark">The row to sit on.</param>
    public static OffsetStage<TSpace> On(IRowLandmark landmark) => Enter(Step.OnRow(landmark));

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The column to sit on.</param>
    public static OffsetStage<TSpace> On(IColumnLandmark landmark) => Enter(Step.OnColumn(landmark));

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The row to sit on. A matcher demanding less is accepted as it is.</param>
    public static OffsetStage<TSpace> On(IRowLandmark<TSpace> landmark) => Enter(Step.OnRow(Required(landmark).Landmark));

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The column to sit on. A matcher demanding less is accepted as it is.</param>
    public static OffsetStage<TSpace> On(IColumnLandmark<TSpace> landmark) => Enter(Step.OnColumn(Required(landmark).Landmark));

    /// <summary>
    /// Opens a pipeline whose section starts on the row directly below the one <paramref
    /// name="landmark"/> matches — for a section under a caption another projection describes, or one
    /// nothing does. Exactly one row beyond the match, the matched row's own height and never a step
    /// you chose; unlike <c>On</c> the concept has a direction, so the word carries one. A missing
    /// landmark is loud, absorbable by <c>Optional</c>/<c>Else</c> and read by a repeat as its end.
    /// </summary>
    /// <param name="landmark">The row to sit below.</param>
    public static OffsetStage<TSpace> Below(IRowLandmark landmark) => Enter(Step.Below(landmark));

    /// <inheritdoc cref="Below(IRowLandmark)"/>
    /// <param name="landmark">The row to sit below. A matcher demanding less is accepted as it is.</param>
    public static OffsetStage<TSpace> Below(IRowLandmark<TSpace> landmark) => Enter(Step.Below(Required(landmark).Landmark));

    /// <summary>
    /// Opens a pipeline whose section starts on the column directly right of the one <paramref
    /// name="landmark"/> matches — the column twin of <see cref="Below(IRowLandmark)"/>, spelled
    /// distinctly because the direction is part of what is being said.
    /// </summary>
    /// <param name="landmark">The column to sit right of.</param>
    public static OffsetStage<TSpace> RightOf(IColumnLandmark landmark) => Enter(Step.RightOf(landmark));

    /// <inheritdoc cref="RightOf(IColumnLandmark)"/>
    /// <param name="landmark">The column to sit right of. A matcher demanding less is accepted as it is.</param>
    public static OffsetStage<TSpace> RightOf(IColumnLandmark<TSpace> landmark) => Enter(Step.RightOf(Required(landmark).Landmark));

    // --- The strategy door, the filler-steppers and the counted movements --------------------------

    /// <summary>
    /// Opens a pipeline whose section starts wherever <paramref name="offset"/> resolves to — an
    /// assignment, not a movement: <em>my start is where that resolves to</em>. This is the door onto
    /// the strategy calculus, the one marked crossing from the cell-model vocabulary into offsets over
    /// intervals; reach for it when no anchor says what you mean, and prefer an anchor when one does.
    /// </summary>
    /// <param name="offset">Where the section starts.</param>
    public static OffsetStage<TSpace> OffsetBy(IOffsetStrategy offset) => Enter(Step.OffsetBy(offset));

    /// <summary>
    /// Opens a pipeline whose section starts past the blank rows in front of it. One of the two
    /// operators that keep the word <em>after</em>: filler is the one thing that genuinely has an
    /// after, and neither takes an argument, so nothing reads as a distance. Tolerant by nature — no
    /// blank rows in front means no movement, not a failure.
    /// </summary>
    public static OffsetStage<TSpace> AfterBlankRows() => Enter(Step.AfterBlankRows());

    /// <inheritdoc cref="AfterBlankRows()"/>
    public static OffsetStage<TSpace> AfterBlankColumns() => Enter(Step.AfterBlankColumns());

    /// <summary>
    /// Opens a pipeline whose section starts at the first non-blank cell scanning row-major from the
    /// top-left — down to the first row that carries content, then across it to its first non-blank
    /// cell. The lazy corner heuristic: it reads a row at a time and never scans down a column, so a
    /// headered or top-left-aligned region locates its corner without measuring its height.
    /// <para>
    /// It finds the FIRST content row's first non-blank cell, which is the region's true corner only
    /// when the region is top-left-aligned; a ragged region whose lower rows reach further left is
    /// the documented, accepted miss. Tolerant by nature — an all-blank space resolves to its end,
    /// an empty section, rather than failing.
    /// </para>
    /// </summary>
    public static OffsetStage<TSpace> SkipToFirstNonBlankCell() => Enter(Step.SkipToFirstNonBlankCell());

    /// <summary>
    /// Steps over the blank rows and then the blank columns in front of the section — the one-word
    /// entry for the instinct the default deliberately refuses.
    /// <para>
    /// Silence is adjacency: a declaration that says nothing starts exactly where the one before it
    /// left off, because the blank bands a <c>separatedBy</c> counts on, and the empty region an
    /// <c>Optional</c> section needs to see, are content rather than noise. Skipping them is
    /// therefore a declared exception like any other, and this is how it is declared.
    /// </para>
    /// </summary>
    public static OffsetStage<TSpace> SkipEmptyRowsAndColumns() => AfterBlankRows().AfterBlankColumns();

    /// <summary>
    /// Opens a pipeline whose section starts <paramref name="rows"/> rows down. A movement composes onto
    /// an offset an earlier stage declared, so <c>Down(1).On(mark)</c> is refused (an anchor is a root)
    /// while <c>On(mark).Down(1)</c> reads cumulatively; a movement with nothing before it —
    /// <c>Down(2).Of(Table())</c> — replaces the shape's own default rather than composing onto it.
    /// </summary>
    /// <param name="rows">How far down.</param>
    public static OffsetStage<TSpace> Down(int rows) => Enter(Step.Down(rows));

    /// <summary>
    /// Opens a pipeline whose section starts <paramref name="columns"/> columns right — the column
    /// twin of <see cref="Down(int)"/>, composing the same way.
    /// </summary>
    /// <param name="columns">How far right.</param>
    public static OffsetStage<TSpace> Right(int columns) => Enter(Step.Right(columns));

    // --- The extent -------------------------------------------------------------------------------

    /// <summary>
    /// Opens a pipeline sized to <paramref name="area"/> and placed at adjacency — the size-only
    /// pipeline start, for a region sized to its content but not moved
    /// (<c>Sized(RowsWhileAnyValue()).Of(header)</c>). To also move it, lead with an offset entry:
    /// <c>On(mark).Sized(area)</c>, <c>Down(1).Sized(area)</c>.
    /// </summary>
    /// <param name="area">The extent.</param>
    public static OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area)
      => new OffsetAndSizeStage<TSpace>(Steps.None.Then(Step.Sized(area)));

    // --- Bounds -----------------------------------------------------------------------------------

    /// <inheritdoc cref="UnboundedStage{TSpace}.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilRow(landmark, orEnd)));

    /// <inheritdoc cref="UnboundedStage{TSpace}.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent stops before. A matcher demanding less is accepted as it is.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> Until(IRowLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilRow(Required(landmark).Landmark, orEnd)));

    /// <inheritdoc cref="UnboundedStage{TSpace}.UntilColumn(IColumnLandmark, bool)"/>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilColumn(landmark, orEnd)));

    /// <inheritdoc cref="UnboundedStage{TSpace}.UntilColumn(IColumnLandmark, bool)"/>
    /// <param name="landmark">The column the extent stops before. A matcher demanding less is accepted as it is.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> UntilColumn(IColumnLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilColumn(Required(landmark).Landmark, orEnd)));

    // --- The heading ------------------------------------------------------------------------------

    /// <inheritdoc cref="HeadingStage{TSpace}"/>
    /// <param name="text">What the heading row says.</param>
    public static HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps.None, Headings.One(text));

    private static OffsetStage<TSpace> Enter(Step step) => new OffsetStage<TSpace>(Steps.None.Then(step));

    private static TLandmark Required<TLandmark>(TLandmark landmark)
      where TLandmark : class
      => landmark ?? throw new ArgumentNullException(nameof(landmark));
  }
}
