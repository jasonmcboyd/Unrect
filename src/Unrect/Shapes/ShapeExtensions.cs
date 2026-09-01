using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// Application, naming, and placement modifiers.
  /// <para>
  /// The movement modifiers — <c>Down</c>, <c>Right</c>, <c>AfterBlankRows</c>,
  /// <c>AfterBlankColumns</c> — <em>compose</em>: each one starts from where the shape already sits,
  /// so <c>.Right(9).Down(1)</c> anchors at column 9, row 1, and <c>Table(...).Down(2)</c> means
  /// "past the blank rows, then two more".
  /// </para>
  /// <para>
  /// <c>After</c> <em>replaces</em> the offset outright — the "put it exactly here" spelling, and
  /// the way to discard a default. <c>Sized</c> replaces too, since extents do not stack.
  /// </para>
  /// </summary>
  public static partial class ShapeExtensions
  {
    /// <summary>
    /// Decomposes <paramref name="space"/> and projects it in one call. The shape's own placement
    /// is applied here too, exactly as it would be nested inside another shape.
    /// <para>
    /// Coordinates in failures are relative to <paramref name="space"/>, so a <c>Map</c> called
    /// from inside another shape's projection restarts them and reports positions relative to its
    /// own space. Compose shapes instead of nesting <c>Map</c> calls wherever you can.
    /// </para>
    /// </summary>
    public static TResult Map<TResult>(this IShape<TResult> shape, ISpace space) => shape.Apply(space).Value;

    /// <summary>
    /// <see cref="Map{TResult}"/> plus where the shape landed and how much it consumed.
    /// </summary>
    public static AppliedResult<TResult> Apply<TResult>(this IShape<TResult> shape, ISpace space)
    {
      if (shape is null)
        throw new ArgumentNullException(nameof(shape));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return ShapeEngine.Apply(shape, space, ShapeContext.Root(space));
    }

    /// <summary>
    /// <see cref="Map{TResult}"/>, keeping what the decomposition noticed: every tolerance boundary
    /// that absorbed a failure, every alternative a choice passed over, and space the shape did not
    /// describe. A failure nothing declared tolerance for still throws — declared tolerance is the
    /// only thing that ever softens a parse.
    /// <para>
    /// Space nothing described is reported as an <c>Info</c>, except where the entire parse was one
    /// absorbed failure: <c>shape.Optional().MapWithDiagnostics(space)</c> — tolerance declared at
    /// the root, the nearest thing to a lenient mode — would otherwise say "consumed 0 of N rows"
    /// underneath a warning that already named the shape, the reason, and the cell. Anything else
    /// still reports, including a root that consumed nothing after absorbing in two places, or a
    /// repeat that found no sections at all.
    /// </para>
    /// <para>
    /// Diagnostics belong to one call: a <c>Map</c> nested inside a projection collects its own and
    /// discards them, so tolerance declared in there is invisible out here. Another reason to
    /// compose shapes rather than nest calls.
    /// </para>
    /// </summary>
    public static MapResult<TResult> MapWithDiagnostics<TResult>(this IShape<TResult> shape, ISpace space)
    {
      if (shape is null)
        throw new ArgumentNullException(nameof(shape));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var context = ShapeContext.Root(space);
      var mark = context.Diagnostics.Mark();
      var applied = ShapeEngine.Apply(shape, space, context);

      // Suppressed only when the whole parse is one absorbed failure: two boundaries that each
      // absorbed something have left a gap worth mentioning, even though neither consumed anything.
      if (!(applied.Advance.Width == 0 && applied.Advance.Height == 0 && context.Diagnostics.AbsorbedAt(mark)))
        ReportUnconsumed(shape, space, applied.Offset.Size, applied.Consumed, context);

      return new MapResult<TResult>(applied.Value, context.Diagnostics.Snapshot());
    }

    /// <summary>
    /// Space left over is the shape drifting from the file — the cells nobody described are exactly
    /// where the next surprise lives. A leading offset is a gap as much as a trailing remainder is:
    /// a shape that starts two rows down described neither those two rows nor whatever follows it.
    /// </summary>
    private static void ReportUnconsumed(IShape shape, ISpace space, Size gap, Size described, ShapeContext context)
    {
      var size = space.Area.Size;

      if (described.Width >= size.Width && described.Height >= size.Height)
        return;

      var counts = new List<string>(2);
      var undescribed = new List<string>(2);

      Describe(gap.Height, described.Height, size.Height, "row", counts, undescribed);
      Describe(gap.Width, described.Width, size.Width, "column", counts, undescribed);


      // The earliest cell nothing described, in reading order: a leading gap on either axis starts
      // at the very first cell, otherwise it is wherever the described region stops.
      var first =
        gap.Width > 0 || gap.Height > 0 ? default
        : described.Width < size.Width ? new Offset(described.Width, 0)
        : new Offset(0, described.Height);

      context.Advance(first).Report(
        DiagnosticSeverity.Info,
        shape,
        $"the shape consumed {string.Join(" and ", counts)}; {string.Join(" and ", undescribed)} were not described",
        space);
    }

    /// <summary>
    /// Adds one axis's worth of what was read and what was skipped, before it and after it — in
    /// 1-based terms, because the reader is looking at a spreadsheet.
    /// </summary>
    private static void Describe(int gap, int described, int total, string axis, List<string> counts, List<string> undescribed)
    {
      if (described >= total)
        return;

      counts.Add($"{described} of {total} {axis}s");

      var ranges = new List<string>(2);

      if (gap > 0)
        ranges.Add(gap == 1 ? "1" : $"1-{gap}");

      var after = gap + described;

      if (after < total)
        ranges.Add($"{after + 1}+");

      undescribed.Add($"{axis}s {string.Join(" and ", ranges)}");
    }

    /// <summary>Labels the shape, so failures and diagnostics say <paramref name="name"/>.</summary>
    public static IShape<T> Named<T>(this IShape<T> shape, string name) => NotNull(shape).WithName(name);

    /// <summary>
    /// Positions the shape at <paramref name="offset"/>, <em>replacing</em> any offset it had —
    /// including a default, which is how a <c>Table</c> is told not to skip its blank rows.
    /// </summary>
    public static IShape<T> After<T>(this IShape<T> shape, IOffsetStrategy offset)
      => NotNull(shape).WithPlacement(shape.Placement.WithOffset(offset));

    /// <summary>Moves the shape on past the blank rows in front of it.</summary>
    public static IShape<T> AfterBlankRows<T>(this IShape<T> shape) => Move(shape, OffsetStrategies.SkipBlankRows());

    /// <summary>Moves the shape on past the blank columns in front of it.</summary>
    public static IShape<T> AfterBlankColumns<T>(this IShape<T> shape) => Move(shape, OffsetStrategies.SkipBlankColumns());

    /// <summary>Moves the shape on <paramref name="rows"/> rows down from where it sits.</summary>
    public static IShape<T> Down<T>(this IShape<T> shape, int rows)
      => Move(shape, OffsetStrategies.ExplicitOffset(0, NotNegative(rows, nameof(rows))));

    /// <summary>
    /// Moves the shape on <paramref name="columns"/> columns right from where it sits.
    /// </summary>
    public static IShape<T> Right<T>(this IShape<T> shape, int columns)
      => Move(shape, OffsetStrategies.ExplicitOffset(NotNegative(columns, nameof(columns)), 0));

    /// <summary>
    /// Declares the shape's extent, replacing whatever it had — including a derived one, after
    /// which the extent is consumed in full whether the projection reads all of it or not.
    /// </summary>
    public static IShape<T> Sized<T>(this IShape<T> shape, IAreaStrategy area)
      => NotNull(shape).WithPlacement(shape.Placement.WithArea(area));

    /// <summary>
    /// Falls back to <paramref name="fallback"/> when this shape fails, recording a
    /// <c>Warning</c> that carries the failing shape's own path, location, and problem.
    /// <para>
    /// Tolerance is declared where it is acceptable, and nowhere else: everything under this shape
    /// still fails exactly as loudly, and the failure travels up to the nearest boundary.
    /// </para>
    /// <para>
    /// A boundary's own placement is resolved before it can catch anything, so where the offset
    /// sits decides what is tolerated: <c>x.After(seek).Else(y)</c> survives a missing anchor,
    /// while <c>x.Else(y).After(seek)</c> does not — which is exactly what a <c>Repeat</c> wants,
    /// since running out of anchors is how it knows to stop.
    /// </para>
    /// <para>
    /// What a boundary absorbs is a failure about the shape of the data. A projection that broke
    /// rather than disagreed — a null reference, an index past the end of your own array — is a bug
    /// in the reading code and passes straight through, location and all. If the fallback fails as
    /// well, that failure is what you get, carrying a note about the shape it stood in for.
    /// </para>
    /// </summary>
    public static IShape<T> Else<T>(this IShape<T> shape, IShape<T> fallback)
    {
      if (fallback is null)
        throw new ArgumentNullException(nameof(fallback));

      return new BoundaryShape<T>(NotNull(shape), fallback, default!, Placement.Default, "Else");
    }

    /// <summary>
    /// Yields <paramref name="fallbackValue"/> when this shape fails, recording a <c>Warning</c>
    /// that carries the failing shape's own path, location, and problem.
    /// <para>
    /// An absorbed shape consumes nothing beyond its own declared placement — nothing was read, so
    /// no honest extent exists, and a following sibling in a stack starts where this shape began
    /// rather than after it. Pair absorbing boundaries with seek-anchored siblings so what comes
    /// next finds itself by content instead of by arithmetic.
    /// </para>
    /// <para>
    /// That makes <c>Repeat(x.Optional())</c> a trap: an absorbed item advances the repetition by
    /// nothing, which ends it. A repeat recovers by consuming the malformed section instead — see
    /// the recipe on <c>Repeat</c> — and only a fallback that reads rows can do that.
    /// </para>
    /// <para>
    /// Tolerance absorbs failures about the shape of the data, never bugs in the code reading it: a
    /// projection that threw a null reference or ran off the end of its own array comes through
    /// undiminished.
    /// </para>
    /// </summary>
    public static IShape<T> Else<T>(this IShape<T> shape, T fallbackValue)
      => new BoundaryShape<T>(NotNull(shape), null, fallbackValue, Placement.Default, "Else");

    /// <summary>
    /// Yields the default value when this shape fails, recording a <c>Warning</c> that carries the
    /// failing shape's own path, location, and problem — the spelling for a section that may simply
    /// not be there.
    /// <para>
    /// Like <see cref="Else{T}(IShape{T}, T)"/>, an absorbed shape consumes nothing. For a value
    /// type the filler is <c>default</c> — <c>0</c>, not null — so where the difference between
    /// "absent" and "zero" matters, either give the filler explicitly with <c>Else(value)</c> or
    /// project to a nullable first.
    /// </para>
    /// </summary>
    public static IShape<T?> Optional<T>(this IShape<T> shape)
      => new BoundaryShape<T?>(NotNull(shape).Select(value => (T?)value), null, default, Placement.Default, "Optional");

    /// <summary>
    /// Insets the shape's extent by <paramref name="all"/> cells on every side.
    /// </summary>
    public static IShape<T> Padded<T>(this IShape<T> shape, int all)
    {
      NotNegative(all, nameof(all));

      return Pad(shape, all, all, all, all);
    }

    /// <summary>
    /// Insets the shape's extent by <paramref name="horizontal"/> cells left and right and
    /// <paramref name="vertical"/> cells top and bottom.
    /// </summary>
    public static IShape<T> Padded<T>(this IShape<T> shape, int horizontal, int vertical)
    {
      NotNegative(horizontal, nameof(horizontal));
      NotNegative(vertical, nameof(vertical));

      return Pad(shape, horizontal, vertical, horizontal, vertical);
    }

    /// <summary>
    /// Insets the shape's extent by the given amounts, so the shape reads the inside of its region
    /// and still consumes the whole of it — a border of labels around a block of numbers, say.
    /// <para>
    /// Padding shrinks the inside, where an offset shifts the outside; that is the difference
    /// between this and the movement modifiers, and the two compose freely.
    /// </para>
    /// </summary>
    public static IShape<T> Padded<T>(this IShape<T> shape, int left, int top, int right, int bottom)
    {
      NotNegative(left, nameof(left));
      NotNegative(top, nameof(top));
      NotNegative(right, nameof(right));
      NotNegative(bottom, nameof(bottom));

      return Pad(shape, left, top, right, bottom);
    }

    /// <summary>
    /// Projects the shape's result through <paramref name="selector"/>. The wrapper is a shape like
    /// any other, so <c>Named</c> and the placement modifiers work on either side of it.
    /// </summary>
    public static IShape<TResult> Select<T, TResult>(this IShape<T> shape, Func<T, TResult> selector)
      => new MapShape<T, TResult>(NotNull(shape), selector, Placement.Default);

    /// <summary>
    /// Carries the shape on from wherever it already sits, so movements read cumulatively. A shape
    /// that has not been placed yet has nothing to carry on from and simply takes the new offset.
    /// </summary>
    private static IShape<T> Move<T>(IShape<T> shape, IOffsetStrategy offset)
    {
      var placement = NotNull(shape).Placement;

      return shape.WithPlacement(placement.WithOffset(
        placement.HasDeclaredOffset ? OffsetStrategies.Then(placement.Offset, offset) : offset));
    }

    private static IShape<T> Pad<T>(IShape<T> shape, int left, int top, int right, int bottom)
      => new PadShape<T>(NotNull(shape), left, top, right, bottom, Placement.Default);

    private static IShape<T> NotNull<T>(IShape<T> shape) => shape ?? throw new ArgumentNullException(nameof(shape));

    private static int NotNegative(int distance, string parameter)
      => distance >= 0 ? distance : throw new ArgumentOutOfRangeException(parameter, distance, "A shape cannot be inset or moved a negative distance.");
  }
}
