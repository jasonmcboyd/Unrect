using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The one check a member that wants a rule about one axis makes on the rule it was handed: a
  /// line rule says which axis it is about, and a declaration that passes a column rule where a
  /// row rule was wanted is refused where it is written, with the two axes named.
  /// </summary>
  internal static class LineAxis
  {
    internal static ILineStrategy Require(ILineStrategy strategy, Orientation along, string parameter)
    {
      if (strategy is null)
        throw new ArgumentNullException(parameter);

      if (strategy.Along != along)
        throw new ArgumentException($"a rule about {Lines(strategy.Along)} where a rule about {Lines(along)} was wanted", parameter);

      return strategy;
    }

    internal static ILineLandmark Require(ILineLandmark landmark, Orientation along, string parameter)
    {
      if (landmark is null)
        throw new ArgumentNullException(parameter);

      if (landmark.Along != along)
        throw new ArgumentException($"a landmark that finds a {Line(landmark.Along)} where one that finds a {Line(along)} was wanted", parameter);

      return landmark;
    }

    private static string Lines(Orientation along) => along == Orientation.Vertical ? "rows" : "columns";

    private static string Line(Orientation along) => along == Orientation.Vertical ? "row" : "column";
  }
}
