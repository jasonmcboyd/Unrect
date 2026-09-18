using System;

namespace Unrect.Projections
{
  /// <summary>
  /// The axes a definition's machine streams along. The engine compares this to the driver's axis:
  /// same axis, spans pass straight through; different or <see cref="None"/>, the engine holds the
  /// node's extent and re-drives it along its own axis from the buffer. A one-cell leaf streams
  /// along <see cref="Either"/>; a block lambda over its whole extent streams along
  /// <see cref="None"/>.
  /// </summary>
  [Flags]
  public enum Axes
  {
    /// <summary>Streams along no axis: the machine reads its whole extent at random, so it is held and handed the region as one span.</summary>
    None = 0,

    /// <summary>Streams along rows.</summary>
    Vertical = 1,

    /// <summary>Streams along columns.</summary>
    Horizontal = 2,

    /// <summary>Streams along either: a one-cell leaf, or a wrapper of one.</summary>
    Either = Vertical | Horizontal,
  }

  internal static class AxesExtensions
  {
    internal static Axes Of(this Orientation orientation)
      => orientation == Orientation.Vertical ? Axes.Vertical : Axes.Horizontal;

    internal static bool Streams(this Axes axes, Orientation driver) => (axes & driver.Of()) != 0;

    /// <summary>The axis a held node is re-driven along: its own, preferring the driver's when it has both.</summary>
    internal static Orientation? Along(this Axes axes, Orientation driver)
      => axes.Streams(driver) ? driver
       : (axes & Axes.Vertical) != 0 ? Orientation.Vertical
       : (axes & Axes.Horizontal) != 0 ? Orientation.Horizontal
       : (Orientation?)null;
  }
}
