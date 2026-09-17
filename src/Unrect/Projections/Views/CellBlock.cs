using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A rectangular block of cells, addressable by coordinate, row, or column, and knowing where it
  /// sits so a caller can cite any of them. A cell is a <see cref="Point{TSpace}"/> — a place rather
  /// than a value — so reading one is whatever its space can answer.
  /// <para>
  /// Over an extent whose height is discovered while it is read, the block obeys the same hybrid
  /// rule the extent does: <see cref="Width"/>, the indexer and <see cref="Row"/> ask for as much
  /// of the sheet as they name and no more, while <see cref="Height"/>, <see cref="Rows"/>,
  /// <see cref="Columns"/>, <see cref="Column"/>, <see cref="Location"/> and <see
  /// cref="AddressOf"/> are dimension queries and settle the bound. Each says so on its own
  /// summary.
  /// </para>
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

    internal CellBlock(Plane<TSpace> space, ProjectionContext context)
    {
      Space = space;
      Context = context;
    }

    /// <summary>The block's own extent.</summary>
    public Plane<TSpace> Space { get; }

    /// <summary>
    /// The context the block was projected in — where it sits, and the context its rows and columns
    /// carry so a typed read of one reports a failure against the declaration that named the block.
    /// </summary>
    private ProjectionContext Context { get; }

    /// <summary>
    /// How many columns wide the block is. Free on an extent still being discovered: a width is
    /// settled before the first row is read.
    /// </summary>
    public int Width => Space.Width;

    /// <summary>
    /// How many rows tall the block is. A dimension query, so on an extent still being discovered
    /// this reads the sheet through to wherever the declaration's rule stops.
    /// </summary>
    public int Height => Space.Area.Height;

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>; either index outside the
    /// block throws <see cref="ArgumentOutOfRangeException"/>. On an extent still being discovered
    /// asking for a cell reaches through <paramref name="row"/> and no further — the streaming way
    /// to read a block.
    /// </summary>
    public Point<TSpace> this[int column, int row]
    {
      get
      {
        Validate(column, row);

        return Space[column, row];
      }
    }

    /// <summary>
    /// The address of the block's top-left cell. Carries the extent it was found in, so on one
    /// still being discovered this settles the bound.
    /// </summary>
    public ProjectionLocation Location => ProjectionLocation.At(Space);

    /// <summary>
    /// The address of one cell of the block, for citing it in a message. Like <see
    /// cref="Location"/> it carries the extent the block was found in, so on one still being
    /// discovered this settles the bound — which costs nothing worth saving on the way to a
    /// message.
    /// </summary>
    public ProjectionLocation AddressOf(int column, int row)
    {
      Validate(column, row);

      return ProjectionLocation.At(Space.Origin + new Offset(column, row), Space.Area.Size);
    }

    /// <summary>
    /// The row at <paramref name="index"/>; an index outside the block throws
    /// <see cref="ArgumentOutOfRangeException"/>. On an extent still being discovered this reads
    /// through <paramref name="index"/> and no further, so a block can be walked row by row without
    /// asking how many rows there are.
    /// </summary>
    public CellStrip<TSpace> Row(int index)
    {
      if (!Space.HasRow(index))
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The block is {Height} rows tall.");

      return new CellStrip<TSpace>(Space.Slice(new Offset(0, index), new Area(Width, 1)), Orientation.Horizontal, Context);
    }

    /// <summary>
    /// The column at <paramref name="index"/>; an index outside the block throws
    /// <see cref="ArgumentOutOfRangeException"/>. A column spans every row, so on an extent still
    /// being discovered this settles the bound.
    /// </summary>
    public CellStrip<TSpace> Column(int index)
    {
      if (index < 0 || index >= Width)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The block is {Width} columns wide.");

      return new CellStrip<TSpace>(Space.Slice(new Offset(index, 0), new Area(1, Height)), Orientation.Vertical, Context);
    }

    /// <summary>
    /// Every row, top to bottom, built once and cached. How many there are is a dimension query, so
    /// on an extent still being discovered this settles the bound; <see cref="Row"/> in a loop is
    /// the reading that does not.
    /// </summary>
    public IReadOnlyList<CellStrip<TSpace>> Rows => _rows ??= Build(Height, Row);

    /// <summary>
    /// Every column, left to right, built once and cached. Each column spans every row, so on an
    /// extent still being discovered this settles the bound.
    /// </summary>
    public IReadOnlyList<CellStrip<TSpace>> Columns => _columns ??= Build(Width, Column);

    private void Validate(int column, int row)
    {
      if (column < 0 || column >= Width)
        throw new ArgumentOutOfRangeException(nameof(column), column, $"The block is {Width} columns wide.");

      // Asked as "is there a row there", which on a discovered bound advances the scan through that
      // row alone. A row that is not there has settled the bound already, so the message is free.
      if (!Space.HasRow(row))
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
