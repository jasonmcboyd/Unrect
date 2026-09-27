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

    /// <summary>
    /// Whether <paramref name="definition"/> says where it starts, itself or through the wrappers
    /// around its one inner node. One that does not takes the default: past the blank spans in front.
    /// </summary>
    internal static bool DeclaresOffset(IProjectionDefinition definition)
    {
      for (var node = definition; ; node = node.Children[0].Definition)
      {
        if (node.Placement.HasDeclaredOffset)
          return true;

        if (!node.IsWrapper || node.Children.Count != 1)
          return false;
      }
    }

    /// <summary>Whether <paramref name="definition"/> streams under <paramref name="driver"/>, asked of the erased tree — the dry run's question.</summary>
    internal static bool Streams(IProjectionDefinition definition, Orientation driver, out string? hold, Orientation? leadingBlanks = null)
    {
      var placementStreams = definition.Placement.StreamsUnder(driver, leadingBlanks, DeclaresOffset(definition), out var derived);

      return Streams(definition, driver, placementStreams, derived, out hold);
    }

    /// <summary>
    /// Whether <paramref name="definition"/> streams under <paramref name="driver"/>, handing back
    /// the two scans its placement machine will drive — the engine's question, asked of the typed
    /// tree because the scans are written over its space.
    /// </summary>
    internal static bool Streams<TSpace, T>(IProjectionDefinition<TSpace, T> definition, Orientation driver, out IOffsetScan<TSpace> offset, out ISizeScan<TSpace>? size, out bool derived, out string? hold, Orientation? leadingBlanks = null)
      where TSpace : class, ISpace
    {
      (offset, size) = definition.Placement.Begin(driver, leadingBlanks, DeclaresOffset(definition));
      derived = size is null;

      return Streams(definition, driver, offset.Incremental && (size is null || size.Incremental), derived, out hold);
    }

    private static bool Streams(IProjectionDefinition definition, Orientation driver, bool placementStreams, bool derived, out string? hold)
    {
      var spans = driver == Orientation.Vertical ? "row" : "column";

      if (!placementStreams)
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
