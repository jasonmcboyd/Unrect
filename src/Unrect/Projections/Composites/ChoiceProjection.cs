using System;
using System.Collections.Generic;
using System.Text;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Alternatives tried in order against the same extent, first match winning. Each alternative
  /// that does not match leaves an <see cref="DiagnosticSeverity.Info"/> saying why, and anything
  /// it absorbed on its way to failing is rolled back — a branch that did not win says nothing
  /// beyond the one line explaining itself.
  /// </summary>
  internal sealed class ChoiceProjection<T> : ProjectionBase<T>
  {
    public ChoiceProjection(IReadOnlyList<IProjection<T>> alternatives, Placement placement)
      : base(placement)
    {
      var copy = new IProjection<T>[alternatives.Count];

      for (var index = 0; index < copy.Length; index++)
        // The factory validates its own parameters; this is the invariant behind it.
        copy[index] = alternatives[index] ?? throw new ArgumentException("A choice cannot contain a null projection.", nameof(alternatives));

      Alternatives = copy;
      Children = copy;
    }

    private IProjection<T>[] Alternatives { get; }

    public override string Description => "Choice";

    public override IReadOnlyList<IProjection> Children { get; }

    public override ProjectionResult<T> Project(ISpace extent, ProjectionContext context)
    {
      ProjectionException[]? failures = null;

      for (var index = 0; index < Alternatives.Length; index++)
      {
        var mark = context.Diagnostics.Mark();

        try
        {
          var applied = ProjectionEngine.Apply(Alternatives[index], extent, context);
          return new ProjectionResult<T>(applied.Value, applied.Advance);
        }
        // A projection that broke rather than disagreed is a bug in the reading code; trying the
        // next alternative would only bury it.
        catch (ProjectionException failure) when (!failure.IsFault)
        {
          // Whatever this branch tolerated on its way to failing goes with it.
          context.Diagnostics.Rollback(mark);
          context.Report(
            DiagnosticSeverity.Info,
            failure,
            ProjectionContext.Describe(this),
            $"alternative {index + 1} ({ProjectionContext.DescribeThrough(Alternatives[index])}) did not match: {failure.Problem}");

          failures ??= new ProjectionException[Alternatives.Length];
          failures[index] = failure;
        }
      }

      throw context.Failure(this, Summarise(failures!), extent, null, failures![failures.Length - 1]);
    }

    private string Summarise(ProjectionException[] failures)
    {
      var summary = new StringBuilder("no alternative matched");

      for (var index = 0; index < failures.Length; index++)
        summary
          .Append(Environment.NewLine)
          .Append($"    alternative {index + 1} ({ProjectionContext.DescribeThrough(Alternatives[index])}): ")
          .Append(failures[index].Problem)
          .Append($" at {failures[index].Location}");

      return summary.ToString();
    }
  }
}
