using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a placement pipeline has declared so far, held as the modifier calls that will write it
  /// onto the projection its terminal builds.
  /// <para>
  /// <b>The pipeline is a spelling, not a semantics.</b> A stage records the modifier the author
  /// wrote and replays it once, in declaration order, rather than composing strategies itself. That
  /// is what keeps <c>Down(2).Table&lt;T&gt;()</c> meaning exactly what <c>Table&lt;T&gt;().Down(2)</c>
  /// means: a movement <em>composes</em> onto a shape's own default offset where <c>OffsetBy</c>
  /// <em>replaces</em> it, and a pipeline that flattened both into one strategy would quietly change
  /// every declaration that relies on the difference.
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

    internal static Step Down(int rows) => new Step(StepKind.Down, count: rows);

    internal static Step Right(int columns) => new Step(StepKind.Right, count: columns);

    internal static Step AfterBlankRows() => new Step(StepKind.AfterBlankRows);

    internal static Step AfterBlankColumns() => new Step(StepKind.AfterBlankColumns);

    internal static Step Sized(IAreaStrategy area) => new Step(StepKind.Sized, NotNull(area, "area"));

    internal static Step UntilRow(IRowLandmark landmark, bool orEnd) => new Step(StepKind.UntilRow, NotNull(landmark), orEnd: orEnd);

    internal static Step UntilColumn(IColumnLandmark landmark, bool orEnd) => new Step(StepKind.UntilColumn, NotNull(landmark), orEnd: orEnd);

    /// <summary>
    /// The headings a section announces itself by, as the caption leaves the replay hands to
    /// <c>Under</c>. The array is the stage's own and is never handed out, so it is safe to hold.
    /// </summary>
    internal static Step Headings(IProjection<string>[] captions) => new Step(StepKind.Headings, captions);

    internal IProjection<T> ApplyTo<T>(IProjection<T> projection) => _kind switch
    {
      StepKind.OnRow => projection.On((IRowLandmark)_subject!),
      StepKind.OnColumn => projection.On((IColumnLandmark)_subject!),
      StepKind.Below => projection.Below((IRowLandmark)_subject!),
      StepKind.RightOf => projection.RightOf((IColumnLandmark)_subject!),
      StepKind.OffsetBy => projection.OffsetBy((IOffsetStrategy)_subject!),
      StepKind.Down => projection.Down(_count),
      StepKind.Right => projection.Right(_count),
      StepKind.AfterBlankRows => projection.AfterBlankRows(),
      StepKind.AfterBlankColumns => projection.AfterBlankColumns(),
      StepKind.Sized => projection.Sized((IAreaStrategy)_subject!),
      StepKind.UntilRow => projection.Until((IRowLandmark)_subject!, _orEnd),
      StepKind.UntilColumn => projection.UntilColumn((IColumnLandmark)_subject!, _orEnd),
      StepKind.Headings => projection.Under((IProjection<string>[])_subject!),
      _ => throw new InvalidOperationException($"Unknown placement step {_kind}."),
    };

    public override string ToString() => _kind switch
    {
      StepKind.Down or StepKind.Right => $"{_kind}({_count})",
      StepKind.AfterBlankRows or StepKind.AfterBlankColumns => $"{_kind}()",
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
  }
}
