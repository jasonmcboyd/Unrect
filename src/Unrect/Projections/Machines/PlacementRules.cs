using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Driven or held: the one decision the placement machine and the cost report share. A
  /// definition is driven span by span when its placement's scans answer as spans arrive and it
  /// can take the driver's spans — along an axis it announces, or, for a collector that announces
  /// none, under a declared rule that bounds it. Anything else is held whole and placed at close.
  /// </summary>
  internal static class PlacementRules
  {
    /// <summary>How far back <paramref name="definition"/>'s own machine may still read once placed: what its node declares, or its extent for a node this library did not write.</summary>
    internal static Reach Retains(IProjectionDefinition definition)
      => definition is DefinitionNode node ? node.Retains : Reach.Extent;

    internal static bool Streams(IProjectionDefinition definition, Orientation driver, out string? hold)
      => Streams(definition, driver, out _, out _, out _, out hold);

    /// <summary>The same decision, handing back the scans a driven placement runs on.</summary>
    internal static bool Streams(IProjectionDefinition definition, Orientation driver, out IOffsetScan offset, out ISizeScan? size, out bool derived, out string? hold)
    {
      var spans = driver == Orientation.Vertical ? "row" : "column";

      offset = definition.Placement.Offset.Begin(driver);
      size = definition.Placement.Area?.Begin(driver);
      derived = size is null;

      if (!offset.Incremental || (size is ISizeScan scan && !scan.Incremental))
      {
        hold = $"its placement answers only over its whole extent under a {spans} driver";
        return false;
      }

      if (definition is DefinitionNode node && node.Holds is string reason)
      {
        hold = reason;
        return false;
      }

      if (definition.Axis.Streams(driver))
      {
        hold = null;
        return true;
      }

      // A collector under a declared rule takes the driver's spans whichever axis it announces:
      // the rule bounds it, and it reads the region whole at close.
      if (!derived && definition is DefinitionNode collector && collector.Collects)
      {
        hold = null;
        return true;
      }

      if (definition.Axis == Axes.None)
      {
        hold = "it reads its extent whole and bounds it itself";
        return false;
      }

      hold = $"it streams along {(definition.Axis == Axes.Vertical ? "rows" : "columns")} only";
      return false;
    }
  }
}
