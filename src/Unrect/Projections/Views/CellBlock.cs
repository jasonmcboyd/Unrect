using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A rectangular block of cells, addressable by coordinate, row, or column, and knowing where it
  /// sits so a caller can cite any of them. A cell is a <see cref="Point{TSpace}"/> — a place rather
  /// than a value — so reading one is whatever its space can answer.
  /// </summary>
  /// <typeparam name="TSpace">The space the block's cells belong to.</typeparam>
  public sealed class CellBlock<TSpace>
    where TSpace : class, ISpace
  {
    // Views are built per projection and are not covered by the projection thread-safety guarantee;
    // the caches race benignly (reference assignment is atomic, so the worst case is duplicated
    // work).
    private IReadOnlyList<CellStrip<TSpace>>? _rows;
    private IReadOnlyList<CellStrip<TSpace>>? _columns;

    internal CellBlock(Plane<TSpace> space, ProjectorScope<TSpace> scope)
    {
      Space = space;
      Scope = scope;
    }

    /// <summary>The block's own extent.</summary>
    public Plane<TSpace> Space { get; }

    /// <summary>
    /// The scope the block was projected in — where it sits, and the scope its rows and columns
    /// carry so a typed read of one reports a failure against the declaration that named the block.
    /// </summary>
    private ProjectorScope<TSpace> Scope { get; }

    /// <summary>How many columns wide the block is.</summary>
    public int Width => Space.Width;

    /// <summary>How many rows tall the block is.</summary>
    public int Height => Space.Area.Height;

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>; either index outside the
    /// block throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public Point<TSpace> this[int column, int row]
    {
      get
      {
        Validate(column, row);

        return Space[column, row];
      }
    }

    /// <summary>The address of the block's top-left cell, with the extent it was found in.</summary>
    public ProjectionLocation Location => ProjectionLocation.At(Space);

    /// <summary>The address of one cell of the block, for citing it in a message.</summary>
    public ProjectionLocation AddressOf(int column, int row)
    {
      Validate(column, row);

      return ProjectionLocation.At(Space.Origin + new Offset(column, row), Space.Area.Size);
    }

    /// <summary>The row at <paramref name="index"/>; an index outside the block throws <see cref="ArgumentOutOfRangeException"/>.</summary>
    public CellStrip<TSpace> Row(int index)
    {
      if (index < 0 || index >= Height)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The block is {Height} rows tall.");

      return new CellStrip<TSpace>(Space.Slice(new Offset(0, index), new Area(Width, 1)), Orientation.Horizontal, Scope);
    }

    /// <summary>The column at <paramref name="index"/>; an index outside the block throws <see cref="ArgumentOutOfRangeException"/>.</summary>
    public CellStrip<TSpace> Column(int index)
    {
      if (index < 0 || index >= Width)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The block is {Width} columns wide.");

      return new CellStrip<TSpace>(Space.Slice(new Offset(index, 0), new Area(1, Height)), Orientation.Vertical, Scope);
    }

    /// <summary>Every row, top to bottom, built once and cached.</summary>
    public IReadOnlyList<CellStrip<TSpace>> Rows => _rows ??= Build(Height, Row);

    /// <summary>Every column, left to right, built once and cached.</summary>
    public IReadOnlyList<CellStrip<TSpace>> Columns => _columns ??= Build(Width, Column);

    private void Validate(int column, int row)
    {
      if (column < 0 || column >= Width)
        throw new ArgumentOutOfRangeException(nameof(column), column, $"The block is {Width} columns wide.");

      if (row < 0 || row >= Height)
        throw new ArgumentOutOfRangeException(nameof(row), row, $"The block is {Height} rows tall.");
    }

    private static IReadOnlyList<CellStrip<TSpace>> Build(int count, Func<int, CellStrip<TSpace>> select)
    {
      var strips = new CellStrip<TSpace>[count];

      for (var index = 0; index < count; index++)
        strips[index] = select(index);

      return strips;
    }
  }
}
