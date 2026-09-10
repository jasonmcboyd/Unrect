using System;
using System.Runtime.CompilerServices;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Application, naming, tolerance, and the one surviving extent modifier.
  /// <para>
  /// <b>Geometry is the pipeline's, and the pipeline's alone.</b> Where a section starts, how big it
  /// is, and where it ends are declared with the placement pipeline — the entries on
  /// <see cref="Projection"/> (<c>On</c>, <c>Below</c>, <c>RightOf</c>, <c>OffsetBy</c>, <c>Down</c>,
  /// <c>Right</c>, <c>AfterBlankRows</c>, <c>AfterBlankColumns</c>, <c>Until</c>, <c>UntilColumn</c>,
  /// <c>Heading</c>) and the stages that follow them. The postfix forms those entries replay still
  /// exist here, but they are <c>internal</c>: making a contradiction such as <c>x.On(a).On(b)</c>
  /// unspellable at compile time is only possible once the modifiers that carry the erasure are gone
  /// from the surface. See <see cref="PlacementStage"/> and the entries on <see cref="Projection"/>.
  /// </para>
  /// <para>
  /// <b>What stays public here is not geometry.</b> <c>Named</c> labels; <c>Select</c> transforms;
  /// <c>Else</c>, <c>Optional</c> and <c>OrBlank</c> declare tolerance. And <c>Padded</c> — the lone
  /// documented exception: it is extent-geometry, but it <em>nests</em> rather than erases
  /// (<c>x.Padded(1).Padded(2)</c> is two insets both in force, losing nothing), so it is not an
  /// erasure vector and needs no pipeline stage to be made safe.
  /// </para>
  /// <para>
  /// <b>A modifier keeps the demand it is applied to.</b> Each one is generic in the
  /// <em>projection's own type</em> and hands that type back, so <c>Text().Named("t")</c> is an
  /// <c>IProjection&lt;string&gt;</c> exactly as it always was, and the same modifier on a
  /// formula-reading declaration hands back a formula-reading declaration. That is why there is one
  /// of each here rather than one per demand. Raising a demand is a different act with its own
  /// overloads — see <c>ProjectionExtensions.Typed</c> — because only a matcher, not a modifier,
  /// can add one.
  /// </para>
  /// </summary>
  public static partial class ProjectionExtensions
  {
    /// <summary>
    /// Decomposes <paramref name="space"/> and projects it in one call. The projection's own
    /// placement is applied here too, exactly as it would be nested inside another projection.
    /// <para>
    /// <paramref name="space"/> must satisfy whatever the projection demands, which for a
    /// declaration that names no capability is any <see cref="ISpace"/> at all. A declaration that
    /// reads formulas will not compile against a grid that has none.
    /// </para>
    /// <para>
    /// Coordinates in failures are relative to <paramref name="space"/>, so a <c>Map</c> called
    /// from inside another projection's Project restarts them and reports positions relative to its
    /// own space. Compose projections instead of nesting <c>Map</c> calls wherever you can.
    /// </para>
    /// <para>
    /// The root of a path renders by description rather than by a name, deliberately: inferring one
    /// from the receiver would need an optional compiler-supplied parameter, and that would stop
    /// <c>Map</c> being usable as a method group — <c>spaces.Select(report.Map)</c>, one projection
    /// over many workbooks, is the reason this library exists. Name the root with <c>Named</c> if a
    /// path should carry it.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">What the projection demands of the space.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static TResult Map<TSpace, TResult>(this IProjection<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
      => projection.Apply(space).Value;

    /// <summary>
    /// <see cref="Map{TSpace, TResult}"/> plus where the projection landed and how much it
    /// consumed.
    /// </summary>
    /// <typeparam name="TSpace">What the projection demands of the space.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static AppliedResult<TResult> Apply<TSpace, TResult>(this IProjection<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return ProjectionEngine.Apply(Plain(projection), space, ProjectionContext.Root(space));
    }

    /// <summary>
    /// <see cref="Map{TSpace, TResult}"/>, keeping what the decomposition noticed: every tolerance
    /// boundary that absorbed a failure, every alternative a choice passed over, and space the
    /// projection did not describe. A failure nothing declared tolerance for still throws —
    /// declared tolerance is the only thing that ever softens a parse.
    /// <para>
    /// Space nothing described is reported as an <c>Info</c>, except where the entire parse was one
    /// absorbed failure: <c>projection.Optional().MapWithDiagnostics(space)</c> — tolerance
    /// declared at the root, the nearest thing to a lenient mode — would otherwise say "consumed 0
    /// of N rows" underneath a warning that already named the projection, the reason, and the cell.
    /// Anything else still reports, including a root that consumed nothing after absorbing in two
    /// places, or a repeat that found no sections at all.
    /// </para>
    /// <para>
    /// Diagnostics belong to one call: a <c>Map</c> nested inside a projection collects its own and
    /// discards them, so tolerance declared in there is invisible out here. Another reason to
    /// compose projections rather than nest calls.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">What the projection demands of the space.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static MapResult<TResult> MapWithDiagnostics<TSpace, TResult>(this IProjection<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var plain = Plain(projection);
      var context = ProjectionContext.Root(space);
      var mark = context.Diagnostics.Mark();
      var applied = ProjectionEngine.Apply(plain, space, context);

      // Suppressed only when the whole parse is one absorbed failure: two boundaries that each
      // absorbed something have left a gap worth mentioning, even though neither consumed anything.
      if (!(applied.Advance.Width == 0 && applied.Advance.Height == 0 && context.Diagnostics.AbsorbedAt(mark)))
        ReportUnconsumed(plain, space, applied.Offset.Size, applied.Consumed, context);

      return new MapResult<TResult>(applied.Value, context.Diagnostics.Snapshot());
    }

    /// <summary>
    /// Labels the projection, so failures and diagnostics say <paramref name="name"/>.
    /// <para>
    /// A name given here beats the one a layout infers from the use site, so a projection-returning
    /// helper must not name what it returns: every call would produce a projection called the same
    /// thing, wherever it appeared, and the helper is the one place that cannot know which of them
    /// this is. Let the use site name it — <c>var captions = FullRow(); … v.Next(captions)</c>
    /// reads as <c>'captions'</c> — and keep <c>Named</c> for the two jobs inference cannot do:
    /// projections written inline, which have no identifier to borrow, and overriding an inferred
    /// name that reads badly.
    /// </para>
    /// </summary>
    /// <typeparam name="TProjection">The projection's own type, handed back unchanged.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="name">What failures and diagnostics should call it.</param>
    public static TProjection Named<TProjection>(this TProjection projection, string name)
      where TProjection : class, IProjection
      => Cloned<TProjection>(Base(projection).Renamed(name ?? throw new ArgumentNullException(nameof(name))));

    /// <summary>
    /// Falls back to <paramref name="fallback"/> when this projection fails, recording a
    /// <c>Warning</c> that carries the failing projection's own path, location, and problem.
    /// <para>
    /// Tolerance is declared where it is acceptable, and nowhere else: everything under this
    /// projection still fails exactly as loudly, and the failure travels up to the nearest
    /// boundary.
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
    /// well, that failure is what you get, carrying a note about the projection it stood in for.
    /// </para>
    /// <para>
    /// The two projections must read the same thing, and the compiler settles the pair's demand
    /// between them: a plain projection with a formula-reading fallback is a formula-reading
    /// declaration, since either of them may be the one that runs.
    /// </para>
    /// </summary>
    /// <typeparam name="TProjection">The type both projections share, handed back unchanged.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="fallback">What to read instead.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="fallback"/> argument.</param>
    public static TProjection Else<TProjection>(
      this TProjection projection,
      TProjection fallback,
      [CallerArgumentExpression("fallback")] string? declared = null)
      where TProjection : class, IProjection
    {
      if (fallback is null)
        throw new ArgumentNullException(nameof(fallback));

      return Wrapped<TProjection>(Base(projection).Otherwise(fallback, declared));
    }

    /// <summary>
    /// Yields <paramref name="fallbackValue"/> when this projection fails, recording a
    /// <c>Warning</c> that carries the failing projection's own path, location, and problem.
    /// <para>
    /// An absorbed projection consumes nothing beyond its own declared placement — nothing was
    /// read, so no honest extent exists, and a following sibling in a flow starts where this
    /// projection began rather than after it. Pair absorbing boundaries with content-anchored
    /// siblings so what comes next finds itself by content instead of by arithmetic.
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
    /// differs from its receiver's; see <c>ProjectionExtensions.Typed</c> for the demanding form.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="fallbackValue">What to yield instead.</param>
    public static IProjection<T> Else<T>(this IProjection<T> projection, T fallbackValue)
      => new BoundaryProjection<T>(NotNull(projection), null, fallbackValue, Placement.Default, "Else");

    /// <summary>
    /// Yields the default value when this projection fails, recording a <c>Warning</c> that carries
    /// the failing projection's own path, location, and problem — the spelling for a section that
    /// may simply not be there.
    /// <para>
    /// Like <see cref="Else{T}(IProjection{T}, T)"/>, an absorbed projection consumes nothing. For
    /// a value type the filler is <c>default</c> — <c>0</c>, not null — so where the difference
    /// between "absent" and "zero" matters, either give the filler explicitly with
    /// <c>Else(value)</c> or project to a nullable first.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    public static IProjection<T?> Optional<T>(this IProjection<T> projection)
      => new BoundaryProjection<T?>(NotNull(projection).Select(value => (T?)value), null, default, Placement.Default, "Optional");

    /// <summary>
    /// Reads a blank cell as null, quietly — the leaf's way of saying "this field may be absent".
    /// <para>
    /// It is not tolerance and it records nothing: an expected blank is a value the declaration
    /// allowed for, where <see cref="Optional{T}"/> absorbs a <em>failure</em> and says so with a
    /// <c>Warning</c>. Everything else about the leaf is unchanged — a cell of the wrong kind fails
    /// exactly as loudly, and a number that will not fit still fails as a conversion. Blankness is
    /// about the data; a kind is about the format, and no format tolerates the wrong one.
    /// </para>
    /// <para>
    /// This is the standalone spelling of what a nullable member already gets inside
    /// <c>Table&lt;T&gt;()</c>, so the two say the same thing about the same cell:
    /// <code>
    /// Overlay(o =&gt; new Allocation(
    ///   Fund:    o.Next(Text().Right(1)),
    ///   Primary: o.Next(Decimal().OrBlank().Right(6))))
    /// </code>
    /// </para>
    /// <para>
    /// It belongs to the typed cell leaves — <c>Text</c>, <c>Decimal</c>, <c>Integer</c>,
    /// <c>Double</c>, <c>Date</c>, <c>Boolean</c> — because a blank is only a value in a reading
    /// that asserts a kind; on anything else it is a declaration error, raised where the projection
    /// is built rather than per file. The modifiers commute with it: <c>Decimal().Right(6)
    /// .OrBlank()</c> and <c>Decimal().OrBlank().Right(6)</c> are the same declaration.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What the leaf reads.</typeparam>
    /// <param name="projection">The typed cell leaf.</param>
    public static IProjection<T?> OrBlank<T>(this IProjection<T> projection)
      where T : struct
      => Leaf(projection).Tolerating<T?>(value => value);

    /// <inheritdoc cref="OrBlank{T}(IProjection{T})"/>
    /// <param name="projection">The <c>Text</c> leaf.</param>
    /// <remarks>
    /// The reference-typed half of the family, and there is exactly one leaf in it. A backend's
    /// <c>Formula()</c> is deliberately not reachable here: its null already means <em>that cell is
    /// not computed</em>, and a second null meaning <em>that cell is empty</em> would put two
    /// answers behind one spelling.
    /// </remarks>
    public static IProjection<string?> OrBlank(this IProjection<string> projection)
      => Leaf(projection).Tolerating<string?>(value => value);

    /// <summary>
    /// Passes this projection's result through <paramref name="selector"/>. The wrapper is a
    /// projection like any other, so <c>Named</c> and the placement modifiers work on either side
    /// of it.
    /// </summary>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <typeparam name="TResult">What the selector produces.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="selector">The transformation applied to what it reads.</param>
    public static IProjection<TResult> Select<T, TResult>(this IProjection<T> projection, Func<T, TResult> selector)
      => new MapProjection<T, TResult>(NotNull(projection), selector, Placement.Default);

    /// <summary>
    /// Insets the projection's extent by <paramref name="all"/> cells on every side.
    /// </summary>
    /// <typeparam name="TProjection">The projection's own type, handed back unchanged.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="all">The inset on every side.</param>
    public static TProjection Padded<TProjection>(this TProjection projection, int all)
      where TProjection : class, IProjection
    {
      NotNegative(all, nameof(all));

      return Pad(projection, all, all, all, all);
    }

    /// <summary>
    /// Insets the projection's extent by <paramref name="horizontal"/> cells left and right and
    /// <paramref name="vertical"/> cells top and bottom.
    /// </summary>
    /// <typeparam name="TProjection">The projection's own type, handed back unchanged.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="horizontal">The inset left and right.</param>
    /// <param name="vertical">The inset top and bottom.</param>
    public static TProjection Padded<TProjection>(this TProjection projection, int horizontal, int vertical)
      where TProjection : class, IProjection
    {
      NotNegative(horizontal, nameof(horizontal));
      NotNegative(vertical, nameof(vertical));

      return Pad(projection, horizontal, vertical, horizontal, vertical);
    }

    /// <summary>
    /// Insets the projection's extent by the given amounts, so the projection reads the inside of
    /// its region and still consumes the whole of it — a border of labels around a block of
    /// numbers, say.
    /// <para>
    /// Padding shrinks the inside, where an offset shifts the outside; that is the difference
    /// between this and the movement modifiers, and the two compose freely.
    /// </para>
    /// <para>
    /// <b>It is the one geometry modifier that stays postfix.</b> Every other extent or placement
    /// modifier moved into the pipeline, because each could erase what came before it and the
    /// pipeline makes that unspellable. <c>Padded</c> cannot erase — it wraps, so two of them nest —
    /// so it is safe as a modifier and needs no stage of its own.
    /// </para>
    /// </summary>
    /// <typeparam name="TProjection">The projection's own type, handed back unchanged.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="left">The inset on the left.</param>
    /// <param name="top">The inset on the top.</param>
    /// <param name="right">The inset on the right.</param>
    /// <param name="bottom">The inset on the bottom.</param>
    public static TProjection Padded<TProjection>(this TProjection projection, int left, int top, int right, int bottom)
      where TProjection : class, IProjection
    {
      NotNegative(left, nameof(left));
      NotNegative(top, nameof(top));
      NotNegative(right, nameof(right));
      NotNegative(bottom, nameof(bottom));

      return Pad(projection, left, top, right, bottom);
    }

    /// <summary>
    /// The projection as the engine sees it. Sound because every <see cref="IProjection{TSpace,
    /// TResult}"/> this library produces is a <see cref="ProjectionBase{TResult}"/>, which
    /// implements <see cref="IProjection{TResult}"/>; the demand lives only in the static type, so
    /// forgetting it here is the identity.
    /// </summary>
    internal static IProjection<T> Plain<TSpace, T>(IProjection<TSpace, T> projection)
      where TSpace : class, ISpace
      => projection as IProjection<T> ?? throw NotOurs(projection, nameof(projection));

    private static TProjection Pad<TProjection>(TProjection projection, int left, int top, int right, int bottom)
      where TProjection : class, IProjection
      => Wrapped<TProjection>(Base(projection).Inset(left, top, right, bottom));

    /// <summary>
    /// The receiver as the typed cell leaf <c>OrBlank</c> needs it to be. A leaf keeps its own class
    /// through every clone-returning modifier, so this recognises a placed and named one as well as
    /// a bare one; anything else never asserted a kind, and reading a blank as null there would be
    /// inventing a meaning the declaration never stated.
    /// </summary>
    private static TypedCellProjection<T> Leaf<T>(IProjection<T> projection)
      => NotNull(projection) as TypedCellProjection<T>
        ?? throw new ArgumentException(
          $"OrBlank reads a blank cell as null, so it belongs on a cell leaf that declares a kind — "
          + $"Text, Decimal, Integer, Double, Date or Boolean. {ProjectionContext.Describe(projection)} is not one.",
          nameof(projection));

    /// <summary>The projection as this library builds them, which is the only kind a modifier can modify.</summary>
    private static ProjectionBase Base<TProjection>(TProjection projection)
      where TProjection : class, IProjection
      => NotNull(projection) as ProjectionBase ?? throw NotOurs(projection, nameof(projection));

    /// <summary>
    /// A clone back as the receiver's own type. Total: a clone has the receiver's runtime type, so
    /// it is whatever the receiver was seen as.
    /// </summary>
    private static TProjection Cloned<TProjection>(IProjection clone)
      where TProjection : class, IProjection
      => (TProjection)clone;

    /// <summary>
    /// A wrapper back as the receiver's own type. A wrapper is a new projection reading the same
    /// thing, so it satisfies every <em>interface</em> the receiver was seen through — but it is
    /// not the receiver's class, which only a caller holding a projection by its own concrete type
    /// would ask for.
    /// </summary>
    private static TProjection Wrapped<TProjection>(IProjection wrapper)
      where TProjection : class, IProjection
      => wrapper as TProjection
        ?? throw new InvalidOperationException(
          $"A modifier that wraps hands back a {wrapper.GetType().Name}, which is not a {typeof(TProjection).Name}. "
          + "Hold the projection as IProjection<T> — or, where it demands a capability, as IProjection<TSpace, T> — rather than as its own class.");

    private static ArgumentException NotOurs(IProjection? projection, string parameter)
      => new ArgumentException(
        $"{projection?.GetType().Name ?? "null"} is not a projection this library built; only those can be modified or applied.",
        parameter);

    /// <summary>One guard: the receiver defaults to its own parameter name, anything else names itself.</summary>
    private static T NotNull<T>(T value, string parameter = "projection") where T : class
      => value ?? throw new ArgumentNullException(parameter);

    private static int NotNegative(int distance, string parameter)
      => distance >= 0 ? distance : throw new ArgumentOutOfRangeException(parameter, distance, "A projection cannot be inset or moved a negative distance.");
  }
}
