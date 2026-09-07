using Unrect.Core;

namespace Unrect
{
  /// <summary>
  /// A space that is a <em>chart</em> of another: the same geometry, the same coordinates, a
  /// different view of it. <see cref="Unrect.Projections.BoundedSpace"/> is the one in the box — an
  /// extent whose height is still being discovered — and any wrapper that neither moves the origin
  /// nor changes what a cell is belongs here too.
  /// <para>
  /// This is the unwrap protocol behind <see cref="SpaceCapabilities.Capability{TCapability}"/>,
  /// and the reason a raw <c>space is IFormulaSpace</c> is the wrong question: a wrapper cannot
  /// statically implement a capability on behalf of whatever it happens to wrap, so through a chart
  /// the type test says <c>false</c> over a sheet that plainly has the capability.
  /// </para>
  /// <para>
  /// <b>Coordinates must not move.</b> This interface says one thing and it is not "here is what I
  /// wrap": it says <em>my cell (c, r) is the underlying space's cell (c, r)</em>. A chart that
  /// translated its origin would hand back a capability answering about the wrong cells — worse
  /// than reporting absence, because the answer would look right. A wrapper that slices must
  /// therefore implement the capability itself, translating as it forwards, which is what the
  /// slicing law asks of every capable backend anyway; it must not implement this.
  /// </para>
  /// <para>
  /// Narrowing is not moving. <see cref="Unrect.Projections.BoundedSpace"/> hides rows past a bound
  /// it has not admitted yet and is still a chart, because every cell it does address is the same
  /// cell underneath.
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
  /// <para>
  /// Two doors, because absence means two different things. <see cref="Capability{TCapability}"/>
  /// answers null, which is what a <em>projection</em> site wants: a cell in a space that cannot
  /// carry formulas has no formula, and that is a fact about the cell.
  /// <see cref="RequiredCapability{TCapability}"/> throws, which is what a <em>boundary</em> site
  /// wants: a matcher that reported "no match" because it could not look would be describing the
  /// document when it was really describing itself.
  /// </para>
  /// </summary>
  public static class SpaceCapabilities
  {
    /// <summary>
    /// <paramref name="space"/> as a <typeparamref name="TCapability"/>, looking through any
    /// <see cref="ISpaceChart"/> wrappers around it; null when nothing in the stack offers one.
    /// <para>
    /// The walk is the whole point. A declaration is handed subspaces and charts, not the sheet the
    /// backend built, so a direct type test answers about the wrapper rather than about the file —
    /// <c>false</c> over a formula-bearing sheet reached through a discovered extent. Every chart in
    /// the stack keeps the same coordinates, so whatever comes back answers about the cells the
    /// caller is holding.
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

    /// <summary>
    /// <see cref="Capability{TCapability}"/> for a caller that cannot proceed without it: the same
    /// walk, and a <see cref="MissingCapabilityException"/> where that one would answer null.
    /// <para>
    /// This is the boundary site's door, and the fault it throws is unabsorbable by design — see
    /// <see cref="MissingCapabilityException"/>. <paramref name="demandedBy"/> is what a failure
    /// will name, so pass the matcher's own spelling (<c>"RowWithFormula"</c>), not a type name.
    /// </para>
    /// </summary>
    /// <typeparam name="TCapability">The capability interface being demanded.</typeparam>
    /// <param name="space">The space, possibly charted.</param>
    /// <param name="demandedBy">What is asking, as a failure should name it.</param>
    /// <exception cref="MissingCapabilityException">Nothing in the stack offers the capability.</exception>
    public static TCapability RequiredCapability<TCapability>(this ISpace? space, string demandedBy)
      where TCapability : class
      => space.Capability<TCapability>()
        ?? throw new MissingCapabilityException(typeof(TCapability), demandedBy);
  }
}
