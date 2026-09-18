using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Reads one cell as a value: what a cell leaf is made of. True with the value when the cell
  /// could be read as the kind the leaf asserts; false with a <see cref="CellProblem"/> — a
  /// sentence awaiting the cell's address — when it could not.
  /// </summary>
  /// <typeparam name="TSpace">The space the cell belongs to.</typeparam>
  /// <typeparam name="TValue">What the reading produces.</typeparam>
  /// <param name="cell">The cell to read.</param>
  /// <param name="value">What the cell holds, when the read succeeded.</param>
  /// <param name="problem">Why it did not, when it did not.</param>
  public delegate bool CellRead<TSpace, TValue>(Point<TSpace> cell, out TValue value, out CellProblem? problem)
    where TSpace : class, ISpace;

  /// <summary>
  /// One cell read as a named kind: the leaf every cell reading is — <c>AsText()</c>, and the six
  /// kinded leaves a backend ships. The kind is data and the read is a function of one cell, so a
  /// backend that adds a leaf varies a string and a delegate rather than copying a node: the
  /// one-cell check, blank tolerance and the located failure are the same for every kind, which is
  /// what keeps a <c>Decimal()</c> leaf and a bound <c>decimal</c> column describing a bad cell
  /// identically.
  /// </summary>
  /// <typeparam name="TSpace">The space the leaf is declared over.</typeparam>
  /// <typeparam name="TResult">What the leaf hands back.</typeparam>
  internal sealed class ReadDefinition<TSpace, TResult> : DefinitionNode<TSpace, TResult>
    where TSpace : class, ISpace
  {
    internal ReadDefinition(string kind, CellRead<TSpace, TResult> read, Placement placement, bool blankIsNull)
      : base(placement)
    {
      Kind = kind ?? throw new ArgumentNullException(nameof(kind));
      Read = read ?? throw new ArgumentNullException(nameof(read));
      BlankIsNull = blankIsNull;
    }

    /// <summary>What the declaration asserts the cell is — <c>"Decimal"</c>, <c>"Date"</c>, <c>"AsText"</c>. Rendered in a path, and what a writer would emit.</summary>
    public string Kind { get; }

    /// <summary>The reading of one cell.</summary>
    public CellRead<TSpace, TResult> Read { get; }

    /// <summary>
    /// Whether a blank cell reads as null instead of failing — what <c>OrBlank</c> declares, and
    /// quietly: the declaration said this cell may be absent, so its absence is the answer rather
    /// than something to report. It tolerates a blank and nothing else: a cell of the wrong kind
    /// still fails exactly as loudly, because a missing value says something about the data and a
    /// wrong kind says something about the format.
    /// </summary>
    public bool BlankIsNull { get; }

    public override string Description => BlankIsNull ? Kind + "?" : Kind;

    public override IProjector<TSpace, TResult> Start(ProjectorScope<TSpace> scope)
      => new SpanCountProjector<TSpace, TResult>(this, scope, 1);

    public override ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var size = extent.Area.Size;

      if (size.Width != 1 || size.Height != 1)
        throw context.Failure($"{Article(Kind)} {Kind} must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

      var cell = extent[0, 0];

      if (BlankIsNull && cell.IsBlank)
        return new ProjectionResult<TResult>(default!, size);

      if (!Read(cell, out var value, out var problem))
        throw context.Failure(problem!(context.Locate(extent).A1), extent);

      return new ProjectionResult<TResult>(value, size);
    }

    /// <summary>
    /// The same reading, tolerating a blank cell — what <c>OrBlank</c> declares. The widening comes
    /// from the caller because C# cannot say "this same reading, of <typeparamref name="TValue"/>";
    /// at run time it is the identity. Everything else — the placement, the name, the kind — is
    /// the receiver's own, so <c>Decimal().Right(6).OrBlank()</c> and
    /// <c>Decimal().OrBlank().Right(6)</c> declare the same thing.
    /// </summary>
    /// <typeparam name="TValue">The nullable form of <typeparamref name="TResult"/>.</typeparam>
    /// <param name="widen">The widening, which is the identity conversion at run time.</param>
    internal IProjectionDefinition<TSpace, TValue> Tolerating<TValue>(Func<TResult, TValue> widen)
    {
      var read = Read;

      bool Widened(Point<TSpace> cell, out TValue value, out CellProblem? problem)
      {
        if (!read(cell, out var raw, out problem))
        {
          value = default!;
          return false;
        }

        value = widen(raw);
        return true;
      }

      return (IProjectionDefinition<TSpace, TValue>)new ReadDefinition<TSpace, TValue>(Kind, Widened, Placement, blankIsNull: true).With(Annotations);
    }

    private static string Article(string kind)
      => kind.Length > 0 && "AEIOUaeiou".IndexOf(kind[0]) >= 0 ? "an" : "a";
  }
}
