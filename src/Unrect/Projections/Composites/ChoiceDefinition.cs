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
  internal sealed class ChoiceDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public ChoiceDefinition(IReadOnlyList<IProjectionDefinition<TSpace, T>> alternatives, Placement placement)
      : base(placement)
    {
      var copy = new IProjectionDefinition<TSpace, T>[alternatives.Count];
      var children = new Child[alternatives.Count];

      for (var index = 0; index < copy.Length; index++)
      {
        // The factory validates its own parameters; this is the invariant behind it.
        copy[index] = alternatives[index] ?? throw new ArgumentException("A choice cannot contain a null projection.", nameof(alternatives));
        // A params array captures no per-argument text, so an alternative is known only by its position.
        children[index] = new Child(copy[index], UseSite.From(null, index + 1));
      }

      Alternatives = copy;
      Children = children;
    }

    private IProjectionDefinition<TSpace, T>[] Alternatives { get; }

    public override string Description => "Choice";

    public override IReadOnlyList<Child> Children { get; }

    public override Axes Axis
    {
      get
      {
        var axis = Axes.Either;

        foreach (var alternative in Alternatives)
          axis &= alternative.Axis;

        return axis.OrEither();
      }
    }

    /// <summary>A choice replays everything a losing alternative took into the next.</summary>
    public override Reach Reach => Reach.Extent;

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// Alternatives in declaration order, one at a time. An alternative that fails with a failure
    /// rather than a fault is abandoned: the diagnostics since the choice started roll back, the
    /// Info line naming it is recorded, and everything it took is fed to the next. The first to
    /// close without failing wins; none left is the summarising failure.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, T>
    {
      private readonly ChoiceDefinition<TSpace, T> _choice;
      private readonly ProjectorScope<TSpace> _scope;
      private readonly ProjectionException[] _failures;
      private int _index;
      private int _mark;
      private IChildHandle<TSpace, T>? _current;
      private Settlement<T>? _settled;
      private Plane<TSpace>? _first;
      private bool _finished;

      public Machine(ChoiceDefinition<TSpace, T> choice, ProjectorScope<TSpace> scope)
      {
        _choice = choice;
        _scope = scope;
        _failures = new ProjectionException[choice.Alternatives.Length];
      }

      public bool Next(Plane<TSpace> span)
      {
        _first ??= span;

        return Offer(span);
      }

      public Settlement<T> Close()
      {
        if (_settled is Settlement<T> settled)
          return settled;

        while (true)
        {
          _current ??= StartAlternative(_scope.Anchor);

          try
          {
            var settlement = _current.Close();
            return new Settlement<T>(settlement.Value, _current.Advance, _current.Presence);
          }
          catch (ProjectionException failure) when (!failure.IsFault)
          {
            var taken = Abandon(failure);

            foreach (var span in taken)
              if (!Offer(span))
                return _settled!.Value;
          }
        }
      }

      /// <summary>
      /// Offers a span to the current alternative. A refusal settles it at once: closed cleanly, it
      /// has won; failed, it is abandoned and the span in hand goes to the next alternative after
      /// everything the loser took.
      /// </summary>
      private bool Offer(Plane<TSpace> span)
      {
        if (_finished)
          return false;

        _current ??= StartAlternative(Spans.Empty(span, _scope.Driver));

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
          catch (ProjectionException failure) when (!failure.IsFault)
          {
            var taken = Abandon(failure);

            foreach (var replayed in taken)
              if (!Offer(replayed))
                return false;
          }
        }
      }

      /// <summary>Rolls back to the attempt's mark, records the Info, and moves to the next alternative — or throws the summary when there is none.</summary>
      private List<Plane<TSpace>> Abandon(ProjectionException failure)
      {
        _scope.Diagnostics.Rollback(_mark);
        _scope.Report(
          DiagnosticSeverity.Info,
          failure,
          PathRenderer.Describe(_choice),
          $"alternative {_index + 1} ({PathRenderer.DescribeThrough(_choice.Alternatives[_index])}) did not match: {failure.Problem}");
        _failures[_index] = failure;

        var taken = new List<Plane<TSpace>>(_current!.Shortfall());
        _current = null;
        _index++;

        if (_index == _choice.Alternatives.Length)
        {
          var extent = _first is Plane<TSpace> first ? Spans.Region(first, taken.Count, _scope.Driver) : _scope.Anchor;
          throw _scope.Failure(_choice, _choice.Summarise(_failures), extent, null, _failures[_failures.Length - 1]);
        }

        _current = StartAlternative(_first is Plane<TSpace> at ? Spans.Empty(at, _scope.Driver) : _scope.Anchor);
        return taken;
      }

      private IChildHandle<TSpace, T> StartAlternative(Plane<TSpace> at)
      {
        _mark = _scope.Diagnostics.Mark();
        return _scope.Start(_choice.Children[_index], _choice.Alternatives[_index], at, inheritSite: true);
      }
    }

    internal override Reach Retains => Reach.Extent;

    private static readonly string[] LineBreaks = { "\r\n", "\n" };

    private const string Indent = "    ";

    /// <summary>
    /// One indented line per alternative: what it was, what it made of the space, and where. A
    /// problem that arrives in more than one line — a nested choice's tally, most often, though a
    /// quoted foreign exception can do it too — has its continuation lines pushed one level deeper,
    /// and this level's location stays on the line that names the alternative — otherwise the
    /// nested block reads as this one's and two locations end up back to back at the bottom of it.
    /// A single-line problem is the same sentence either way.
    /// </summary>
    private string Summarise(ProjectionException[] failures)
    {
      var summary = new StringBuilder("no alternative matched");

      for (var index = 0; index < failures.Length; index++)
      {
        var problem = failures[index].Problem.Split(LineBreaks, StringSplitOptions.None);

        summary
          .Append(Environment.NewLine)
          .Append(Indent)
          .Append($"alternative {index + 1} ({PathRenderer.DescribeThrough(Alternatives[index])}): ")
          .Append(problem[0])
          .Append($" at {failures[index].Location}");

        for (var line = 1; line < problem.Length; line++)
          summary.Append(Environment.NewLine).Append(Indent).Append(problem[line]);
      }

      return summary.ToString();
    }
  }
}
