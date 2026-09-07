using System;
using System.Collections.Generic;

namespace Unrect.Projections
{
  /// <summary>
  /// What a decomposition produced together with what it noticed on the way. The diagnostics are a
  /// snapshot: reading them cannot disturb a parse, and a parse cannot disturb them.
  /// </summary>
  public readonly struct MapResult<T>
  {
    private readonly IReadOnlyList<ProjectionDiagnostic>? _diagnostics;

    internal MapResult(T value, IReadOnlyList<ProjectionDiagnostic> diagnostics)
    {
      Value = value;
      _diagnostics = diagnostics;
    }

    /// <summary>What the projection projected.</summary>
    public T Value { get; }

    /// <summary>
    /// What the decomposition noticed along the way — near-misses a <c>Choice</c> tried, tolerance
    /// a boundary absorbed, space nothing described. Empty, never null, when nothing was recorded.
    /// </summary>
    public IReadOnlyList<ProjectionDiagnostic> Diagnostics => _diagnostics ?? Array.Empty<ProjectionDiagnostic>();
  }
}
