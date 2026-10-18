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
  public sealed class ReadDefinition<TSpace, TResult> : CollectorNode<TSpace, TResult>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A one-cell leaf that reads the cell through <paramref name="read"/> and fails with the read's own sentence, under the name <paramref name="kind"/>.
    /// <para>Fluent: <c>Point().Decimal()</c>.</para>
    /// </summary>
    /// <param name="kind">The path segment and the word in the failure sentence, such as <c>Decimal</c>.</param>
    /// <param name="read">The reading: the value, or the <see cref="CellProblem"/> saying why not.</param>
    /// <param name="placement">Where the cell is.</param>
    /// <param name="blankIsNull">Whether a blank cell yields <c>default</c> rather than failing; the <c>OrBlank</c> forms set it.</param>
    public ReadDefinition(string kind, CellRead<TSpace, TResult> read, Placement<TSpace> placement, bool blankIsNull)
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

    /// <inheritdoc/>
    public override string Description => BlankIsNull ? Kind + "?" : Kind;

    internal override Settlement<TResult> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      var size = extent.Extent;

      if (size.Width != 1 || size.Height != 1)
        throw scope.Failure($"{Article(Kind)} {Kind} must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

      var cell = extent[0, 0];

      if (BlankIsNull && cell.IsBlank())
        return new Settlement<TResult>(default!, size);

      if (!Read(cell, out var value, out var problem))
        throw scope.Failure(problem!.Value.Render(scope.Locate(extent).A1), extent);

      return new Settlement<TResult>(value, size);
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
    public IProjectionDefinition<TSpace, TValue> Tolerating<TValue>(Func<TResult, TValue> widen)
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
