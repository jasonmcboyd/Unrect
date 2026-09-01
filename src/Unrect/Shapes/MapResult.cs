using System.Collections.Generic;

namespace Unrect.Shapes
{
  /// <summary>
  /// What a decomposition produced together with what it noticed on the way. The diagnostics are a
  /// snapshot: reading them cannot disturb a parse, and a parse cannot disturb them.
  /// </summary>
  public readonly struct MapResult<T>
  {
    private readonly IReadOnlyList<ShapeDiagnostic>? _diagnostics;

    internal MapResult(T value, IReadOnlyList<ShapeDiagnostic> diagnostics)
    {
      Value = value;
      _diagnostics = diagnostics;
    }

    public T Value { get; }

    public IReadOnlyList<ShapeDiagnostic> Diagnostics => _diagnostics ?? System.Array.Empty<ShapeDiagnostic>();
  }
}
