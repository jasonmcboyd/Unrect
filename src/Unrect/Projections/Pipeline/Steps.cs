using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// What a placement pipeline has declared so far, held as the modifier calls that will write it
  /// onto the projection its terminal builds.
  /// <para>
  /// <b>The pipeline is a spelling, not a semantics.</b> A stage records the modifier the author
  /// wrote and replays it once, in declaration order, rather than composing strategies itself. That
  /// order is load-bearing: an offset step <em>composes</em> onto an offset an earlier stage declared,
  /// and a pipeline that reordered the steps would quietly change every declaration that relies on the
  /// composition. An offset step with no earlier offset — <c>Right(1).Of(Table())</c> — starts from
  /// the origin, <em>replacing</em> the shape's own default rather than composing onto it (see
  /// <c>Offset</c>).
  /// </para>
  /// <para>
  /// Each slot is written at most once, because the stage types make a second write unspellable —
  /// so the declared-over-declared refusal in <see cref="ProjectionExtensions"/> can never fire from
  /// inside a pipeline.
  /// </para>
  /// </summary>
  internal sealed class Steps
  {
    internal static readonly Steps None = new Steps(Array.Empty<Step>());

    private readonly Step[] _steps;

    private Steps(Step[] steps) => _steps = steps;

    /// <summary>Records <paramref name="step"/> as the outermost one so far.</summary>
    internal Steps Then(Step step)
    {
      var next = new Step[_steps.Length + 1];

      Array.Copy(_steps, next, _steps.Length);
      next[_steps.Length] = step;

      return new Steps(next);
    }

    /// <summary>
    /// Records <paramref name="step"/> as the <em>innermost</em> one, whatever was declared before
    /// it.
    /// <para>
    /// One step needs this: the headings. A heading and the section it announces are together the
    /// thing an anchor places — the repeat-stop recipe puts the anchor on the heading-and-content
    /// flow, so that running out of headings ends the repetition. Left to right on the page is top
    /// to bottom on the sheet, and the replay is inside-out, which is what this prepend is for.
    /// </para>
    /// </summary>
    internal Steps Before(Step step)
    {
      var next = new Step[_steps.Length + 1];

      next[0] = step;
      Array.Copy(_steps, 0, next, 1, _steps.Length);

      return new Steps(next);
    }

    /// <summary>Writes the declared placement onto <paramref name="projection"/>, each slot once.</summary>
    internal IProjection<T> ApplyTo<T>(IProjection<T> projection)
    {
      foreach (var step in _steps)
        projection = step.ApplyTo(projection);

      return projection;
    }

    public override string ToString()
      => _steps.Length == 0 ? "(adjacent, sized to children)" : string.Join<Step>(".", _steps);
  }

  internal enum StepKind
  {
    OnRow,
    OnColumn,
    Below,
    RightOf,
    OffsetBy,
    Down,
    Right,
    AfterBlankRows,
    AfterBlankColumns,
    SkipToFirstNonBlankCell,
    Sized,
    UntilRow,
    UntilColumn,
    Headings,
  }

  /// <summary>One modifier the author declared, replayed by name so the semantics are today's exactly.</summary>
  internal sealed class Step
  {
    private readonly StepKind _kind;
    private readonly object? _subject;
    private readonly int _count;
    private readonly bool _orEnd;

    private Step(StepKind kind, object? subject = null, int count = 0, bool orEnd = false)
    {
      _kind = kind;
      _subject = subject;
      _count = count;
      _orEnd = orEnd;
    }

    internal static Step OnRow(IRowLandmark landmark) => new Step(StepKind.OnRow, NotNull(landmark));

    internal static Step OnColumn(IColumnLandmark landmark) => new Step(StepKind.OnColumn, NotNull(landmark));

    internal static Step Below(IRowLandmark landmark) => new Step(StepKind.Below, NotNull(landmark));

    internal static Step RightOf(IColumnLandmark landmark) => new Step(StepKind.RightOf, NotNull(landmark));

    internal static Step OffsetBy(IOffsetStrategy offset) => new Step(StepKind.OffsetBy, NotNull(offset, "offset"));

    internal static Step Down(int rows) => new Step(StepKind.Down, count: NotNegative(rows, nameof(rows)));

    internal static Step Right(int columns) => new Step(StepKind.Right, count: NotNegative(columns, nameof(columns)));

    internal static Step AfterBlankRows() => new Step(StepKind.AfterBlankRows);

    internal static Step AfterBlankColumns() => new Step(StepKind.AfterBlankColumns);

    internal static Step SkipToFirstNonBlankCell() => new Step(StepKind.SkipToFirstNonBlankCell);

    internal static Step Sized(IAreaStrategy area) => new Step(StepKind.Sized, NotNull(area, "area"));

    internal static Step UntilRow(IRowLandmark landmark, bool orEnd) => new Step(StepKind.UntilRow, NotNull(landmark), orEnd: orEnd);

    internal static Step UntilColumn(IColumnLandmark landmark, bool orEnd) => new Step(StepKind.UntilColumn, NotNull(landmark), orEnd: orEnd);

    /// <summary>
    /// The headings a section announces itself by, as the caption leaves the replay folds into one
    /// vertical flow. The array is the stage's own and is never handed out, so it is safe to hold.
    /// </summary>
    internal static Step Headings(IProjection<string>[] captions) => new Step(StepKind.Headings, captions);

    /// <summary>
    /// Writes this step onto <paramref name="projection"/> by manipulating its <see cref="Placement"/>
    /// directly — the offset/area/bound operation the retired postfix modifier used to wrap. There is
    /// no public modifier to route through, and none is needed: geometry carries no
    /// <c>CallerArgumentExpression</c> capture, so the modifier surface added nothing the step does
    /// not. The pipeline stage types write each slot at most once, so the declared-over-declared
    /// guards the modifiers carried can never fire here and are simply not replayed.
    /// </summary>
    internal IProjection<T> ApplyTo<T>(IProjection<T> projection)
    {
      var subject = (ProjectionBase)projection;

      return _kind switch
      {
        // An offset is an offset: every kind composes onto an earlier pipeline offset, or starts
        // from the origin. An anchor is structurally always the first offset step, so it always
        // starts fresh — replacing the shape's own default, as it always has.
        StepKind.OnRow => Offset<T>(subject, OffsetStrategies.To((IRowLandmark)_subject!)),
        StepKind.OnColumn => Offset<T>(subject, OffsetStrategies.To((IColumnLandmark)_subject!)),
        StepKind.Below => Offset<T>(subject, OffsetStrategies.Past((IRowLandmark)_subject!)),
        StepKind.RightOf => Offset<T>(subject, OffsetStrategies.Past((IColumnLandmark)_subject!)),
        StepKind.OffsetBy => Offset<T>(subject, (IOffsetStrategy)_subject!),
        StepKind.Down => Offset<T>(subject, OffsetStrategies.ExplicitOffset(0, _count)),
        StepKind.Right => Offset<T>(subject, OffsetStrategies.ExplicitOffset(_count, 0)),
        StepKind.AfterBlankRows => Offset<T>(subject, OffsetStrategies.SkipBlankRows()),
        StepKind.AfterBlankColumns => Offset<T>(subject, OffsetStrategies.SkipBlankColumns()),
        StepKind.SkipToFirstNonBlankCell => Offset<T>(subject, OffsetStrategies.SkipToFirstNonBlankCell()),

        // An extent replaces the projection's derived one.
        StepKind.Sized => (IProjection<T>)subject.Replaced(subject.Placement.WithArea((IAreaStrategy)_subject!)),

        // Bounds and headings wrap rather than reposition.
        StepKind.UntilRow => (IProjection<T>)subject.BoundedBy(Landmark.Of((IRowLandmark)_subject!), _orEnd),
        StepKind.UntilColumn => (IProjection<T>)subject.BoundedBy(Landmark.Of((IColumnLandmark)_subject!), _orEnd),
        StepKind.Headings => (IProjection<T>)subject.WithHeadings((IProjection<string>[])_subject!),

        _ => throw new InvalidOperationException($"Unknown placement step {_kind}."),
      };
    }

    /// <summary>
    /// The one offset rule, for every offset step alike. <paramref name="offset"/> composes onto an
    /// offset an earlier pipeline stage declared when there is one to compose with — the placement is
    /// BOTH pipeline-declared (<see cref="Placement.OffsetWasDeclared"/>) AND carries a real offset
    /// (<see cref="Placement.HasDeclaredOffset"/>) — and otherwise starts from the origin.
    /// <para>
    /// Starting fresh is what replaces a shape's own default (a <c>Table</c>'s self-locate — not
    /// pipeline-declared) and what ignores an explicit no-op (<c>OffsetBy(MinOffset())</c> —
    /// pipeline-declared but the canonical origin), so "saying no movement out loud says nothing"
    /// holds. An anchor is always the first offset step (a second anchor is unspellable), so it always
    /// starts fresh, exactly as an unconditional replace would.
    /// </para>
    /// </summary>
    private static IProjection<T> Offset<T>(ProjectionBase subject, IOffsetStrategy offset)
    {
      var placement = subject.Placement;
      var composeOntoBase = placement.OffsetWasDeclared && placement.HasDeclaredOffset;
      var composed = composeOntoBase ? OffsetStrategies.Then(placement.Offset, offset) : offset;

      return (IProjection<T>)subject.Replaced(placement.WithOffset(composed));
    }

    public override string ToString() => _kind switch
    {
      StepKind.Down or StepKind.Right => $"{_kind}({_count})",
      StepKind.AfterBlankRows or StepKind.AfterBlankColumns or StepKind.SkipToFirstNonBlankCell => $"{_kind}()",
      _ => $"{_kind}({Describe(_subject)})",
    };

    private static string Describe(object? subject) => subject switch
    {
      IRowLandmark row => row.Description,
      IColumnLandmark column => column.Description,
      IProjection<string>[] captions => string.Join(", ", Array.ConvertAll(captions, caption => caption.Description)),
      null => "?",
      _ => subject.GetType().Name,
    };

    private static T NotNull<T>(T value, string parameter = "landmark") where T : class
      => value ?? throw new ArgumentNullException(parameter);

    private static int NotNegative(int distance, string parameter)
      => distance >= 0 ? distance : throw new ArgumentOutOfRangeException(parameter, distance, "A projection cannot be inset or moved a negative distance.");
  }
}
