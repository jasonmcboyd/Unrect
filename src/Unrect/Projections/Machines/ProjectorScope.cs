using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a machine needs of the run it belongs to: its tree position for diagnostics, and the
  /// engine's services for starting a child behind the placement machine that feeds it. Abstract
  /// with a <c>private protected</c> constructor: only the engine derives one, so it can grow a
  /// member without breaking anyone — the same reason <c>Plane</c> and <c>Point</c> are structs.
  /// </summary>
  /// <typeparam name="TSpace">The space the run is over.</typeparam>
  public abstract class ProjectorScope<TSpace>
    where TSpace : class, ISpace
  {
    private protected ProjectorScope()
    {
    }

    /// <summary>The axis the driver offers spans along.</summary>
    internal abstract Orientation Driver { get; }

    /// <summary>The tree position and diagnostics this scope is at — the pull engine's own context, shared until it retires.</summary>
    internal abstract ProjectionContext Context { get; }

    /// <summary>Where a machine started under this scope would begin — used only when it is never offered a span.</summary>
    internal abstract Plane<TSpace> Anchor { get; }

    /// <summary>This scope at <paramref name="context"/> and <paramref name="anchor"/>, driving along <paramref name="driver"/>.</summary>
    internal abstract ProjectorScope<TSpace> At(ProjectionContext context, Plane<TSpace> anchor, Orientation driver);

    /// <summary>
    /// Starts <paramref name="definition"/> as a child at <paramref name="edge"/>, behind the
    /// placement machine that resolves its offset and area and feeds it. <paramref name="anchor"/> is
    /// where the child would begin — the parent's current position — used when no span is ever
    /// offered. <paramref name="occurrence"/> stamps a repeat's or tiler's index and ordinal.
    /// <paramref name="strict"/> false makes a placement failure a refusal the parent reads off the
    /// handle rather than a thrown failure — a repeat's stopping condition.
    /// </summary>
    internal abstract IChildHandle<TSpace, T> Start<T>(Child edge, IProjectionDefinition<TSpace, T> definition, Plane<TSpace> anchor, int? occurrence = null, bool strict = true, bool inheritSite = false);

    /// <summary>Drives <paramref name="machine"/> over <paramref name="region"/> along <paramref name="along"/> (or as one span when null) and closes it.</summary>
    internal abstract Settlement<T> Drive<T>(IProjector<TSpace, T> machine, Plane<TSpace> region, Orientation? along);
  }
}
