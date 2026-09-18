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
  /// <see cref="ProjectionBuilders{TSpace}"/> (<c>On</c>, <c>Below</c>, <c>RightOf</c>,
  /// <c>OffsetBy</c>, <c>Down</c>, <c>Right</c>, <c>AfterBlankRows</c>, <c>AfterBlankColumns</c>,
  /// <c>Until</c>, <c>UntilColumn</c>, <c>Heading</c>) and the stages that follow them
  /// (<see cref="PlacementStage{TSpace}"/>). Making a contradiction such as <c>x.On(a).On(b)</c>
  /// unspellable at compile time is only possible while no modifier here can carry that erasure.
  /// </para>
  /// <para>
  /// <b>What stays public here is not geometry.</b> <c>Named</c> labels; <c>Select</c> transforms;
  /// <c>Else</c>, <c>Optional</c> and <c>OrBlank</c> declare tolerance. And <c>Padded</c> — the lone
  /// documented exception: it is extent-geometry, but it <em>nests</em> rather than erases
  /// (<c>x.Padded(1).Padded(2)</c> is two insets both in force, losing nothing), so it is not an
  /// erasure vector and needs no pipeline stage to be made safe.
  /// </para>
  /// <para>
  /// <b>A modifier keeps the space it is applied to.</b> Each one is generic in the
  /// <em>projection's own type</em> and hands that type back, so <c>Text().Named("t")</c> is the
  /// same <c>IProjectionDefinition&lt;TSpace, string&gt;</c> it was. The three that cannot —
  /// <c>Optional</c>, <c>Select</c> and <c>Else(value)</c> — read the space as a separate type
  /// parameter, because C# has no way to say "this projection with its result swapped".
  /// </para>
  /// </summary>
  public static partial class ProjectionExtensions
  {
    /// <summary>
    /// Decomposes <paramref name="space"/> and projects it in one call. The projection's own
    /// placement is applied here too, exactly as it would be nested inside another projection.
    /// <para>
    /// <paramref name="space"/> is the very type the declaration was written over, so a
    /// declaration that reads formulas will not compile against a grid that has none.
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
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static TResult Map<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
      => projection.Apply(space).Value;

    /// <summary>
    /// <see cref="Map{TSpace, TResult}"/> plus where the projection landed and how much it
    /// consumed.
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static AppliedResult<TResult> Apply<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return ProjectionEngine.Pushing
        ? PushSession<TSpace>.Apply(projection, space, ProjectionContext.Root(space))
        : ProjectionEngine.Apply(projection, Plane<TSpace>.Of(space), ProjectionContext.Root(space));
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
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static MapResult<TResult> MapWithDiagnostics<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var context = ProjectionContext.Root(space);
      var mark = context.Diagnostics.Mark();
      var extent = Plane<TSpace>.Of(space);
      var applied = ProjectionEngine.Pushing
        ? PushSession<TSpace>.Apply(projection, space, context)
        : ProjectionEngine.Apply(projection, extent, context);

      // Suppressed only when the whole parse is one absorbed failure: two boundaries that each
      // absorbed something have left a gap worth mentioning, even though neither consumed anything.
      if (!(applied.Advance.Width == 0 && applied.Advance.Height == 0 && context.Diagnostics.AbsorbedAt(mark)))
        ReportUnconsumed(projection, extent, applied.Offset.Size, applied.Consumed, context);

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
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="name">What failures and diagnostics should call it.</param>
    public static IProjectionDefinition<TSpace, TResult> Named<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, string name)
      where TSpace : class, ISpace
      => NotNull(projection).With(projection.Annotations.WithName(name));

    /// <summary>
    /// Calls the projection <paramref name="name"/> in a failure or diagnostic path: the node renders
    /// that label unquoted, as the kind of thing it is rather than as the factory that built it, and
    /// it is opaque — a wrapper that would otherwise contribute no segment claims one here.
    /// <para>
    /// The label sits beside <c>.Named</c> rather than replacing it, so a unit that is also named
    /// renders <c>label:name</c> — <c>Table:fruit</c>.
    /// </para>
    /// <para>
    /// It folds nothing on its own. What collapses a path is <see cref="AsScaffolding"/>, marked on
    /// the parts a factory assembled, and the uncollapsed path is kept on
    /// <c>ProjectionException.FullPath</c> and <c>ProjectionDiagnostic.FullPath</c> either way.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="name">What a path and a subject should call the unit.</param>
    public static IProjectionDefinition<TSpace, TResult> AsUnit<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, string name)
      where TSpace : class, ISpace
      => NotNull(projection).With(projection.Annotations.WithUnitName(name));

    /// <summary>
    /// Marks the projection a composition's internal plumbing: in a failure or diagnostic path it
    /// contributes no segment of its own, carrying only its occurrence index up onto the nearest
    /// segment that was kept, while the full uncollapsed path keeps it for drill-through.
    /// <para>
    /// This is what a factory marks the parts it assembles with, so a declaration the user wrote as
    /// one thing reads as one thing. Marking a projection the user wrote would hide it from every
    /// path that goes through it.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    public static IProjectionDefinition<TSpace, TResult> AsScaffolding<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection)
      where TSpace : class, ISpace
      => NotNull(projection).With(projection.Annotations.AsScaffolding());

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
    /// <typeparam name="TSpace">The space both projections are written over.</typeparam>
    /// <typeparam name="TResult">What both projections read.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="fallback">What to read instead.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="fallback"/> argument.</param>
    public static IProjectionDefinition<TSpace, TResult> Else<TSpace, TResult>(
      this IProjectionDefinition<TSpace, TResult> projection,
      IProjectionDefinition<TSpace, TResult> fallback,
      [CallerArgumentExpression("fallback")] string? declared = null)
      where TSpace : class, ISpace
      => new FallbackDefinition<TSpace, TResult>(
        NotNull(projection),
        NotNull(fallback, nameof(fallback)),
        default!,
        Placement.Default,
        "Else",
        UseSite.From(declared, null));

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
    /// differs from its receiver's: it hands back an <see cref="IProjectionDefinition{TSpace, TResult}"/>
    /// rather than the receiver's own projection type, which is why a placement modifier goes before
    /// it and not after.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="fallbackValue">What to yield instead.</param>
    public static IProjectionDefinition<TSpace, T> Else<TSpace, T>(this IProjectionDefinition<TSpace, T> projection, T fallbackValue)
      where TSpace : class, ISpace
      => new FallbackDefinition<TSpace, T>(NotNull(projection), null, fallbackValue, Placement.Default, "Else");

    /// <summary>
    /// Yields the default value when this projection fails, recording a <c>Warning</c> that carries
    /// the failing projection's own path, location, and problem — the spelling for a section that
    /// may simply not be there.
    /// <para>
    /// Like <see cref="Else{TSpace, T}(IProjectionDefinition{TSpace, T}, T)"/>, an absorbed projection consumes nothing. For
    /// a value type the filler is <c>default</c> — <c>0</c>, not null — so where the difference
    /// between "absent" and "zero" matters, either give the filler explicitly with
    /// <c>Else(value)</c> or project to a nullable first.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    public static IProjectionDefinition<TSpace, T?> Optional<TSpace, T>(this IProjectionDefinition<TSpace, T> projection)
      where TSpace : class, ISpace
      => new FallbackDefinition<TSpace, T?>(NotNull(projection).Select(value => (T?)value), null, default, Placement.Default, "Optional");

    /// <summary>
    /// Reads a blank cell as null, quietly — the leaf's way of saying "this field may be absent".
    /// <para>
    /// It is not tolerance and it records nothing: an expected blank is a value the declaration
    /// allowed for, where <see cref="Optional{TSpace, T}"/> absorbs a <em>failure</em> and says so with a
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
    /// It belongs to a cell leaf and nothing else: <c>AsText</c>, the canonical one, and whatever
    /// kinded leaves a backend publishes — <c>Text</c>, <c>Decimal</c>, <c>Integer</c>,
    /// <c>Double</c>, <c>Date</c>, <c>Boolean</c> in <c>Unrect.Spreadsheets</c>. A blank is a value
    /// in a reading of one cell and a declaration error on anything else, raised where the
    /// projection is built rather than per file. The modifiers commute with it —
    /// <c>Decimal().Right(6).OrBlank()</c> and <c>Decimal().OrBlank().Right(6)</c> are the same
    /// declaration — because the widening carries the leaf's placement and its naming across.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the leaf is written over.</typeparam>
    /// <typeparam name="T">What the leaf reads.</typeparam>
    /// <param name="projection">The cell leaf.</param>
    public static IProjectionDefinition<TSpace, T?> OrBlank<TSpace, T>(this IProjectionDefinition<TSpace, T> projection)
      where TSpace : class, ISpace
      where T : struct
      => Leaf(projection).Tolerating<T?>(value => value);

    /// <inheritdoc cref="OrBlank{TSpace, T}(IProjectionDefinition{TSpace, T})"/>
    /// <typeparam name="TSpace">The space the leaf is written over.</typeparam>
    /// <param name="projection">The <c>AsText</c> leaf, or a backend's <c>Text</c>.</param>
    /// <remarks>
    /// The reference-typed half of the family. A backend's <c>Formula()</c> is deliberately not
    /// reachable here: its null already means <em>that cell is not computed</em>, and a second null
    /// meaning <em>that cell is empty</em> would put two answers behind one spelling.
    /// </remarks>
    public static IProjectionDefinition<TSpace, string?> OrBlank<TSpace>(this IProjectionDefinition<TSpace, string> projection)
      where TSpace : class, ISpace
      => Leaf(projection).Tolerating<string?>(value => value);

    /// <summary>
    /// Passes this projection's result through <paramref name="selector"/>. The wrapper is a
    /// projection like any other, so <c>Named</c> and the placement modifiers work on either side
    /// of it.
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <typeparam name="TResult">What the selector produces.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="selector">The transformation applied to what it reads.</param>
    public static IProjectionDefinition<TSpace, TResult> Select<TSpace, T, TResult>(this IProjectionDefinition<TSpace, T> projection, Func<T, TResult> selector)
      where TSpace : class, ISpace
      => new SelectDefinition<TSpace, T, TResult>(NotNull(projection), selector, Placement.Default);

    /// <summary>
    /// Insets the projection's extent by <paramref name="all"/> cells on every side.
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="all">The inset on every side.</param>
    public static IProjectionDefinition<TSpace, TResult> Padded<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, int all)
      where TSpace : class, ISpace
    {
      NotNegative(all, nameof(all));

      return Pad(projection, all, all, all, all);
    }

    /// <summary>
    /// Insets the projection's extent by <paramref name="horizontal"/> cells left and right and
    /// <paramref name="vertical"/> cells top and bottom.
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="horizontal">The inset left and right.</param>
    /// <param name="vertical">The inset top and bottom.</param>
    public static IProjectionDefinition<TSpace, TResult> Padded<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, int horizontal, int vertical)
      where TSpace : class, ISpace
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
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="left">The inset on the left.</param>
    /// <param name="top">The inset on the top.</param>
    /// <param name="right">The inset on the right.</param>
    /// <param name="bottom">The inset on the bottom.</param>
    public static IProjectionDefinition<TSpace, TResult> Padded<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, int left, int top, int right, int bottom)
      where TSpace : class, ISpace
    {
      NotNegative(left, nameof(left));
      NotNegative(top, nameof(top));
      NotNegative(right, nameof(right));
      NotNegative(bottom, nameof(bottom));

      return Pad(projection, left, top, right, bottom);
    }

    private static IProjectionDefinition<TSpace, TResult> Pad<TSpace, TResult>(IProjectionDefinition<TSpace, TResult> projection, int left, int top, int right, int bottom)
      where TSpace : class, ISpace
      => new PadDefinition<TSpace, TResult>(NotNull(projection), left, top, right, bottom, Placement.Default);

    /// <summary>
    /// The receiver as the one node that can tolerate a blank: a blank is a value in a reading of
    /// one cell, and on anything else it is a declaration error, which is why refusing here is
    /// where that error is raised.
    /// </summary>
    private static ReadDefinition<TSpace, T> Leaf<TSpace, T>(IProjectionDefinition<TSpace, T> projection)
      where TSpace : class, ISpace
      => NotNull(projection) as ReadDefinition<TSpace, T>
        ?? throw new ArgumentException(
          "OrBlank reads a blank cell as null, so it belongs on a cell leaf — AsText, or one of a "
          + $"backend's kinded leaves. {ProjectionContext.Describe(projection)} is not one.",
          nameof(projection));

    /// <summary>One guard: the receiver defaults to its own parameter name, anything else names itself.</summary>
    private static T NotNull<T>(T value, string parameter = "projection") where T : class
      => value ?? throw new ArgumentNullException(parameter);

    private static int NotNegative(int distance, string parameter)
      => distance >= 0 ? distance : throw new ArgumentOutOfRangeException(parameter, distance, "A projection cannot be inset or moved a negative distance.");
  }
}
