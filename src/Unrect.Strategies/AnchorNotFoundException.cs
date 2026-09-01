using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A seek that found nothing. It is an <see cref="OutOfBoundsException"/> because a missing
  /// anchor is a placement failure like any other — strict callers report it, and a repeat whose
  /// item seeks its own anchor simply stops. The extra <see cref="Description"/> lets the shape
  /// layer say what was sought; it is internal, and visible to the Unrect assembly for that
  /// purpose alone (see the InternalsVisibleTo item in Unrect.Strategies.csproj), so the public
  /// surface still shows nothing but <see cref="OutOfBoundsException"/>.
  /// </summary>
  internal sealed class AnchorNotFoundException : OutOfBoundsException
  {
    public AnchorNotFoundException(string description)
    {
      Description = description;
    }

    /// <summary>What was not found, phrased as a noun: "no row containing 'Taxable Income'".</summary>
    public string Description { get; }
  }
}
