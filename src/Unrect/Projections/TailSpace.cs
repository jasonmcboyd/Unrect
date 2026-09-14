using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// An extent whose height is not settled: the width, which is free, and the forward row probe that
  /// asks for one row rather than for all of them.
  /// </summary>
  internal interface ILazyExtent
  {
    /// <summary>The extent's width, which is settled before any row is read.</summary>
    int LazyWidth { get; }

    /// <summary>Whether there is a row at <paramref name="row"/>, reading only as far as it takes to say.</summary>
    bool LazyHasRow(int row);

    /// <summary>A view of this extent from <paramref name="offset"/>, <paramref name="width"/> wide, with the height still unsettled.</summary>
    ISpace LazySlice(Offset offset, int width);
  }

  /// <summary>
  /// A view into a <see cref="BoundedSpace"/> from an offset, whose height is still the bound's to
  /// discover. Every cell it addresses comes from a real subspace of the measured space underneath,
  /// so slicing it hands back an ordinary measured space and only row admission goes back to the
  /// bound.
  /// </summary>
  internal sealed class TailSpace : ISpace, ISpaceChart, ILazyExtent
  {
    private TailSpace(BoundedSpace bound, int rowShift, ISpace view, int width)
    {
      Bound = bound;
      RowShift = rowShift;
      View = view;
      LazyWidth = width;
    }

    /// <summary>The bound that decides which rows exist.</summary>
    private BoundedSpace Bound { get; }

    /// <summary>This view's first row, counted in the bound's own rows.</summary>
    private int RowShift { get; }

    /// <summary>The measured space every cell comes from, already sliced for the offset and the width.</summary>
    private ISpace View { get; }

    public int LazyWidth { get; }

    /// <summary>
    /// The chart's underlying space. It is the view this space reads its own cells through, so its
    /// cell (c, r) is this one's cell (c, r) — the condition <see cref="ISpaceChart"/> imposes, met
    /// by construction rather than by agreement.
    /// </summary>
    ISpace ISpaceChart.Underlying => View;

    /// <summary>
    /// The extent, which means reading the bound's scan to exhaustion. The bound admitted every row
    /// this view was offset past, so the subtraction never goes below zero.
    /// </summary>
    public Area Area => new Area(LazyWidth, Bound.ForceResolved().Height - RowShift);

    /// <inheritdoc/>
    public CellValue this[int column, int row]
    {
      get
      {
        if (column < 0 || column >= LazyWidth || !LazyHasRow(row))
          throw new OutOfBoundsException();

        return View[column, row];
      }
    }

    /// <inheritdoc/>
    public ISpace GetSubspace(Offset offset, Area area)
    {
      if (offset.Width + area.Width > LazyWidth)
        throw new OutOfBoundsException();

      var rows = offset.Height + area.Height;

      // Through the rows asked for and no further, as the bound itself slices: what comes back is
      // an ordinary measured subspace, its extent having just been named.
      if (rows > 0 && !LazyHasRow(rows - 1))
        throw new OutOfBoundsException();

      return View.GetSubspace(offset, area);
    }

    public bool LazyHasRow(int row) => row >= 0 && Bound.Includes(row + RowShift);

    public ISpace LazySlice(Offset offset, int width) => Over(Bound, View, RowShift, offset, width);

    /// <summary>
    /// A tail of <paramref name="view"/> from <paramref name="offset"/>, still admitting rows
    /// through <paramref name="bound"/>. Slicing the view is free — it is measured — so the bound is
    /// asked one question: whether the last row the offset steps over is there, which is the
    /// measured path's "does this offset fit" asked a row at a time. An offset landing exactly at the
    /// far edge is a zero-height tail rather than an overrun, as it is there.
    /// </summary>
    internal static ISpace Over(BoundedSpace bound, ISpace view, int rowShift, Offset offset, int width)
    {
      if (offset.Height > 0 && !bound.Includes(rowShift + offset.Height - 1))
        throw new OutOfBoundsException();

      var inner = view.GetSubspace(offset, new Area(width, view.Area.Height - offset.Height));

      return new TailSpace(bound, rowShift + offset.Height, inner, width);
    }
  }
}
