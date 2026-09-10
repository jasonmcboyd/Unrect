using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

namespace PlacementGauntlet.Staged
{
  /// <summary>
  /// SPIKE. What a pipeline has declared so far, held as the modifier calls that will write it onto
  /// the terminal's projection.
  /// <para>
  /// The façade is a THIN SPELLING over today's engine: it changes no runtime semantics. Rather than
  /// compose strategies itself, a stage records the modifier the author wrote and replays it once,
  /// in declaration order, on the projection the terminal builds. That is what keeps
  /// <c>Down(2).Table&lt;T&gt;()</c> meaning what <c>Table&lt;T&gt;().Down(2)</c> means today — a
  /// movement COMPOSES onto a shape's own default offset, where <c>OffsetBy</c> would replace it, so
  /// a façade that flattened both into one strategy would silently change the corpus.
  /// </para>
  /// </summary>
  internal sealed class Steps
  {
    internal static readonly Steps None = new Steps(Array.Empty<Step>());

    private readonly Step[] _steps;

    private Steps(Step[] steps) => _steps = steps;

    internal Steps Then(Step step)
    {
      var next = new Step[_steps.Length + 1];

      Array.Copy(_steps, next, _steps.Length);
      next[_steps.Length] = step;

      return new Steps(next);
    }

    /// <summary>
    /// Records <paramref name="step"/> as the INNERMOST one, whatever was declared before it.
    /// <para>
    /// One step needs this: <c>Under</c>. The captions and the subject together are the thing an
    /// anchor places — the documented repeat-stop recipe is <c>lines.Under(cap).On(mark)</c>, where
    /// the anchor sits on the flow so that running out of anchors ends the repetition. Under the
    /// geography law that reads <c>On(mark).Under(cap).Of(lines)</c>: left to right is top to bottom
    /// on the sheet, and the replay is inside-out, which is what this prepend is for.
    /// </para>
    /// </summary>
    internal Steps Before(Step step)
    {
      var next = new Step[_steps.Length + 1];

      next[0] = step;
      Array.Copy(_steps, 0, next, 1, _steps.Length);

      return new Steps(next);
    }

    /// <summary>
    /// Writes the declared placement onto <paramref name="projection"/> — each slot exactly once, so
    /// the shipped declared-over-declared refusal can never fire from inside the façade.
    /// </summary>
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
    OnRow, OnColumn, Below, RightOf, OffsetBy, Down, Right, AfterBlankRows, AfterBlankColumns, Sized, UntilRow, UntilColumn, Under,
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

    internal static Step OffsetBy(IOffsetStrategy offset) => new Step(StepKind.OffsetBy, NotNull(offset));

    internal static Step Down(int rows) => new Step(StepKind.Down, count: rows);

    internal static Step Right(int columns) => new Step(StepKind.Right, count: columns);

    internal static Step AfterBlankRows() => new Step(StepKind.AfterBlankRows);

    internal static Step AfterBlankColumns() => new Step(StepKind.AfterBlankColumns);

    internal static Step Sized(IAreaStrategy area) => new Step(StepKind.Sized, NotNull(area));

    internal static Step UntilRow(IRowLandmark landmark, bool orEnd) => new Step(StepKind.UntilRow, NotNull(landmark), orEnd: orEnd);

    internal static Step UntilColumn(IColumnLandmark landmark, bool orEnd) => new Step(StepKind.UntilColumn, NotNull(landmark), orEnd: orEnd);

    /// <summary>
    /// The captions a section sits under. Copied, because a step is held for every future
    /// application of the pipeline and a declaration that could change is not a declaration.
    /// </summary>
    internal static Step Under(IProjection<string>[] captions)
    {
      if (NotNull(captions, "captions").Length == 0)
        throw new ArgumentException("A projection must sit under at least one caption.", "captions");

      return new Step(StepKind.Under, (IProjection<string>[])captions.Clone());
    }

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
      StepKind.Under => projection.Under((IProjection<string>[])_subject!),
      _ => throw new InvalidOperationException($"Unknown step {_kind}."),
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
      IProjection<string>[] captions => string.Join(", ", System.Linq.Enumerable.Select(captions, c => c.Description)),
      null => "?",
      _ => subject.GetType().Name,
    };

    private static T NotNull<T>(T value, string parameter = "landmark") where T : class
      => value ?? throw new ArgumentNullException(parameter);
  }
}
