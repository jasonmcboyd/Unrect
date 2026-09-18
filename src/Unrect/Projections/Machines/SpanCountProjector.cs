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
        throw _scope.Failure(_definition, $"{PathRenderer.Describe(_definition)} was fed a span after it was closed", span, null, null, isFault: true);

      // Under a declared area the placement machine bounds the spans and the node reads the whole
      // area — so a leaf forced to two rows still sees two rows and says so. A derived placement
      // is the node's own to bound.
      if (_definition.Placement.Area is null && _taken >= _count)
        return false;

      _first ??= span;
      _taken++;
      return true;
    }

    public Settlement<T> Close()
    {
      _closed = true;

      Plane<TSpace> extent;

      if (_first is not Plane<TSpace> first)
        extent = Spans.Empty(_scope.Anchor, _scope.Driver);
      // A whole region re-driven as one span arrives as that region, not as one row of it.
      else if (_taken == 1 && !_definition.Axis.Streams(_scope.Driver))
        extent = first;
      else
        extent = Spans.Region(first, _taken, _scope.Driver);

      return _definition.Collect(extent, _scope);
    }
  }
}
