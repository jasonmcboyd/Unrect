using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// What a placement pipeline has declared so far, held as the modifier calls its terminal replays
  /// onto the projection in declaration order. Each slot is written at most once.
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
    /// it — used by the headings, which must wrap the section they announce.
    /// </summary>
    internal Steps Before(Step step)
    {
      var next = new Step[_steps.Length + 1];

      next[0] = step;
      Array.Copy(_steps, 0, next, 1, _steps.Length);

      return new Steps(next);
    }

    /// <summary>Writes the declared placement onto <paramref name="projection"/>, each slot once.</summary>
    internal IProjectionDefinition<TSpace, T> ApplyTo<TSpace, T>(IProjectionDefinition<TSpace, T> projection)
      where TSpace : class, ISpace
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
    internal static Step Headings(IProjectionDefinition[] captions) => new Step(StepKind.Headings, captions);

    /// <summary>
    /// Writes this step's offset/area/bound operation onto <paramref name="projection"/>'s
    /// <see cref="Placement"/>.
    /// </summary>
    internal IProjectionDefinition<TSpace, T> ApplyTo<TSpace, T>(IProjectionDefinition<TSpace, T> projection)
      where TSpace : class, ISpace
    {
      return _kind switch
      {
        // Every offset kind composes onto an earlier pipeline offset, or starts from the origin.
        StepKind.OnRow => Offset(projection, OffsetStrategies.To((IRowLandmark)_subject!)),
        StepKind.OnColumn => Offset(projection, OffsetStrategies.To((IColumnLandmark)_subject!)),
        StepKind.Below => Offset(projection, OffsetStrategies.Past((IRowLandmark)_subject!)),
        StepKind.RightOf => Offset(projection, OffsetStrategies.Past((IColumnLandmark)_subject!)),
        StepKind.OffsetBy => Offset(projection, (IOffsetStrategy)_subject!),
        StepKind.Down => Offset(projection, OffsetStrategies.ExplicitOffset(0, _count)),
        StepKind.Right => Offset(projection, OffsetStrategies.ExplicitOffset(_count, 0)),
        StepKind.AfterBlankRows => Offset(projection, OffsetStrategies.SkipBlankRows()),
        StepKind.AfterBlankColumns => Offset(projection, OffsetStrategies.SkipBlankColumns()),
        StepKind.SkipToFirstNonBlankCell => Offset(projection, OffsetStrategies.SkipToFirstNonBlankCell()),

        // An extent replaces the projection's derived one.
        StepKind.Sized => projection.With(projection.Annotations.WithPlacement(projection.Placement.WithArea((IAreaStrategy)_subject!))),

        // Bounds and headings wrap rather than reposition.
        StepKind.UntilRow => Bounded(projection, Landmark.Of((IRowLandmark)_subject!), _orEnd),
        StepKind.UntilColumn => Bounded(projection, Landmark.Of((IColumnLandmark)_subject!), _orEnd),
        StepKind.Headings => Headed(projection, (IProjectionDefinition[])_subject!),

        _ => throw new InvalidOperationException($"Unknown placement step {_kind}."),
      };
    }

    /// <summary>
    /// Refuses a second end rather than replacing the first, so <c>Until(A).Until(B)</c> is not a
    /// declaration at all: a projection has one end, and the axis comes with the landmark, so a
    /// column bound over a row bound is a second end too. A wrapper in between makes the outer bound
    /// nest, which is a different declaration and a legal one.
    /// </summary>
    private static IProjectionDefinition<TSpace, T> Bounded<TSpace, T>(IProjectionDefinition<TSpace, T> projection, Landmark landmark, bool orEnd)
      where TSpace : class, ISpace
    {
      if (projection is BoundedDefinition<TSpace, T> bounded)
        throw bounded.AlreadyEnded(landmark);

      return new BoundedDefinition<TSpace, T>(projection, landmark, orEnd, Placement.Default);
    }

    /// <summary>
    /// The projection below <paramref name="captions"/>, as one vertical flow — the structure a
    /// <c>Heading</c> stage builds. The captions are the heading rows, read and discarded; the
    /// projection is the section they announce.
    /// </summary>
    private static IProjectionDefinition<TSpace, T> Headed<TSpace, T>(IProjectionDefinition<TSpace, T> projection, IProjectionDefinition[] captions)
      where TSpace : class, ISpace
      => new FlowDefinition<TSpace, T>(
        Orientation.Vertical,
        LayoutBuilder<TSpace>.Declare<T>(
          cursor =>
          {
            // declared: null at both sites, and it is mandatory. Left to the compiler, the naming
            // ladder would read the argument text from inside HERE and label every caption 'caption'
            // and the section 'projection' — identifiers the user never wrote. Capture reads the
            // immediate call site, so a helper has to opt out.
            foreach (var caption in captions)
              cursor.Next((IProjectionDefinition<TSpace, string>)caption, declared: null);

            var section = cursor.Next(projection, declared: null);

            return cursor.Build(read => read.Of(section));
          },
          "a flow",
          nameof(projection)),
        Placement.Default,
        description: "Heading");

    /// <summary>
    /// The one offset rule for every offset step: <paramref name="offset"/> composes onto an offset an
    /// earlier pipeline stage declared (placement both <see cref="Placement.OffsetWasDeclared"/> and
    /// <see cref="Placement.HasDeclaredOffset"/>), otherwise starts from the origin.
    /// </summary>
    private static IProjectionDefinition<TSpace, T> Offset<TSpace, T>(IProjectionDefinition<TSpace, T> projection, IOffsetStrategy offset)
      where TSpace : class, ISpace
    {
      var placement = projection.Placement;
      var composeOntoBase = placement.OffsetWasDeclared && placement.HasDeclaredOffset;
      var composed = composeOntoBase ? OffsetStrategies.Then(placement.Offset, offset) : offset;

      return projection.With(projection.Annotations.WithPlacement(placement.WithOffset(composed)));
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
      IProjectionDefinition[] captions => string.Join(", ", Array.ConvertAll(captions, caption => caption.Description)),
      null => "?",
      _ => subject.GetType().Name,
    };

    private static T NotNull<T>(T value, string parameter = "landmark") where T : class
      => value ?? throw new ArgumentNullException(parameter);

    private static int NotNegative(int distance, string parameter)
      => distance >= 0 ? distance : throw new ArgumentOutOfRangeException(parameter, distance, "A projection cannot be inset or moved a negative distance.");
  }
}
