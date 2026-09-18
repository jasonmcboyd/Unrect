using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A tolerance boundary: it applies the projection it wraps and, if anything inside fails,
  /// absorbs the failure, records a <see cref="DiagnosticSeverity.Warning"/> describing what
  /// actually went wrong, and supplies a filler — either a fallback projection or a constant value.
  /// <para>
  /// It behaves like a catch block. Its own placement is resolved before it can catch anything, so
  /// tolerance goes innermost: <c>x.On(anchor).Optional()</c> absorbs a missing anchor, while
  /// <c>x.Optional().On(anchor)</c> does not.
  /// </para>
  /// </summary>
  internal sealed class FallbackDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public FallbackDefinition(
      IProjectionDefinition<TSpace, T> inner,
      IProjectionDefinition<TSpace, T>? fallback,
      T fallbackValue,
      Placement placement,
      string description,
      UseSite fallbackSite = default)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Fallback = fallback;
      FallbackValue = fallbackValue;
      Description = description;
      FallbackSite = fallbackSite;
      Children = fallback is null ? new[] { new Child(inner, default) } : new[] { new Child(inner, default), new Child(fallback, fallbackSite) };
    }

    private IProjectionDefinition<TSpace, T> Inner { get; }
    private IProjectionDefinition<TSpace, T>? Fallback { get; }
    private T FallbackValue { get; }

    /// <summary>What the declaration called the fallback, so a stand-in names itself as written.</summary>
    private UseSite FallbackSite { get; }

    public override string Description { get; }

    public override IReadOnlyList<Child> Children { get; }

    public override bool IsWrapper => true;

    public override Axes Axis => (Fallback is null ? Inner.Axis : Inner.Axis & Fallback.Axis).OrEither();

    /// <summary>A boundary may hand back everything the inner took: what it absorbs, it did not consume.</summary>
    public override Reach Reach => Reach.Extent;

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// Forwards to the inner. When the inner fails with a failure rather than a fault, the
    /// diagnostics recorded since the boundary started roll back, the one Warning that replaces them
    /// is recorded, and either the fallback is started and fed everything the inner took, or the
    /// boundary settles on nothing with <see cref="Presence.Absorbed"/>.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, T>
    {
      private readonly FallbackDefinition<TSpace, T> _boundary;
      private readonly ProjectorScope<TSpace> _scope;
      private readonly int _mark;
      private IChildHandle<TSpace, T>? _current;
      private Settlement<T>? _settled;
      private bool _fallingBack;
      private bool _absorbed;
      private ProjectionException? _primary;
      private Plane<TSpace>? _first;
      private bool _finished;
      private bool _closed;

      public Machine(FallbackDefinition<TSpace, T> boundary, ProjectorScope<TSpace> scope)
      {
        _boundary = boundary;
        _scope = scope;
        _mark = scope.Diagnostics.Mark();
      }

      public bool Next(Plane<TSpace> span)
      {
        if (_closed)
          throw _scope.Failure(_boundary, $"{PathRenderer.Describe(_boundary)} was fed a span after it was closed", span, null, null, isFault: true);

        _first ??= span;

        return Offer(span);
      }

      public Settlement<T> Close()
      {
        _closed = true;

        if (_absorbed)
          return Absorbed();

        if (_settled is Settlement<T> settled)
          return settled;

        _current ??= StartInner(_scope.Anchor);

        while (true)
        {
          try
          {
            var settlement = _current.Close();
            return new Settlement<T>(settlement.Value, _current.Advance, _current.Presence);
          }
          catch (ProjectionException failure) when (!_fallingBack && !failure.IsFault)
          {
            var taken = Absorb(failure);

            if (_absorbed)
              return Absorbed();

            foreach (var span in taken)
              if (!Offer(span))
                return _settled!.Value;
          }
          catch (ProjectionException fallbackFailure) when (_fallingBack)
          {
            throw StandingIn(fallbackFailure);
          }
        }
      }

      /// <summary>
      /// Offers a span to whichever of the two is open. A refusal settles that projection at once:
      /// a primary that closes cleanly has won, and one that fails is absorbed here — with the span
      /// in hand still to be offered to the fallback, which the primary never had a claim on.
      /// </summary>
      private bool Offer(Plane<TSpace> span)
      {
        if (_finished)
          return false;

        _current ??= StartInner(Spans.Empty(span, _scope.Driver));

        while (true)
        {
          try
          {
            if (_current.Next(span))
              return true;

            var settlement = _current.Close();
            _settled = new Settlement<T>(settlement.Value, _current.Advance, _current.Presence);
            _finished = true;
            return false;
          }
          catch (ProjectionException failure) when (!_fallingBack && !failure.IsFault)
          {
            var taken = Absorb(failure);

            if (_absorbed)
            {
              _finished = true;
              return false;
            }

            foreach (var replayed in taken)
              if (!Offer(replayed))
                return false;
          }
          catch (ProjectionException fallbackFailure) when (_fallingBack)
          {
            throw StandingIn(fallbackFailure);
          }
        }
      }

      /// <summary>Rolls back, records the Warning, and either settles on nothing or starts the fallback — handing back what the inner took.</summary>
      private List<Plane<TSpace>> Absorb(ProjectionException failure)
      {
        _primary = failure;
        _scope.Diagnostics.Rollback(_mark);
        _scope.Report(DiagnosticSeverity.Warning, failure);

        var taken = new List<Plane<TSpace>>(_current!.Shortfall());

        if (_boundary.Fallback is null)
        {
          _absorbed = true;
          _current = null;
          return taken;
        }

        _fallingBack = true;
        _current = _scope.Start(_boundary.Children[1], _boundary.Fallback, _first is Plane<TSpace> first ? Spans.Empty(first, _scope.Driver) : _scope.Anchor);
        return taken;
      }

      private Settlement<T> Absorbed() => new Settlement<T>(_boundary.FallbackValue, new Size(0, 0), Presence.Absorbed);

      private ProjectionException StandingIn(ProjectionException fallbackFailure)
        => fallbackFailure.WithNote($"it stands in for {_primary!.Subject}, which failed too: {_primary.Problem}");

      private IChildHandle<TSpace, T> StartInner(Plane<TSpace> at)
        => _scope.Start(_boundary.Children[0], _boundary.Inner, at, inheritSite: true);
    }

    internal override Reach Retains => Reach.Extent;
  }
}
