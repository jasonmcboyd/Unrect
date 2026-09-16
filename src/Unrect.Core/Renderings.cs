using System.Globalization;

namespace Unrect.Core
{
  /// <summary>
  /// The one way a double is written as text. Written out because neither of this package's
  /// targets renders a double the same way by default: .NET Framework's <c>"R"</c> tries fifteen
  /// significant digits and then falls straight to seventeen, so a value whose shortest exact form
  /// has sixteen says <c>0.33333333333333331</c> there and <c>0.3333333333333333</c> everywhere
  /// else. A declaration matching on a rendered number must find it through either target.
  /// </summary>
  internal static class Renderings
  {
    /// <summary>
    /// The fewest significant digits that read back as exactly this double: fifteen, sixteen or
    /// seventeen, invariant culture. Not-a-number and the infinities render as their names.
    /// </summary>
    internal static string ShortestRoundTrip(double value)
    {
      var fifteen = value.ToString("G15", CultureInfo.InvariantCulture);
      if (ReadsBack(fifteen, value))
        return fifteen;

      var sixteen = value.ToString("G16", CultureInfo.InvariantCulture);
      if (ReadsBack(sixteen, value))
        return sixteen;

      return value.ToString("G17", CultureInfo.InvariantCulture);
    }

    private static bool ReadsBack(string text, double value)
      => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
         && (parsed == value || (double.IsNaN(parsed) && double.IsNaN(value)));
  }
}
