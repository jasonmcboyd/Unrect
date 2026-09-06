using Unrect.Core;

namespace Unrect
{
  /// <summary>
  /// A space that is a <em>chart</em> of another: the same geometry, the same coordinates, a
  /// different view of it. <see cref="Unrect.Shapes.BoundedSpace"/> is the one in the box — an extent
  /// whose height is still being discovered — and any wrapper that neither moves the origin nor
  /// changes what a cell is belongs here too.
  /// <para>
  /// This is the unwrap protocol behind <see cref="SpaceCapabilities.Capability{TCapability}"/>, and
  /// the reason a raw <c>space is IFormulaSpace</c> is the wrong question: a wrapper cannot
  /// statically implement a capability on behalf of whatever it happens to wrap, so through a chart
  /// the type test says <c>false</c> over a sheet that plainly has the capability.
  /// </para>
  /// <para>
  /// <b>Coordinates must not move.</b> A chart that translates its origin would hand back a
  /// capability answering about the wrong cells — <em>worse</em> than reporting absence. A wrapper
  /// that slices must implement the capability itself, translating as it forwards, which is what the
  /// slicing law asks of every capable backend anyway.
  /// </para>
  /// </summary>
  public interface ISpaceChart
  {
    /// <summary>The space this one charts, in the same coordinates.</summary>
    ISpace Underlying { get; }
  }

  /// <summary>
  /// The capability transport seam: how a declaration asks a space for something
  /// <see cref="ISpace"/> does not promise.
  /// </summary>
  public static class SpaceCapabilities
  {
    /// <summary>
    /// <paramref name="space"/> as a <typeparamref name="TCapability"/>, looking through any
    /// <see cref="ISpaceChart"/> wrappers around it; null when nothing in the stack offers one.
    /// <para>
    /// Null is an honest per-cell answer at a projection site. At a <em>boundary</em> — a matcher
    /// deciding where a region ends — it is not: "I could not look" and "I looked and it is not
    /// there" must never share a spelling, so a boundary that cannot look must fault.
    /// </para>
    /// </summary>
    /// <typeparam name="TCapability">The capability interface being asked for.</typeparam>
    /// <param name="space">The space, possibly charted.</param>
    public static TCapability? Capability<TCapability>(this ISpace? space)
      where TCapability : class
    {
      for (var current = space; current is not null; current = (current as ISpaceChart)?.Underlying)
        if (current is TCapability capability)
          return capability;

      return null;
    }
  }
}
