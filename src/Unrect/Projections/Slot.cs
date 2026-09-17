using System;

namespace Unrect.Projections
{
  /// <summary>
  /// A typed slot in one layout: what <c>Next</c> hands back in place of a value, and what the
  /// combiner handed to <c>Build</c> reads through — <c>read.Of(slot)</c>. An index into the
  /// layout's reading, not a box: the declaration holds no value, so a layout stays a reusable,
  /// thread-safe value applied to many spaces at once.
  /// </summary>
  /// <typeparam name="T">What the child in this slot reads.</typeparam>
  public readonly struct Slot<T>
  {
    internal Slot(object layout, int index)
    {
      Layout = layout;
      Index = index;
    }

    /// <summary>The layout that declared this slot, or null for <c>default</c>, which no layout declared.</summary>
    internal object? Layout { get; }

    internal int Index { get; }
  }

  /// <summary>
  /// What a layout's children read, in one application of it — the argument to the combiner
  /// <c>Build</c> was given. A slot declared by another layout is refused: a reading answers only
  /// for the children that produced it.
  /// </summary>
  public readonly struct Reading
  {
    private readonly object _layout;
    private readonly object?[] _values;

    internal Reading(object layout, object?[] values)
    {
      _layout = layout;
      _values = values;
    }

    /// <summary>What the child in <paramref name="slot"/> read.</summary>
    /// <typeparam name="T">What that child reads.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="slot"/> was never declared, or was declared by a different layout.
    /// </exception>
    public T Of<T>(Slot<T> slot)
    {
      if (slot.Layout is null)
        throw new InvalidOperationException("This slot was never declared; a slot comes from Next on a layout cursor.");

      if (!ReferenceEquals(slot.Layout, _layout))
        throw new InvalidOperationException("A slot reads only in the layout that declared it; this one belongs to another layout.");

      return (T)_values[slot.Index]!;
    }
  }
}
