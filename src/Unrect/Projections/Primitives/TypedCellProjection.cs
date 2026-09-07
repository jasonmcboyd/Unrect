using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Reads a cell whose kind has already been asserted. False means the value is of the right kind
  /// but does not fit the CLR type the declaration asked for, and <paramref name="conversion"/>
  /// says so in the reader's vocabulary rather than the document's.
  /// <para>
  /// <paramref name="at"/> is a thunk rather than a string because it is only ever needed on the
  /// failing path: a table binds one per cell, and formatting an A1 address for every cell of a
  /// large sheet would allocate tens of thousands of strings nobody reads.
  /// </para>
  /// </summary>
  internal delegate bool CellReader<T>(CellValue cell, Func<string> at, out T value, out string? conversion);

  /// <summary>
  /// One cell, of a declared kind, read by the one canonical accessor for that kind. The kind is
  /// declaration data here rather than a lambda body, which is what lets a failure name the cell
  /// and what a writer would need to emit it.
  /// </summary>
  internal sealed class TypedCellProjection<T> : ProjectionBase<T>
  {
    public TypedCellProjection(CellKind kind, string description, CellReader<T> read, Placement placement, bool blankIsNull)
      : base(placement)
    {
      Kind = kind;
      Description = description;
      Read = read;
      BlankIsNull = blankIsNull;
    }

    private CellKind Kind { get; }
    private CellReader<T> Read { get; }

    /// <summary>
    /// Whether a blank cell reads as null instead of failing — what <c>OrBlank</c> declares. It
    /// tolerates a blank and nothing else: a cell of the wrong kind still fails exactly as loudly,
    /// because a missing value says something about the data and a wrong kind says something about
    /// the format.
    /// </summary>
    private bool BlankIsNull { get; }

    public override string Description { get; }

    public override ProjectionResult<T> Project(ISpace extent, ProjectionContext context)
    {
      var size = extent.Area.Size;

      if (size.Width != 1 || size.Height != 1)
        throw context.Failure($"a {Description} must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

      var cell = extent[0, 0];

      // Quietly: the declaration said this cell may be absent, so its absence is the answer rather
      // than something to report. That is the whole difference from Optional, which absorbs a
      // failure and says so with a Warning.
      if (BlankIsNull && cell.IsBlank)
        return new ProjectionResult<T>(default!, size);

      string At() => context.Locate(extent).A1;

      if (cell.Kind != Kind)
        throw context.Failure(CellReading.WrongKind(Kind, cell, At()), extent);

      if (!Read(cell, At, out var value, out var conversion))
        throw context.Failure(conversion!, extent);

      return new ProjectionResult<T>(value, size);
    }

    /// <summary>
    /// The same reading, tolerating a blank cell — <c>OrBlank</c>. The kind, the accessor and the
    /// placement are this leaf's own, so <c>Decimal().Right(6).OrBlank()</c> and
    /// <c>Decimal().OrBlank().Right(6)</c> declare the same thing; only the result type changes,
    /// which is why <paramref name="asNullable"/> comes from the caller — C# cannot say "this same
    /// reading, of <typeparamref name="TValue"/>" on its own.
    /// </summary>
    /// <typeparam name="TValue">The nullable form of <typeparamref name="T"/>.</typeparam>
    /// <param name="asNullable">The widening, which is the identity conversion at run time.</param>
    internal IProjection<TValue> Tolerating<TValue>(Func<T, TValue> asNullable)
    {
      bool Tolerant(CellValue cell, Func<string> at, out TValue value, out string? conversion)
      {
        if (!Read(cell, at, out var raw, out conversion))
        {
          value = default!;
          return false;
        }

        value = asNullable(raw);
        return true;
      }

      var tolerant = new TypedCellProjection<TValue>(Kind, Description + "?", Tolerant, Placement, blankIsNull: true);

      return Name is null ? tolerant : tolerant.WithName(Name);
    }
  }
}
