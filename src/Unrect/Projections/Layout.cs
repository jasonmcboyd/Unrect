using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Declares a layout and assembles its result: each <see cref="LayoutCursor{TSpace}.Next{T}"/>
  /// declares the next child, in the order they appear on the sheet, and hands back what it read,
  /// and the lambda returns the result built from those readings. It runs twice — once when the
  /// layout is declared, with each child reading as the empty value of its type, to learn the
  /// children, and once per application, with the values, to assemble them — so it may assemble,
  /// count, join and format, but may not read a cell or reach into an object nothing has built:
  /// that belongs in the leaf, or in a <c>Select</c> after the layout.
  /// </summary>
  /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
  /// <typeparam name="TResult">What the layout builds from what its children read.</typeparam>
  /// <param name="cursor">The cursor the layout declares its children with.</param>
  public delegate TResult LayoutDeclaration<TSpace, TResult>(LayoutCursor<TSpace> cursor)
    where TSpace : class, ISpace;

  /// <summary>
  /// A declared layout: its children in order, as one recording pass over the lambda found them,
  /// and the lambda itself, replayed over the children's values once they have all settled.
  /// </summary>
  /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
  /// <typeparam name="TResult">What the layout builds from what its children read.</typeparam>
  internal sealed class Layout<TSpace, TResult>
    where TSpace : class, ISpace
  {
    private Layout(LayoutDeclaration<TSpace, TResult> declaration, Child[] children, LayoutRunner<TSpace>[] runners)
    {
      Declaration = declaration;
      Children = children;
      Runners = runners;
    }

    private LayoutDeclaration<TSpace, TResult> Declaration { get; }

    internal IReadOnlyList<Child> Children { get; }

    internal IReadOnlyList<LayoutRunner<TSpace>> Runners { get; }

    /// <summary>
    /// Runs <paramref name="declare"/> once, now, with every child reading as the empty value of its
    /// type (<see cref="Empty{T}"/>), and keeps the children it declared. A lambda that throws here
    /// reached into what a child read — a cell of a point with no space, a member of an object
    /// nothing built — and is refused where it is written, naming the fix; one that declared
    /// nothing is refused too.
    /// </summary>
    internal static Layout<TSpace, TResult> Declare(LayoutDeclaration<TSpace, TResult> declare, string noun, string parameter)
    {
      if (declare is null)
        throw new ArgumentNullException(parameter);

      var recording = new RecordingPass<TSpace>(noun);

      try
      {
        declare(new LayoutCursor<TSpace>(recording));
      }
      catch (Exception exception) when (!LayoutPass<TSpace>.IsRefusal(exception))
      {
        throw new ArgumentException(
          "The layout lambda reaches into what its children read, and a layout has no values when it is "
          + "declared — the lambda runs once here to learn its children, with each reading as the empty value "
          + "of its type. Read a cell inside the leaf that names it, and compute on a child's value in Select. "
          + $"It threw {exception.GetType().Name}: {exception.Message}",
          parameter,
          exception);
      }

      return new Layout<TSpace, TResult>(declare, recording.Close(), recording.Runners.ToArray());
    }

    /// <summary>
    /// The result over <paramref name="values"/>, one per child in declaration order: the lambda
    /// run again with each <c>Next</c> handing back its child's value. The definition a replayed
    /// <c>Next</c> is handed is only compared, never run — the recorded one ran — and a lambda that
    /// asks for a child of another kind or name than it declared at that position, or for a
    /// different number of them, had a shape that depended on a value and throws
    /// <see cref="LayoutShapeException"/>, which the machine reports as a fault.
    /// </summary>
    internal TResult Combine(object?[] values)
    {
      var replaying = new ReplayingPass<TSpace>(Children, values);
      var result = Declaration(new LayoutCursor<TSpace>(replaying));

      replaying.Close();

      return result;
    }
  }

  /// <summary>One run of a layout lambda: what <c>Next</c> does on it.</summary>
  internal abstract class LayoutPass<TSpace>
    where TSpace : class, ISpace
  {
    private const string RefusalKey = "Unrect.LayoutRefusal";

    private const string Outside = "A layout cursor cannot be used outside the layout that created it";

    /// <summary>
    /// Being outside a layout covers two different bugs, so the messages name which one: a cursor
    /// that never had a layout (<c>default</c>), and one used after its layout was declared.
    /// </summary>
    internal const string NoLayout = Outside + "; this one never had a layout.";

    /// <inheritdoc cref="NoLayout"/>
    internal const string AlreadyDeclared = Outside + "; this one has already been declared.";

    internal abstract T Next<T>(IProjectionDefinition<TSpace, T> projection, string? declared);

    /// <summary>Whether <paramref name="exception"/> is one of this layer's own refusals rather than the lambda's.</summary>
    internal static bool IsRefusal(Exception exception) => exception.Data.Contains(RefusalKey);

    /// <summary>Marks <paramref name="refusal"/> as this layer's own, so a declaration-time catch lets it through as it is.</summary>
    protected static Exception Refused(Exception refusal)
    {
      refusal.Data[RefusalKey] = true;
      return refusal;
    }
  }

  /// <summary>
  /// What a child reads as on the declaration pass, when nothing has been read: the empty value of
  /// its type where the type has one — zero, the empty string, an empty list or dictionary — and
  /// <c>default</c> otherwise. So a lambda may count, join or format what its children read and
  /// still survive the pass; what it may not do is read a cell (a point with no space) or reach into
  /// an object that is null because nothing built it, which is where <c>Select</c> comes in.
  /// </summary>
  internal static class Empty<T>
  {
    internal static readonly T Value = Make();

    private static T Make()
    {
      var type = typeof(T);

      if (type == typeof(string))
        return (T)(object)string.Empty;

      if (type.IsArray && type.GetArrayRank() == 1)
        return (T)(object)Array.CreateInstance(type.GetElementType()!, 0);

      if (type.IsGenericType)
      {
        var definition = type.GetGenericTypeDefinition();
        var arguments = type.GetGenericArguments();

        if (definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IReadOnlyList<>)
          || definition == typeof(ICollection<>) || definition == typeof(IList<>))
          return (T)(object)Array.CreateInstance(arguments[0], 0);

        if (definition == typeof(List<>) || definition == typeof(Dictionary<,>) || definition == typeof(HashSet<>))
          return (T)Activator.CreateInstance(type)!;

        if (definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IDictionary<,>))
          return (T)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(arguments))!;
      }

      return default!;
    }
  }

  /// <summary>The declaration-time pass: records each child and hands back the empty value of its type.</summary>
  internal sealed class RecordingPass<TSpace> : LayoutPass<TSpace>
    where TSpace : class, ISpace
  {
    private readonly List<Child> _children = new List<Child>();
    private readonly string _noun;
    private bool _done;

    internal RecordingPass(string noun) => _noun = noun;

    internal List<LayoutRunner<TSpace>> Runners { get; } = new List<LayoutRunner<TSpace>>();

    internal override T Next<T>(IProjectionDefinition<TSpace, T> projection, string? declared)
    {
      if (_done)
        throw Refused(new InvalidOperationException(AlreadyDeclared));

      var index = _children.Count;

      // A null projection is a hole in the declaration, refused where the declaration is written.
      if (projection is null)
        throw Refused(new ArgumentNullException(nameof(projection), $"a null projection was declared as child {index + 1}"));

      _children.Add(new Child(projection, UseSite.From(declared, index + 1)));
      Runners.Add(new LayoutRunner<TSpace, T>(projection));

      return Empty<T>.Value;
    }

    /// <summary>The children recorded, once the lambda has returned; a layout that declared none is refused.</summary>
    internal Child[] Close()
    {
      // A layout that declared nothing would match anything, describe nothing, and quietly end an
      // enclosing repetition by consuming nothing. That is a bug in the declaration, so it is
      // refused where the declaration is written.
      if (_children.Count == 0)
        throw Refused(new InvalidOperationException($"{_noun} must declare at least one projection; this one called Next zero times"));

      _done = true;

      return _children.ToArray();
    }
  }

  /// <summary>The per-application pass: hands each child's value back in the order the children were declared.</summary>
  internal sealed class ReplayingPass<TSpace> : LayoutPass<TSpace>
    where TSpace : class, ISpace
  {
    private readonly IReadOnlyList<Child> _children;
    private readonly object?[] _values;
    private int _index;

    internal ReplayingPass(IReadOnlyList<Child> children, object?[] values)
    {
      _children = children;
      _values = values;
    }

    internal override T Next<T>(IProjectionDefinition<TSpace, T> projection, string? declared)
    {
      if (_index >= _children.Count)
        throw new LayoutShapeException(
          $"the layout asked for a child {_index + 1} ({PathRenderer.Describe(projection)}) that it did not declare; "
          + "it declared " + Count(_children.Count) + ". " + Rule);

      // What the child IS, not which object it is: a lambda written with inline factories builds a
      // fresh definition on every run, and the recorded one is the one the engine ran.
      var recorded = _children[_index].Definition;

      if (projection is null || projection.GetType() != recorded.GetType() || projection.Description != recorded.Description || projection.Name != recorded.Name)
        throw new LayoutShapeException(
          $"the layout asked for {(projection is null ? "a null projection" : PathRenderer.Describe(projection))} as child {_index + 1}, where it declared "
          + $"{PathRenderer.Describe(recorded)}. " + Rule);

      return (T)_values[_index++]!;
    }

    /// <summary>Every declared child must have been asked for, or the lambda's shape changed.</summary>
    internal void Close()
    {
      if (_index < _children.Count)
        throw new LayoutShapeException(
          $"the layout asked for {Count(_index)} where it declared {Count(_children.Count)}. " + Rule);
    }

    private const string Rule =
      "A layout's shape may not depend on a value it read: the lambda runs once at declaration to learn its "
      + "children and once per application to assemble them, and the two must agree. Declare the alternatives "
      + "with Choice, Else or Optional";

    private static string Count(int children) => children == 1 ? "1 child" : $"{children} children";
  }

  /// <summary>A layout lambda that declared one set of children and asked for another when replayed — a bug in the declaration, reported as a fault.</summary>
  internal sealed class LayoutShapeException : InvalidOperationException
  {
    internal LayoutShapeException(string message)
      : base(message)
    {
    }
  }

  /// <summary>
  /// One child of a layout, applied with its result type known — what lets a flow's state take a
  /// child generically from an edge that holds it non-generically.
  /// </summary>
  internal abstract class LayoutRunner<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>Starts the child under the push engine, behind its placement machine, at <paramref name="edge"/>.</summary>
    public abstract IChildHandle<TSpace> Start(ProjectorScope<TSpace> scope, Child edge, Plane<TSpace> anchor);
  }

  internal sealed class LayoutRunner<TSpace, T> : LayoutRunner<TSpace>
    where TSpace : class, ISpace
  {
    public LayoutRunner(IProjectionDefinition<TSpace, T> projection) => Projection = projection;

    private IProjectionDefinition<TSpace, T> Projection { get; }

    public override IChildHandle<TSpace> Start(ProjectorScope<TSpace> scope, Child edge, Plane<TSpace> anchor)
      => scope.Start(edge, Projection, anchor);
  }
}
