using System;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// Application, naming, and placement modifiers.
  /// <para>
  /// <b>Silence is adjacency.</b> A shape with no placement modifier starts exactly where the one
  /// before it left off, so every modifier here is a declared exception — and each word names the
  /// <em>kind</em> of reason it is an exception for. <c>On</c>, <c>Below</c> and <c>RightOf</c>
  /// state a relation to something in the grid; <c>AfterBlankRows</c> and <c>AfterBlankColumns</c>
  /// step over filler; <c>Down</c> and <c>Right</c> count cells; <c>OffsetBy</c> hands the decision
  /// to the strategy calculus. Reading a declaration, you never have to ask why a shape moved.
  /// </para>
  /// <para>
  /// The movement modifiers — <c>Down</c>, <c>Right</c>, <c>AfterBlankRows</c>,
  /// <c>AfterBlankColumns</c> — <em>compose</em>: each one starts from where the shape already sits,
  /// so <c>.Right(9).Down(1)</c> anchors at column 9, row 1, and <c>Table(...).Down(2)</c> means
  /// "past the blank rows, then two more".
  /// </para>
  /// <para>
  /// The anchors — <c>On</c>, <c>Below</c>, <c>RightOf</c> — and <c>OffsetBy</c> <em>replace</em>
  /// the offset outright: a position stated as a relation to a thing owes nothing to wherever the
  /// cursor had got to, and replacing is also how a default is discarded. <c>Sized</c> replaces
  /// too, since extents do not stack.
  /// </para>
  /// <para>
  /// <b>A modifier keeps the demand it is applied to.</b> Each one is generic in the <em>shape's own
  /// type</em> and hands that type back, so <c>Text().Named("t")</c> is an <c>IShape&lt;string&gt;</c>
  /// exactly as it always was, and the same modifier on a formula-reading declaration hands back a
  /// formula-reading declaration. That is why there is one of each here rather than one per demand.
  /// Raising a demand is a different act with its own overloads — see
  /// <c>ShapeExtensions.Typed</c> — because only a matcher, not a modifier, can add one.
  /// </para>
  /// </summary>
  public static partial class ShapeExtensions
  {
    /// <summary>
    /// Decomposes <paramref name="space"/> and projects it in one call. The shape's own placement
    /// is applied here too, exactly as it would be nested inside another shape.
    /// <para>
    /// <paramref name="space"/> must satisfy whatever the shape demands, which for a declaration
    /// that names no capability is any <see cref="ISpace"/> at all. A declaration that reads
    /// formulas will not compile against a grid that has none.
    /// </para>
    /// <para>
    /// Coordinates in failures are relative to <paramref name="space"/>, so a <c>Map</c> called
    /// from inside another shape's projection restarts them and reports positions relative to its
    /// own space. Compose shapes instead of nesting <c>Map</c> calls wherever you can.
    /// </para>
    /// <para>
    /// The root of a path renders by description rather than by a name, deliberately: inferring one
    /// from the receiver would need an optional compiler-supplied parameter, and that would stop
    /// <c>Map</c> being usable as a method group — <c>spaces.Select(report.Map)</c>, one shape over
    /// many workbooks, is the reason this library exists. Name the root with <c>Named</c> if a path
    /// should carry it.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">What the shape demands of the space.</typeparam>
    /// <typeparam name="TResult">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static TResult Map<TSpace, TResult>(this IShape<TSpace, TResult> shape, TSpace space)
      where TSpace : class, ISpace
      => shape.Apply(space).Value;

    /// <summary>
    /// <see cref="Map{TSpace, TResult}"/> plus where the shape landed and how much it consumed.
    /// </summary>
    /// <typeparam name="TSpace">What the shape demands of the space.</typeparam>
    /// <typeparam name="TResult">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static AppliedResult<TResult> Apply<TSpace, TResult>(this IShape<TSpace, TResult> shape, TSpace space)
      where TSpace : class, ISpace
    {
      if (shape is null)
        throw new ArgumentNullException(nameof(shape));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return ShapeEngine.Apply(Plain(shape), space, ShapeContext.Root(space));
    }

    /// <summary>
    /// <see cref="Map{TSpace, TResult}"/>, keeping what the decomposition noticed: every tolerance
    /// boundary that absorbed a failure, every alternative a choice passed over, and space the shape
    /// did not describe. A failure nothing declared tolerance for still throws — declared tolerance
    /// is the only thing that ever softens a parse.
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
    /// <typeparam name="TSpace">What the shape demands of the space.</typeparam>
    /// <typeparam name="TResult">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static MapResult<TResult> MapWithDiagnostics<TSpace, TResult>(this IShape<TSpace, TResult> shape, TSpace space)
      where TSpace : class, ISpace
    {
      if (shape is null)
        throw new ArgumentNullException(nameof(shape));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var plain = Plain(shape);
      var context = ShapeContext.Root(space);
      var mark = context.Diagnostics.Mark();
      var applied = ShapeEngine.Apply(plain, space, context);

      // Suppressed only when the whole parse is one absorbed failure: two boundaries that each
      // absorbed something have left a gap worth mentioning, even though neither consumed anything.
      if (!(applied.Advance.Width == 0 && applied.Advance.Height == 0 && context.Diagnostics.AbsorbedAt(mark)))
        ReportUnconsumed(plain, space, applied.Offset.Size, applied.Consumed, context);

      return new MapResult<TResult>(applied.Value, context.Diagnostics.Snapshot());
    }

    /// <summary>
    /// Labels the shape, so failures and diagnostics say <paramref name="name"/>.
    /// <para>
    /// A name given here beats the one a layout infers from the use site, so a shape-returning
    /// helper must not name what it returns: every call would produce a shape called the same
    /// thing, wherever it appeared, and the helper is the one place that cannot know which of them
    /// this is. Let the use site name it — <c>var captions = FullRow(); … v.Next(captions)</c> reads
    /// as <c>'captions'</c> — and keep <c>Named</c> for the two jobs inference cannot do: shapes
    /// written inline, which have no identifier to borrow, and overriding an inferred name that
    /// reads badly.
    /// </para>
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="name">What failures and diagnostics should call it.</param>
    public static TShape Named<TShape>(this TShape shape, string name)
      where TShape : class, IShape
      => Cloned<TShape>(Base(shape).Renamed(name ?? throw new ArgumentNullException(nameof(name))));

    /// <summary>
    /// Puts the shape <em>on</em> the row <paramref name="landmark"/> matches: it starts at that row
    /// and owns it, so a caption is content the shape reads rather than a gap it steps over.
    /// <para>
    /// A position is a relation to a thing, never a distance arrived at — <c>On</c> says which row,
    /// and the arithmetic of reaching it is not the declaration's business. Occupancy has no
    /// direction, so the word names none; the argument's type carries the axis, and the column form
    /// is this same word.
    /// </para>
    /// <para>
    /// Anchoring <em>replaces</em> the shape's offset, including a default. A landmark that matches
    /// nothing is loud, because it means the section the declaration describes is not the section in
    /// the file. That is a disagreement about the data rather than a broken projection, so
    /// <c>Optional</c> and <c>Else</c> absorb it — and a <c>VerticalRepeat</c> reads it as having run
    /// out of sections, which is how a repetition knows to stop.
    /// </para>
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row to sit on.</param>
    public static TShape On<TShape>(this TShape shape, IRowLandmark landmark)
      where TShape : class, IShape
      => shape.OffsetBy(OffsetStrategies.To(landmark));

    /// <inheritdoc cref="On{TShape}(TShape, IRowLandmark)"/>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column to sit on.</param>
    public static TShape On<TShape>(this TShape shape, IColumnLandmark landmark)
      where TShape : class, IShape
      => shape.OffsetBy(OffsetStrategies.To(landmark));

    /// <summary>
    /// Starts the shape on the row directly below the one <paramref name="landmark"/> matches — for
    /// a section that sits under a caption some other shape describes, or under one nothing does.
    /// <para>
    /// Exactly one row beyond the match, which is the matched row's own height and never a step you
    /// chose. Like <c>On</c>, it states a relation and <em>replaces</em> the offset; unlike
    /// <c>On</c>, the concept genuinely has a direction, so the word carries one — and it is
    /// grid-absolute, meaning down the sheet, not "the next band along whichever way this flow
    /// happens to run".
    /// </para>
    /// <para>
    /// A landmark that matches nothing is loud, absorbable by <c>Optional</c> and <c>Else</c>, and
    /// read by a repetition as the end of its sections — the same absence semantics as <c>On</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row to sit below.</param>
    public static TShape Below<TShape>(this TShape shape, IRowLandmark landmark)
      where TShape : class, IShape
      => shape.OffsetBy(OffsetStrategies.Past(landmark));

    /// <summary>
    /// Starts the shape on the column directly right of the one <paramref name="landmark"/> matches
    /// — the column twin of <see cref="Below{TShape}"/>, spelled distinctly because the direction is
    /// part of what is being said.
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column to sit right of.</param>
    public static TShape RightOf<TShape>(this TShape shape, IColumnLandmark landmark)
      where TShape : class, IShape
      => shape.OffsetBy(OffsetStrategies.Past(landmark));

    /// <summary>
    /// Starts the shape wherever <paramref name="offset"/> ends — an assignment rather than a
    /// movement: <em>my start is where that resolves to</em>. So it <em>replaces</em> any offset the
    /// shape had, including a default, which is how a <c>Table</c> is told not to skip its blank
    /// rows.
    /// <para>
    /// This is the door onto the strategy calculus, and the one marked crossing between the two
    /// models the library otherwise keeps apart: the vocabulary around it speaks of rows, columns
    /// and cells the way a sheet is read, while a strategy computes offsets and sizes over
    /// intervals. Reach for it when no anchor says what you mean —
    /// <c>.OffsetBy(Then(BlankRows(), SkipRows(1)))</c>, or a lift the shape layer does not
    /// re-export — and prefer the anchors when one does.
    /// </para>
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="offset">Where the shape starts.</param>
    public static TShape OffsetBy<TShape>(this TShape shape, IOffsetStrategy offset)
      where TShape : class, IShape
      => Cloned<TShape>(Base(shape).Replaced(shape.Placement.WithOffset(offset)));

    /// <summary>
    /// Moves the shape on past the blank rows in front of it.
    /// <para>
    /// One of the two operators that keep the word <em>after</em>, and they earn it: filler is the
    /// one thing that genuinely has an after, and neither takes a positional argument, so there is
    /// no relation for a reader to mis-read as a distance. Tolerant by nature — no blank rows in
    /// front means no movement, not a failure.
    /// </para>
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    public static TShape AfterBlankRows<TShape>(this TShape shape)
      where TShape : class, IShape
      => Move(shape, OffsetStrategies.SkipBlankRows());

    /// <summary>
    /// Moves the shape on past the blank columns in front of it; see
    /// <see cref="AfterBlankRows{TShape}"/> for why these two keep the word "after".
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    public static TShape AfterBlankColumns<TShape>(this TShape shape)
      where TShape : class, IShape
      => Move(shape, OffsetStrategies.SkipBlankColumns());

    /// <summary>Moves the shape on <paramref name="rows"/> rows down from where it sits.</summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="rows">How far down.</param>
    public static TShape Down<TShape>(this TShape shape, int rows)
      where TShape : class, IShape
      => Move(shape, OffsetStrategies.ExplicitOffset(0, NotNegative(rows, nameof(rows))));

    /// <summary>
    /// Moves the shape on <paramref name="columns"/> columns right from where it sits.
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="columns">How far right.</param>
    public static TShape Right<TShape>(this TShape shape, int columns)
      where TShape : class, IShape
      => Move(shape, OffsetStrategies.ExplicitOffset(NotNegative(columns, nameof(columns)), 0));

    /// <summary>
    /// Declares the shape's extent, replacing whatever it had — including a derived one, after
    /// which the extent is consumed in full whether the projection reads all of it or not.
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="area">The extent.</param>
    public static TShape Sized<TShape>(this TShape shape, IAreaStrategy area)
      where TShape : class, IShape
      => Cloned<TShape>(Base(shape).Replaced(shape.Placement.WithArea(area)));

    /// <summary>
    /// Falls back to <paramref name="fallback"/> when this shape fails, recording a
    /// <c>Warning</c> that carries the failing shape's own path, location, and problem.
    /// <para>
    /// Tolerance is declared where it is acceptable, and nowhere else: everything under this shape
    /// still fails exactly as loudly, and the failure travels up to the nearest boundary.
    /// </para>
    /// <para>
    /// A boundary's own placement is resolved before it can catch anything, so where the offset
    /// sits decides what is tolerated: <c>x.On(anchor).Else(y)</c> survives a missing anchor,
    /// while <c>x.Else(y).On(anchor)</c> does not — which is exactly what a <c>VerticalRepeat</c>
    /// wants, since running out of anchors is how it knows to stop.
    /// </para>
    /// <para>
    /// What a boundary absorbs is a failure about the shape of the data. A projection that broke
    /// rather than disagreed — a null reference, an index past the end of your own array — is a bug
    /// in the reading code and passes straight through, location and all. If the fallback fails as
    /// well, that failure is what you get, carrying a note about the shape it stood in for.
    /// </para>
    /// <para>
    /// The two shapes must read the same thing, and the compiler settles the pair's demand between
    /// them: a plain shape with a formula-reading fallback is a formula-reading declaration, since
    /// either of them may be the one that runs.
    /// </para>
    /// </summary>
    /// <typeparam name="TShape">The type both shapes share, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="fallback">What to read instead.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="fallback"/> argument.</param>
    public static TShape Else<TShape>(
      this TShape shape,
      TShape fallback,
      [CallerArgumentExpression("fallback")] string? declared = null)
      where TShape : class, IShape
    {
      if (fallback is null)
        throw new ArgumentNullException(nameof(fallback));

      return Wrapped<TShape>(Base(shape).Otherwise(fallback, declared));
    }

    /// <summary>
    /// Yields <paramref name="fallbackValue"/> when this shape fails, recording a <c>Warning</c>
    /// that carries the failing shape's own path, location, and problem.
    /// <para>
    /// An absorbed shape consumes nothing beyond its own declared placement — nothing was read, so
    /// no honest extent exists, and a following sibling in a flow starts where this shape began
    /// rather than after it. Pair absorbing boundaries with content-anchored siblings so what comes
    /// next finds itself by content instead of by arithmetic.
    /// </para>
    /// <para>
    /// That makes <c>VerticalRepeat(x.Optional())</c> a trap: an absorbed item advances the
    /// repetition by nothing, which ends it. A repeat recovers by consuming the malformed section
    /// instead — see the recipe on <c>VerticalRepeat</c> — and only a fallback that reads rows can
    /// do that.
    /// </para>
    /// <para>
    /// Tolerance absorbs failures about the shape of the data, never bugs in the code reading it: a
    /// projection that threw a null reference or ran off the end of its own array comes through
    /// undiminished.
    /// </para>
    /// <para>
    /// A filler value is not a declaration, so this is one of the few modifiers whose result type
    /// differs from its receiver's; see <c>ShapeExtensions.Typed</c> for the demanding form.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="fallbackValue">What to yield instead.</param>
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
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    public static IShape<T?> Optional<T>(this IShape<T> shape)
      => new BoundaryShape<T?>(NotNull(shape).Select(value => (T?)value), null, default, Placement.Default, "Optional");

    /// <summary>
    /// Projects the shape's result through <paramref name="selector"/>. The wrapper is a shape like
    /// any other, so <c>Named</c> and the placement modifiers work on either side of it.
    /// </summary>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <typeparam name="TResult">What the selector produces.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="selector">The projection.</param>
    public static IShape<TResult> Select<T, TResult>(this IShape<T> shape, Func<T, TResult> selector)
      => new MapShape<T, TResult>(NotNull(shape), selector, Placement.Default);

    /// <summary>
    /// Insets the shape's extent by <paramref name="all"/> cells on every side.
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="all">The inset on every side.</param>
    public static TShape Padded<TShape>(this TShape shape, int all)
      where TShape : class, IShape
    {
      NotNegative(all, nameof(all));

      return Pad(shape, all, all, all, all);
    }

    /// <summary>
    /// Insets the shape's extent by <paramref name="horizontal"/> cells left and right and
    /// <paramref name="vertical"/> cells top and bottom.
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="horizontal">The inset left and right.</param>
    /// <param name="vertical">The inset top and bottom.</param>
    public static TShape Padded<TShape>(this TShape shape, int horizontal, int vertical)
      where TShape : class, IShape
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
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="left">The inset on the left.</param>
    /// <param name="top">The inset on the top.</param>
    /// <param name="right">The inset on the right.</param>
    /// <param name="bottom">The inset on the bottom.</param>
    public static TShape Padded<TShape>(this TShape shape, int left, int top, int right, int bottom)
      where TShape : class, IShape
    {
      NotNegative(left, nameof(left));
      NotNegative(top, nameof(top));
      NotNegative(right, nameof(right));
      NotNegative(bottom, nameof(bottom));

      return Pad(shape, left, top, right, bottom);
    }

    /// <summary>
    /// Ends the shape's extent just before the first row that is <paramref name="landmark"/>, which
    /// the shape therefore never reads. Where <c>On</c> and <c>Below</c> say where a shape starts by
    /// content, this says where it ends by content — a section that runs until the next caption:
    /// <c>VerticalRepeat(block, separatedBy: BlankRows()).Until(RowContaining("Cash flows by inception date"))</c>.
    /// <para>
    /// The bound is consumed in full, whether or not the shape read all of it, so whatever follows
    /// starts <em>at</em> the landmark row and can anchor on it at distance zero. That is the point
    /// of bounding here rather than asking the inner shape to stop.
    /// </para>
    /// <para>
    /// A missing landmark is loud by default, as a missed anchor is: it means the section the
    /// declaration describes is not the section in the file. It is a disagreement about the data
    /// rather than a broken projection, so <c>Optional</c> and <c>Else</c> absorb it.
    /// <paramref name="orEnd"/> opts one shape into "until this, or the end of the space", recording
    /// an <c>Info</c> when it does run to the end so a reader can still tell which section was
    /// open-ended.
    /// </para>
    /// <para>
    /// Applied straight to an already-bounded shape, a second <c>Until</c> replaces the first rather
    /// than nesting — a shape has one end. Through a wrapper it does not: <c>x.Until(A).Select(f)</c>
    /// then <c>.Until(B)</c> bounds the <c>Select</c>, so B applies outside A and both are in force.
    /// (Unlike <c>Sized</c>, which replaces whatever it is applied to, because a placement belongs to
    /// one shape and a bound is a wrapper around one.)
    /// </para>
    /// <para>
    /// It belongs on the section, not on the thing repeated inside it. A bound applies its inner
    /// shape strictly, as <c>Padded</c> always has, so wrapping a repeat's <em>item</em> turns that
    /// item's own missing anchor into a hard failure instead of the graceful stop a repeat relies on
    /// to know it has run out of sections. Write <c>VerticalRepeat(item, …).Until(landmark)</c>, not
    /// <c>VerticalRepeat(item.Until(landmark), …)</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static TShape Until<TShape>(this TShape shape, IRowLandmark landmark, bool orEnd = false)
      where TShape : class, IShape
      => Bound(shape, Landmark.Of(NotNull(landmark, nameof(landmark))), orEnd);

    /// <summary>
    /// Ends the shape's extent just before the first column that is <paramref name="landmark"/> —
    /// the column twin of <see cref="Until{TShape}(TShape, IRowLandmark, bool)"/>, spelled distinctly
    /// so the common row form never has to be disambiguated by the reader.
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static TShape UntilColumn<TShape>(this TShape shape, IColumnLandmark landmark, bool orEnd = false)
      where TShape : class, IShape
      => Bound(shape, Landmark.Of(NotNull(landmark, nameof(landmark))), orEnd);

    /// <summary>
    /// Puts this shape under <paramref name="captions"/> — the rows that announce it — so the
    /// section starts below them and the caption rows are described rather than swallowed:
    /// <c>lines.Under(Caption("K-1 Lines 1-21"))</c>.
    /// <para>
    /// It is sugar for a vertical flow and nothing else: <c>x.Under(a, b)</c> is
    /// <c>VerticalFlow(v =&gt; { v.Next(a); v.Next(b); return v.Next(x); })</c>. So every caption is
    /// a real child with its own path segment, each one seeks from where the last left off (a
    /// stacked pair reads adjacent rows, and a gap is absorbed), the result is this shape's value
    /// with the caption values discarded, and <c>Named</c>, <c>On</c>, <c>Until</c>,
    /// <c>Optional</c> and the rest behave as they do on any flow. A missing caption under
    /// <c>Optional</c> is an absent section, which is usually what you want.
    /// </para>
    /// <para>
    /// <b>Inside a <c>VerticalRepeat</c>, anchor the item as well.</b> A repeat stops when the
    /// item's own <em>placement</em> fails, and this puts the anchor inside the flow, whose placement
    /// always fits — so the iteration past the last section fails loudly instead of stopping. Hoist
    /// the matcher and put it on the item too; anchoring is idempotent, so the caption inside then
    /// finds its row at distance zero:
    /// <code>
    /// var detail  = RowContaining("Detail");
    /// var section = lines.Under(Caption("Detail")).On(detail);
    ///
    /// VerticalRepeat(section, separatedBy: BlankRows())   // stops at the first row that is not a section
    /// </code>
    /// </para>
    /// <para>
    /// <paramref name="captions"/> is typed as any string-valued shape rather than a caption type,
    /// so a row shape whose value you want discarded may sit there too — but <c>Caption</c> is what
    /// belongs there. A caption demands nothing of its space, which is the whole of what a caption
    /// is; a section that sits under one it cannot read has a matcher problem, not a caption one.
    /// </para>
    /// </summary>
    /// <typeparam name="TShape">The shape's own type, which the modifier hands back.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="captions">The rows that announce it.</param>
    public static TShape Under<TShape>(this TShape shape, params IShape<string>[] captions)
      where TShape : class, IShape
    {
      if (captions is null)
        throw new ArgumentNullException(nameof(captions));
      if (captions.Length == 0)
        throw new ArgumentException("A shape must sit under at least one caption.", nameof(captions));

      for (var index = 0; index < captions.Length; index++)
        if (captions[index] is null)
          throw new ArgumentException($"Caption {index + 1} is null.", nameof(captions));

      // Copied because params may hand us the caller's own array, and the flow below is captured for
      // every future application of this shape. A shape that could change is not a declaration.
      return Wrapped<TShape>(Base(shape).Beneath((IShape<string>[])captions.Clone()));
    }

    /// <summary>
    /// The shape as the engine sees it. Sound because every <see cref="IShape{TSpace, TResult}"/>
    /// this library produces is a <see cref="ShapeBase{TResult}"/>, which implements
    /// <see cref="IShape{TResult}"/>; the demand lives only in the static type, so forgetting it
    /// here is the identity.
    /// </summary>
    internal static IShape<T> Plain<TSpace, T>(IShape<TSpace, T> shape)
      where TSpace : class, ISpace
      => shape as IShape<T> ?? throw NotOurs(shape, nameof(shape));

    /// <summary>
    /// Carries the shape on from wherever it already sits, so movements read cumulatively. A shape
    /// that has not been placed yet has nothing to carry on from and simply takes the new offset.
    /// </summary>
    private static TShape Move<TShape>(TShape shape, IOffsetStrategy offset)
      where TShape : class, IShape
    {
      var placement = NotNull(shape).Placement;

      return shape.OffsetBy(placement.HasDeclaredOffset
        ? OffsetStrategies.Then(placement.Offset, offset)
        : offset);
    }

    private static TShape Pad<TShape>(TShape shape, int left, int top, int right, int bottom)
      where TShape : class, IShape
      => Wrapped<TShape>(Base(shape).Inset(left, top, right, bottom));

    private static TShape Bound<TShape>(TShape shape, Landmark landmark, bool orEnd)
      where TShape : class, IShape
      => Wrapped<TShape>(Base(shape).BoundedBy(landmark, orEnd));

    /// <summary>The shape as this library builds them, which is the only kind a modifier can modify.</summary>
    private static ShapeBase Base<TShape>(TShape shape)
      where TShape : class, IShape
      => NotNull(shape) as ShapeBase ?? throw NotOurs(shape, nameof(shape));

    /// <summary>
    /// A clone back as the receiver's own type. Total: a clone has the receiver's runtime type, so
    /// it is whatever the receiver was seen as.
    /// </summary>
    private static TShape Cloned<TShape>(IShape clone)
      where TShape : class, IShape
      => (TShape)clone;

    /// <summary>
    /// A wrapper back as the receiver's own type. A wrapper is a new shape reading the same thing,
    /// so it satisfies every <em>interface</em> the receiver was seen through — but it is not the
    /// receiver's class, which only a caller holding a shape by its own concrete type would ask for.
    /// </summary>
    private static TShape Wrapped<TShape>(IShape wrapper)
      where TShape : class, IShape
      => wrapper as TShape
        ?? throw new InvalidOperationException(
          $"A modifier that wraps hands back a {wrapper.GetType().Name}, which is not a {typeof(TShape).Name}. "
          + "Hold the shape as IShape<T> — or, where it demands a capability, as IShape<TSpace, T> — rather than as its own class.");

    private static ArgumentException NotOurs(IShape? shape, string parameter)
      => new ArgumentException(
        $"{shape?.GetType().Name ?? "null"} is not a shape this library built; only those can be modified or applied.",
        parameter);

    /// <summary>One guard: the receiver defaults to its own parameter name, anything else names itself.</summary>
    private static T NotNull<T>(T value, string parameter = "shape") where T : class
      => value ?? throw new ArgumentNullException(parameter);

    private static int NotNegative(int distance, string parameter)
      => distance >= 0 ? distance : throw new ArgumentOutOfRangeException(parameter, distance, "A shape cannot be inset or moved a negative distance.");
  }
}
