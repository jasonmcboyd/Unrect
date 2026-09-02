using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// Reads a cell whose kind has already been asserted. False means the value is of the right kind
  /// but does not fit the CLR type the declaration asked for, and <paramref name="conversion"/> says
  /// so in the reader's vocabulary rather than the document's.
  /// <para>
  /// <paramref name="at"/> is a thunk rather than a string because it is only ever needed on the
  /// failing path: a table binds one per cell, and formatting an A1 address for every cell of a
  /// large sheet would allocate tens of thousands of strings nobody reads.
  /// </para>
  /// </summary>
  internal delegate bool CellReader<T>(CellValue cell, Func<string> at, out T value, out string? conversion);

  /// <summary>
  /// One cell, of a declared kind, read by the one canonical accessor for that kind. The kind is
  /// declaration data here rather than a lambda body, which is what lets a failure name the cell and
  /// what a writer would need to emit it.
  /// </summary>
  internal sealed class TypedCellShape<T> : ShapeBase<T>
  {
    public TypedCellShape(CellKind kind, string description, CellReader<T> read, Placement placement)
      : base(placement)
    {
      Kind = kind;
      Description = description;
      Read = read;
    }

    private CellKind Kind { get; }
    private CellReader<T> Read { get; }

    public override string Description { get; }

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      var size = extent.Area.Size;

      if (size.Width != 1 || size.Height != 1)
        throw context.Failure($"a {Description} must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

      var cell = extent[0, 0];

      string At() => context.Locate(extent).A1;

      if (cell.Kind != Kind)
        throw context.Failure(CellReading.WrongKind(Kind, cell, At()), extent);

      if (!Read(cell, At, out var value, out var conversion))
        throw context.Failure(conversion!, extent);

      return new ShapeResult<T>(value, size);
    }
  }
}
