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
