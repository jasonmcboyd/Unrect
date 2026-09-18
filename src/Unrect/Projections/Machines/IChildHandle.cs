using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>A started child seen without its result type: what a layout or a repeat feeds and closes.</summary>
  internal interface IChildHandle<TSpace>
    where TSpace : class, ISpace
  {
    bool Next(Plane<TSpace> span);

    object? CloseBoxed();

    Offset Offset { get; }

    /// <summary>What the child kept, offset included, from where it was started.</summary>
    Size Advance { get; }

    /// <summary>What the child itself consumed, offset excluded — a repeat's productivity guard reads this.</summary>
    Size Consumed { get; }

    Presence Presence { get; }

    bool PlacementFailed { get; }

    /// <summary>The spans offered to the child that it did not keep, oldest first — for the parent to feed to the successor.</summary>
    IEnumerable<Plane<TSpace>> Shortfall();

    /// <summary>
    /// The oldest source row this child, or anything open beneath it, may still read, and the
    /// innermost machine holding it — or null when nothing under this handle holds a row. Asked
    /// of the root after each row; every handle answers for its subtree.
    /// </summary>
    Hold? Retained(int current);
  }

  /// <summary>A row held, and the machine holding it — what a subtree answers when asked how far back it may still read.</summary>
  internal readonly struct Hold
  {
    internal Hold(int row, IProjectionDefinition holder)
    {
      Row = row;
      Holder = holder;
    }

    /// <summary>The oldest row still needed.</summary>
    internal int Row { get; }

    /// <summary>The innermost machine that needs it.</summary>
    internal IProjectionDefinition Holder { get; }
  }

  /// <summary>
  /// What a handle is to the children started under it: they report to it when they open and
  /// close, so it can answer for its subtree. The engine's placement machine is the one implementer.
  /// </summary>
  internal interface IChildRegistry<TSpace>
    where TSpace : class, ISpace
  {
    void Opened(IChildHandle<TSpace> child);

    void Closed(IChildHandle<TSpace> child);
  }

  /// <summary>
  /// A started child with its result type: what <see cref="ProjectorScope{TSpace}.Start{T}"/> hands
  /// a node's machine. The engine's placement machine implements it; a node feeds it spans, reads
  /// what it kept, and closes it for its settlement.
  /// </summary>
  internal interface IChildHandle<TSpace, T> : IChildHandle<TSpace>
    where TSpace : class, ISpace
  {
    Settlement<T> Close();
  }
}
