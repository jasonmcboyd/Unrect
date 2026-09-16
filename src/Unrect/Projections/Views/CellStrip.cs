using System;
using System.Collections;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One row or one column of cells, indexed along its own axis and knowing where it sits, so a
  /// caller's own complaints about the data can cite a cell the way the framework's do.
  /// <para>
  /// A cell is a <see cref="Point{TSpace}"/> — a place rather than a value — so what a strip offers
  /// is addressing, and reading is whatever the space can answer: the canonical questions on every
  /// space, and a backend's own reads where the space carries them.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space the strip's cells belong to.</typeparam>
  public sealed class CellStrip<TSpace> : IReadOnlyList<Point<TSpace>>
    where TSpace : class, ISpace
  {
    internal CellStrip(Plane<TSpace> space, Orientation orientation, ProjectionContext context)
    {
      Space = space;
      Orientation = orientation;
      Context = context;
    }

    /// <summary>The strip's own extent — one cell wide or one cell tall, depending on its orientation.</summary>
    public Plane<TSpace> Space { get; }

    private Orientation Orientation { get; }

    /// <summary>
    /// The context the strip was projected in — where it sits, and what a reading built on it blames
    /// when it cannot make sense of a cell.
    /// </summary>
    private ProjectionContext Context { get; }

    /// <summary>
    /// How many cells the strip holds. A row's length is its extent's width, which is free even
    /// where the height is still being discovered; a column's is that height, and asking settles
    /// it.
    /// </summary>
    public int Count => Orientation == Orientation.Horizontal ? Space.Width : Space.Area.Height;

    /// <summary>The cell at <paramref name="index"/> along the strip's own axis; an index outside it throws <see cref="ArgumentOutOfRangeException"/>.</summary>
    public Point<TSpace> this[int index]
    {
      get
      {
        Validate(index);

        var step = Step(index);

        return Space[step.Width, step.Height];
      }
    }

    /// <summary>
    /// The address of the strip's first cell. It carries the extent it was found in, so on one
    /// whose height is still being discovered this settles the bound.
    /// </summary>
    public ProjectionLocation Location => ProjectionLocation.At(Space);

    /// <summary>The address of one cell of the strip, for citing it in a message.</summary>
    /// <param name="index">The cell's position along the strip's own axis.</param>
    public ProjectionLocation AddressOf(int index)
    {
      Validate(index);

      return ProjectionLocation.At(Space.Origin + Step(index), Space.Area.Size);
    }

    /// <summary>The strip's cells, in order.</summary>
    public IEnumerator<Point<TSpace>> GetEnumerator()
    {
      for (var index = 0; index < Count; index++)
        yield return this[index];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// A failure blaming the declaration that named this strip — how a reading built on one reports
    /// a cell it could not make sense of.
    /// </summary>
    internal ProjectionException Failure(string problem) => Context.Failure(problem, Space);

    private Offset Step(int index) => Orientation == Orientation.Horizontal ? new Offset(index, 0) : new Offset(0, index);

    private void Validate(int index)
    {
      if (index < 0 || index >= Count)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The strip has {Count} cells.");
    }
  }
}
