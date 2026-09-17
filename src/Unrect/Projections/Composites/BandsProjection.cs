using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One declared projection applied to each band of a fixed stride, until a whole band is no longer
  /// left. The extent is cut rather than searched: nothing about a band's content decides where the
  /// next one starts, and a trailing part-band is not a band at all.
  /// <para>
  /// It declares no extent of its own — how far the tiling runs is whoever places it decides — so
  /// the stride and the bound stay two separate declarations.
  /// </para>
  /// </summary>
  internal sealed class BandsProjection<TSpace, T> : ProjectionBase<TSpace, IReadOnlyList<T>>
    where TSpace : class, ISpace
  {
    public BandsProjection(
      IProjection<TSpace, T> each,
      Orientation orientation,
      int stride,
      UseSite eachSite,
      BlankRowStrategy? onBlank,
      Placement placement)
      : base(placement)
    {
      Each = each ?? throw new ArgumentNullException(nameof(each));
      Orientation = orientation;
      Stride = stride;
      EachSite = eachSite;
      OnBlank = onBlank;
      Children = new[] { new Child(each, eachSite) };
    }

    private IProjection<TSpace, T> Each { get; }

    /// <summary>What the declaration called the band projection, for every band to be labelled by.</summary>
    private UseSite EachSite { get; }

    private Orientation Orientation { get; }

    /// <summary>How many rows (or columns) one band is.</summary>
    private int Stride { get; }

    /// <summary>How a fully-blank band is treated, or null to project every band there is.</summary>
    private BlankRowStrategy? OnBlank { get; }

    public override string Description => Orientation == Orientation.Vertical ? "VerticalBands" : "HorizontalBands";

    public override IReadOnlyList<Child> Children { get; }

    public override ProjectionResult<IReadOnlyList<T>> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      // The across axis, measured once. A vertical tiler takes the width, which is free even on an
      // extent still being discovered; a horizontal one takes the height, which on such an extent
      // settles it — a band spanning the other axis in full is what "column" means, so the cost
      // comes with the axis rather than with this walk.
      var across = Orientation == Orientation.Vertical ? extent.Width : extent.Area.Height;

      var values = new List<T>();
      var bands = 0;
      var cursor = 0;

      while (across > 0 && HasBand(extent, cursor))
      {
        var offset = Step(cursor);
        var band = Band(extent, offset, across);

        if (OnBlank is BlankRowStrategy onBlank && IsBlank(band))
        {
          if (onBlank.IsStop)
            break;

          ReportBlank(onBlank, band, context);
        }
        else
        {
          var scope = context.WithIndex(bands).WithOrdinal(bands).WithUseSite(EachSite);

          values.Add(ProjectionEngine.Apply(Each, band, scope).Value);
        }

        bands++;
        cursor += Stride;
      }

      values.TrimExcess();

      // The cursor is bands VISITED times the stride, not bands collected: a band the policy omitted
      // was still cut out of the extent, and the shape after this one starts past it.
      return new ProjectionResult<IReadOnlyList<T>>(
        values,
        Extent(cursor, across),
        values.Count == 0 ? Presence.Empty : Presence.Read);
    }

    /// <summary>Whether a whole band is left at <paramref name="cursor"/>; a part-band is not one.</summary>
    private bool HasBand(Plane<TSpace> extent, int cursor)
      => Orientation == Orientation.Vertical
        ? extent.HasRow(cursor + Stride - 1)
        : cursor + Stride - 1 < extent.Width;

    /// <summary>
    /// The band at <paramref name="offset"/>, spanning the other axis in full.
    /// <para>
    /// Naming the extent is what makes this work over a bound still being discovered:
    /// cutting a named region advances the discovery through exactly the rows named and
    /// hands back a measured region, so whatever the band projection declares — an area of its own,
    /// or an extent derived from what it reads — is resolved against a region that knows how tall it
    /// is, and no strategy is ever handed an unsettled tail.
    /// </para>
    /// </summary>
    private Plane<TSpace> Band(Plane<TSpace> extent, Offset offset, int across)
      => extent.Slice(offset, new Area(Extent(Stride, across)));

    private void ReportBlank(BlankRowStrategy onBlank, Plane<TSpace> band, ProjectionContext scope)
    {
      var noun = Stride == 1 ? "row" : "band";
      var at = scope.Locate(band).A1;

      if (onBlank.IsFault)
        throw scope.Failure($"the {noun} at {at} is blank, which is not allowed here", band, null, isFault: true);

      if (onBlank.Diagnostic is DiagnosticSeverity severity)
        scope.Report(severity, this, $"the {noun} at {at} is blank; it was skipped", band);
    }

    /// <summary>Whether every cell of a cut band is blank. The band is measured, so its extent is free.</summary>
    private static bool IsBlank(Plane<TSpace> band)
    {
      var area = band.Area;

      for (var row = 0; row < area.Height; row++)
        for (var column = 0; column < area.Width; column++)
          if (!band[column, row].IsBlank)
            return false;

      return true;
    }

    private Offset Step(int along) => Orientation == Orientation.Vertical ? new Offset(0, along) : new Offset(along, 0);

    private Size Extent(int along, int across)
      => Orientation == Orientation.Vertical ? new Size(across, along) : new Size(along, across);
  }
}
