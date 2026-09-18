using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The machine every leaf and every view is: take exactly <c>count</c> spans, refuse the rest, and
  /// at <c>Close</c> read the region they cover the way the node has always read an extent. A
  /// one-cell leaf takes one; a block lambda over its whole extent, held and re-driven as one span,
  /// takes one; a header takes its header rows; <c>Nothing</c> takes none.
  /// </summary>
  internal sealed class SpanCountProjector<TSpace, T> : IProjector<TSpace, T>
    where TSpace : class, ISpace
  {
    private readonly DefinitionNode<TSpace, T> _definition;
    private readonly ProjectorScope<TSpace> _scope;
    private readonly int _count;
    private Plane<TSpace>? _first;
    private int _taken;
    private bool _closed;

    internal SpanCountProjector(DefinitionNode<TSpace, T> definition, ProjectorScope<TSpace> scope, int count)
    {
      _definition = definition;
      _scope = scope;
      _count = count;
    }

    public bool Next(Plane<TSpace> span)
    {
      if (_closed)
        throw _scope.Context.Failure(_definition, $"{ProjectionContext.Describe(_definition)} was fed a span after it was closed", span, null, null, isFault: true);

      if (_taken >= _count)
        return false;

      _first ??= span;
      _taken++;
      return true;
    }

    public Settlement<T> Close()
    {
      _closed = true;

      var extent = _first is Plane<TSpace> first
        ? Spans.Region(first, _taken, _scope.Driver)
        : Spans.Empty(_scope.Anchor, _scope.Driver);

      // A whole region re-driven as one span arrives as that region, not as one row of it.
      if (_first is Plane<TSpace> whole && _taken == 1 && !_definition.Axis.Streams(_scope.Driver))
        extent = whole;

      var result = _definition.Project(extent, _scope.Context);

      return new Settlement<T>(result.Value, result.Consumed, result.Presence);
    }
  }
}
