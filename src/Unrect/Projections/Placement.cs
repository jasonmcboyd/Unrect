using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// Where a projection sits and how big it is, as the data face carries it: whether an offset was
  /// declared, whether an extent was, and whether each answers span by span under a driver. The
  /// rules themselves are written over a space and live on <see cref="Placement{TSpace}"/>, which
  /// the typed face hands back; this base is what a tool walking the erased tree can ask.
  /// </summary>
  public abstract class Placement
  {
    private protected Placement()
    {
    }

    /// <summary>Whether an offset other than "sit where you are put" was declared.</summary>
    public abstract bool HasDeclaredOffset { get; }

    /// <summary>Whether an extent was declared, rather than left for the projection to derive.</summary>
    public abstract bool HasDeclaredExtent { get; }

    /// <summary>Whether the offset was declared by the placement pipeline, so that a later movement composes onto it.</summary>
    internal abstract bool OffsetWasDeclared { get; }

    /// <summary>
    /// Whether both rules answer span by span under <paramref name="driver"/>, building each rule's
    /// scan to ask it; <paramref name="derived"/> says whether the extent is the projection's own to
    /// find. A child that declares no offset steps over the blank spans along
    /// <paramref name="leadingBlanks"/> when that is given.
    /// </summary>
    internal abstract bool StreamsUnder(Orientation driver, Orientation? leadingBlanks, bool declaresOffset, out bool derived);
  }

  /// <summary>
  /// A projection's placement over <typeparamref name="TSpace"/>: how its origin is found within the
  /// space it is handed, and optionally how its extent is. A null <see cref="Extent"/> means the
  /// extent is derived from the projection's own content.
  /// </summary>
  /// <typeparam name="TSpace">The space the rules read.</typeparam>
  public sealed class Placement<TSpace> : Placement
    where TSpace : class, ISpace
  {
    // The "no offset declared yet" strategy, so HasDeclaredOffset is a reference test. MinOffset
    // hands back one canonical no-movement per space, so a caller who declares one by name lands on
    // this same value rather than on a second way of saying nothing.
    private static readonly IOffsetStrategy<TSpace> NoOffset = OffsetStrategies.MinOffset<TSpace>();

    /// <summary>
    /// Creates a placement from an explicit offset and extent; <paramref name="extent"/> may be null
    /// (derived extent). This is how a projection states its <em>own</em> placement — a
    /// pipeline entry composes onto it through <see cref="WithOffset"/> and <see cref="WithExtent"/>.
    /// </summary>
    public Placement(IOffsetStrategy<TSpace> offset, ISizeStrategy<TSpace>? extent)
      : this(offset, extent, offsetWasDeclared: false)
    {
    }

    private Placement(IOffsetStrategy<TSpace> offset, ISizeStrategy<TSpace>? extent, bool offsetWasDeclared)
    {
      Offset = offset ?? throw new ArgumentNullException(nameof(offset));
      Extent = extent;
      OffsetWasDeclared = offsetWasDeclared;
    }

    /// <summary>No offset declared, no extent declared — a projection that sits where it is handed and derives its own extent.</summary>
    public static Placement<TSpace> Default { get; } = new Placement<TSpace>(NoOffset, null);

    /// <summary>No offset declared, but <paramref name="extent"/> is — a projection that sits where it is handed with a declared extent.</summary>
    public static Placement<TSpace> Of(ISizeStrategy<TSpace> extent) => new Placement<TSpace>(NoOffset, NotNull(extent));

    /// <summary>How the projection's origin is found within the space it is handed.</summary>
    public IOffsetStrategy<TSpace> Offset { get; }

    /// <summary>How the projection's extent is found, once its origin is known; null means the extent is derived, not declared.</summary>
    public ISizeStrategy<TSpace>? Extent { get; }

    /// <summary>
    /// A copy with <paramref name="offset"/> in place of this placement's own — the extent is
    /// untouched, and the copy records that the offset was declared rather than defaulted.
    /// </summary>
    public Placement<TSpace> WithOffset(IOffsetStrategy<TSpace> offset)
      => new Placement<TSpace>(offset, Extent, offsetWasDeclared: true);

    /// <summary>A copy with <paramref name="extent"/> in place of this placement's own — the offset is untouched.</summary>
    public Placement<TSpace> WithExtent(ISizeStrategy<TSpace> extent)
      => new Placement<TSpace>(Offset, NotNull(extent), offsetWasDeclared: OffsetWasDeclared);

    /// <inheritdoc/>
    public override bool HasDeclaredOffset => !ReferenceEquals(Offset, NoOffset);

    /// <inheritdoc/>
    public override bool HasDeclaredExtent => Extent is not null;

    /// <inheritdoc/>
    internal override bool OffsetWasDeclared { get; }

    /// <inheritdoc/>
    internal override bool StreamsUnder(Orientation driver, Orientation? leadingBlanks, bool declaresOffset, out bool derived)
    {
      var (offset, size) = Begin(driver, leadingBlanks, declaresOffset);

      derived = size is null;
      return offset.Incremental && (size is null || size.Incremental);
    }

    /// <summary>
    /// The two scans a placement machine drives under <paramref name="driver"/>: the offset's — the
    /// blank-skipping default along <paramref name="leadingBlanks"/> for a child that declares none
    /// down its wrapper chain — and the extent's, null when the extent is derived.
    /// </summary>
    internal (IOffsetScan<TSpace> Offset, ISizeScan<TSpace>? Extent) Begin(Orientation driver, Orientation? leadingBlanks, bool declaresOffset)
    {
      var offset = leadingBlanks is Orientation session && !declaresOffset
        ? (session == Orientation.Vertical ? OffsetStrategies.SkipBlankRows<TSpace>() : OffsetStrategies.SkipBlankColumns<TSpace>()).Begin(driver)
        : Offset.Begin(driver);

      return (offset, Extent?.Begin(driver));
    }

    // Only the constructor takes a null extent, where it deliberately means "derive the extent".
    // Everywhere else a null would silently turn a declared extent into a derived one.
    private static ISizeStrategy<TSpace> NotNull(ISizeStrategy<TSpace> extent) => extent ?? throw new ArgumentNullException(nameof(extent));
  }
}
