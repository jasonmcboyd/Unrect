using System;
using System.Collections.Generic;

namespace Unrect.Shapes
{
  /// <summary>
  /// The diagnostics gathered during one <c>Map</c> call. A choice takes a mark before each
  /// attempt and rolls back after a failed one, so a branch that did not win leaves nothing behind.
  /// The list is created on first use: a decomposition with no boundary and no choice never
  /// allocates one.
  /// </summary>
  internal sealed class DiagnosticCollector
  {
    private List<ShapeDiagnostic>? _diagnostics;

    public int Mark() => _diagnostics?.Count ?? 0;

    public void Rollback(int mark)
    {
      if (_diagnostics is not null && _diagnostics.Count > mark)
        _diagnostics.RemoveRange(mark, _diagnostics.Count - mark);
    }

    public void Add(ShapeDiagnostic diagnostic) => (_diagnostics ??= new List<ShapeDiagnostic>()).Add(diagnostic);

    /// <summary>
    /// Whether everything recorded since <paramref name="mark"/> is one absorbed failure — a shape
    /// that failed, was tolerated, and produced nothing else to say.
    /// </summary>
    public bool AbsorbedAt(int mark)
      => _diagnostics is not null
      && _diagnostics.Count == mark + 1
      && _diagnostics[mark].Severity == DiagnosticSeverity.Warning;

    /// <summary>A copy, so what a caller reads can never change underneath it.</summary>
    public IReadOnlyList<ShapeDiagnostic> Snapshot()
      => _diagnostics is null ? Array.Empty<ShapeDiagnostic>() : _diagnostics.ToArray();
  }
}
