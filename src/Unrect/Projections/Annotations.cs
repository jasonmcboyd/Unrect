using System;

namespace Unrect.Projections
{
  /// <summary>
  /// What a declaration wrote on a projection, apart from its structure: the name <c>.Named</c>
  /// gave it, the unit label <c>.AsUnit</c> gave it, whether <c>.AsScaffolding</c> marked it, and
  /// where it sits. One immutable record every projection carries, so a modifier that changes any of
  /// them hands the projection a new record rather than reaching for a field.
  /// </summary>
  public sealed class Annotations
  {
    /// <summary>Unnamed, unlabelled, not scaffolding, placed by <see cref="Placement.Default"/>.</summary>
    public static Annotations Default { get; } = new Annotations(null, null, false, Placement.Default);

    private Annotations(string? name, string? unitName, bool isScaffolding, Placement placement)
    {
      Name = name;
      UnitName = unitName;
      IsScaffolding = isScaffolding;
      Placement = placement;
    }

    /// <inheritdoc cref="IProjectionDefinition.Name"/>
    public string? Name { get; }

    /// <inheritdoc cref="IProjectionDefinition.UnitName"/>
    public string? UnitName { get; }

    /// <inheritdoc cref="IProjectionDefinition.IsScaffolding"/>
    public bool IsScaffolding { get; }

    /// <inheritdoc cref="IProjectionDefinition.Placement"/>
    public Placement Placement { get; }

    /// <summary>The same annotations, named <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public Annotations WithName(string name)
      => new Annotations(name ?? throw new ArgumentNullException(nameof(name)), UnitName, IsScaffolding, Placement);

    /// <summary>The same annotations, with unit label <paramref name="unitName"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="unitName"/> is null.</exception>
    public Annotations WithUnitName(string unitName)
      => new Annotations(Name, unitName ?? throw new ArgumentNullException(nameof(unitName)), IsScaffolding, Placement);

    /// <summary>The same annotations, marked scaffolding.</summary>
    public Annotations AsScaffolding()
      => IsScaffolding ? this : new Annotations(Name, UnitName, true, Placement);

    /// <summary>The same annotations, placed by <paramref name="placement"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="placement"/> is null.</exception>
    public Annotations WithPlacement(Placement placement)
      => new Annotations(Name, UnitName, IsScaffolding, placement ?? throw new ArgumentNullException(nameof(placement)));
  }
}
