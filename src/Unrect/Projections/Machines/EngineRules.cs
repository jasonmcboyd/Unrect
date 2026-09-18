using System;
using System.IO;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// The rulings every machine and the placement machine apply alike: what counts as a fault, how
  /// a strategy's own exception is worded, when a declared area that read nothing is empty, and how
  /// a size renders in a message. One place, so a leaf and a composite cannot describe the same
  /// event two ways.
  /// </summary>
  internal static class EngineRules
  {
    /// <summary>A declared area consumed in full that read nothing is empty; anything else keeps what it said.</summary>
    internal static Presence Settled(Presence presence, bool hasDeclaredArea, Size consumed)
      => presence == Presence.Read && hasDeclaredArea && (consumed.Width == 0 || consumed.Height == 0)
        ? Presence.Empty
        : presence;

    internal static ProjectionException AreaFailure<TSpace>(ProjectionContext scope, IProjectionDefinition projection, Plane<TSpace> inner, Exception exception)
      where TSpace : class, ISpace
      => exception is OutOfBoundsException
        ? scope.Failure(projection, "its area ran past the space available here", inner, null, exception)
        : scope.Failure(projection, Threw("area", exception), inner, null, exception, IsFault(exception));

    /// <summary>
    /// An exception that says something about this library or the machine, never about the data:
    /// no tolerance boundary absorbs one. Under a genuine out-of-memory condition the wrap itself
    /// may fail to allocate and the original escapes unwrapped, which is fine: an unwrapped
    /// OutOfMemoryException is not a ProjectionException, so nothing catches it either.
    /// </summary>
    internal static bool IsFault(Exception exception)
      => exception is NullReferenceException
        or IndexOutOfRangeException
        or ArgumentOutOfRangeException
        or ArgumentNullException
        or IOException                 // the disk, the network share, the workbook replaced mid-read
        or ObjectDisposedException     // a view outliving its Workbook
        or InvalidCastException        // an invariant this library owes itself; never the data
        or OutOfMemoryException;       // never a statement about the data

    /// <summary>Asked as "is there a row at the last one" rather than "how tall are you", so a region still being read is not forced.</summary>
    internal static bool Exceeds<TSpace>(Size size, Plane<TSpace> space)
      where TSpace : class, ISpace
      => size.Width > space.Width
      || (size.Height > 0 && !space.HasRow(size.Height - 1));

    internal static string Describe(Size size) => $"{size.Width}x{size.Height}";

    internal static string Threw(string what, Exception exception)
      => $"its {what} strategy threw {exception.GetType().Name}: {exception.Message}";

    /// <summary>A matcher that found nothing says what it was looking for; anything else just ran out of room.</summary>
    internal static string Missing(OutOfBoundsException exception)
      => exception is AnchorNotFoundException anchor
        ? $"{anchor.Description} exists in the available space"
        : "its offset ran past the available space";
  }
}
